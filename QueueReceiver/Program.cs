using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

AutoResetEvent waitHandle = new AutoResetEvent(false);

var instanceId = Guid.NewGuid();
var factory = new ConnectionFactory { HostName = "rabbitmq" };
var connection = factory.CreateConnection();
var channel = connection.CreateModel();
var consumer = new EventingBasicConsumer(channel);

channel.QueueDeclare(queue: "HelloQueue",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null);

consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    string message = Encoding.UTF8.GetString(body);
    
    Console.WriteLine($"[QueueReceiver] Instance id: {instanceId} received '{message}' at {DateTime.Now.ToString()}");
};

consumer.Shutdown += (model, ea) => 
    Console.WriteLine($"[QueueReceiver] Instance id: {instanceId} shutting down at {DateTime.Now.ToString()}");
    
consumer.Registered += (model, ea) => 
    Console.WriteLine($"[QueueReceiver] Instance id: {instanceId} registered at {DateTime.Now.ToString()}");
    
// Running consumer within Task.Run to prevent immediate exit
Task.Run(() =>
{
    channel.BasicConsume(
        queue: "HelloQueue",
        autoAck: true,
        consumer
    );
});

// Keypress handler required otherwise docker can't stop the process without SIGKILL
Console.CancelKeyPress += (o, e) =>
{
    Console.WriteLine("Exiting...");
    waitHandle.Set();
};

waitHandle.WaitOne();