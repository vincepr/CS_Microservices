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

## start up RabbitMQ Kubernetes
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