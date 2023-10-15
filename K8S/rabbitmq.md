# part 7 - Message Bus with RabbitMQ
Goal is to implement the Message-Bus and add the PlatformService as a Publisher and the CommandService as a Subscriber.

## notes about RabbitMQ
- Message Broker: accepts, forwards messages
- Messages are stored on Queues. (in real production those would be persisted if RabbitMQ crashes etc...)
- uses AMQP - Advanced Message Queuing Protocl (among others)
- 4 types of exchanges
    - direct exchange - delivers messages to queues based on a routing key. ideal for direct/unicasting messaging
    - fanout exchange (used here) - delivers messages to all queues bound to the exchange. ideal for broadcast messages.
    - topic exchance - routes messages to 1 or more queues based on routingkey/patterns. ideal for multicasting messages
    - header exchange

## starting up RabbitMQ in Kubernetes
- `K8S/rabbitmq-depl.yaml`
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: rabbitmq-depl
spec:
  replicas: 1
  selector:
    matchLabels:
      app: rabbitmq
  template:
    metadata:
      labels:
        app: rabbitmq
    spec:
      containers:
        - name: rabbitmq
          image: rabbitmq:3-management
          ports:
            ## first port is just to access the "management" webinterface
            - containerPort: 15672
              name: rbmq-mgmt-port
            ## this is the used port for the Bus itself
            - containerPort: 5672
              name: rbmq-msg-port
---
# the Bus needs to be accessible from the Services inside Kubernetes, so we create a ClusterIP for it
apiVersion: v1
kind: Service
metadata:
  name: rabbitmq-clusterip-srv
spec:
  type: ClusterIP
  selector:
    app: rabbitmq
  ports:
    - name: rbmq-mgmt-port
      protocol: TCP
      port: 15672
      targetPort: 15672
    - name: rbmq-msg-port
      protocol: TCP
      port: 5672
      targetPort: 5672 
---
# the Bus also needs to be accessible from outside the Kubernetes (at least for development)
# so we create a LoadbalancerService for it
apiVersion: v1
kind: Service
metadata:
  name: rabbitmq-loadbalancer
spec:
  type: LoadBalancer
  selector:
    app: rabbitmq
  ports:
    - name: rbmq-mgmt-port
      protocol: TCP
      port: 15672
      targetPort: 15672
    - name: rbmq-msg-port
      protocol: TCP
      port: 5672
      targetPort: 5672 
```
- then we deploy our messagebus
```
kubectl apply -f K8S/rabbitmq-depl.yaml
```
- now we can reach out messagebus webinterface with `localhost:15672` username: guest password: guest

## Code in PlatformService
```
dotnet add package RabbitMQ.Client
```
- we add to `appsettings.Development.json` 
```json
"RabbitMQHost": "localhost",
"RabbitMQPort": "5672"
```
- we add to `appsettings.Production.json`
```json
"RabbitMQHost": "rabbitmq-clusterip-srv",
"RabbitMQPort": "5672"
```

- we create `Dtos/PlatformPublishedDto` This is the Event that gets pushed onto the MessageBus
```csharp
public class PlatformPublishdDto {
    public required int Id { get; set; }
    public required string Name { get; set; }  
    public required string Event { get; set; }
}
```

### Implementing the Message Bus Client

- we create an interface for the following RabbitMQ Message Bus implementation. `AsyncDataServices/IMessageBusClient.cs`
```csharp
public interface IMessageBusClient {
    void PublishNewPlatform(PlatformPublishdDto newCreatedPlatform);
}
```

- we inject our Bus in `Program.cs`. Here as a Singleton as we assume it always stays the "same" connection.
```csharp
builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();
```
`AsyncDataServices/IMessageBusClient.cs`
```csharp
public class MessageBusClient : IMessageBusClient
{
    private readonly IConfiguration _config;
    private readonly RabbitMQ.Client.IConnection _connection;
    private readonly RabbitMQ.Client.IModel _channel;

    public MessageBusClient(IConfiguration configuration) {
        _config = configuration;
        // RabbitMQ 1. wants the factory with config data 
        var factory = new RabbitMQ.Client.ConnectionFactory() {
            HostName = _config["RabbitMQHost"],
            Port = int.Parse(_config["RabbitMQPort"]!),
        };

        try {
            // RabbitMQ 2. wants us to create the connection itself
            _connection = factory.CreateConnection();

            // RabbitMQ 3. wants us to create our channel
            _channel = _connection.CreateModel();

            // RabbitMQ 4. wants us to create the Exchange( in this case the fanout-type)
            _channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout);

            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;

            Console.WriteLine("--> Connected to MessageBus");

        } catch (Exception e) {
            Console.WriteLine($"--> Could not connect to the Messagebus! {e.Message}");
        }
    }

    public void PublishNewPlatform(PlatformPublishdDto newCreatedPlatformDto) {
        var message = JsonSerializer.Serialize(newCreatedPlatformDto);

        if (_connection.IsOpen) {
            Console.WriteLine("--> RabbitMQ Connection Open, sending message.");
            SendMessage(message);
        } else {
            Console.WriteLine("--> RabbitMQ Connection CLOSED, NOT sending!");
        }
    }

    private void SendMessage(string message) {
        var body = Encoding.UTF8.GetBytes(message);
        _channel.BasicPublish(
            exchange: "trigger", 
            routingKey: "", 
            basicProperties: null, 
            body: body);
        
        Console.WriteLine($"--> We have sent {message}");
    }
    
    // properly close ressources when this class leaves scope/dies
    public void Dispose() {
        Console.WriteLine("--> MessageBus Disposed");
        if (_channel.IsOpen) {
            _channel.Close();
            _connection.Close();
        }
    }

    // triggers every time the connection to the Bus gets shut down
    private void RabbitMQ_ConnectionShutdown(object? sender, ShutdownEventArgs args) {
        Console.WriteLine($"--> RabbitMQ Connection Shut Down. args={args}");
    }
}
```

### We Use that Bus to send in our Controller
