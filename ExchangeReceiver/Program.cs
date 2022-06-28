using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

AutoResetEvent waitHandle = new AutoResetEvent(false); // Weirdness required for preventing immediate exit within container, can ignore

var instanceId = Guid.NewGuid();
var factory = new ConnectionFactory { HostName = "rabbitmq" };
var connection = factory.CreateConnection();
var channel = connection.CreateModel();
var consumer = new EventingBasicConsumer(channel);

channel.ExchangeDeclare(exchange: "HelloExchange", type: ExchangeType.Fanout);

// Bind to the 'HelloExchange' exchange, effectively adding another consumer to the pool.
// Using .QueueName here to make the package generate a queue name for us, we're using the fanout type
// for the exchange so doesn't matter what the queue name is, this receiver will always get the message
var queueName = channel.QueueDeclare().QueueName;
channel.QueueBind(queue: queueName,
        exchange: "HelloExchange",
        routingKey: "");

// This particular RabbitMQ package is awesome and uses C# events, register handlers here
consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    string message = Encoding.UTF8.GetString(body);
    
    Console.WriteLine($"[ExchangeReceiver] Instance id: {instanceId} received '{message}' at {DateTime.Now.ToString()}");
};

consumer.Shutdown += (model, ea) =>  
    Console.WriteLine($"[ExchangeReceiver] Instance id: {instanceId} shutting down at {DateTime.Now.ToString()}");

consumer.Registered += (model, ea) =>
    Console.WriteLine($"[ExchangeReceiver] Instance id: {instanceId} registered at {DateTime.Now.ToString()}");

// Running consumer within Task.Run to prevent immediate exit
Task.Run(() =>
{
    channel.BasicConsume(
        queue: queueName,
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
