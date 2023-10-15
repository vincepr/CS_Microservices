using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlatformService.Dtos;
using RabbitMQ.Client;

namespace PlatformService.AsyncDataServices;

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