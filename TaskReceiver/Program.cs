using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

AutoResetEvent waitHandle = new AutoResetEvent(false);

var instanceId = Guid.NewGuid();
var factory = new ConnectionFactory { HostName = "rabbitmq" };
var connection = factory.CreateConnection();
var channel = connection.CreateModel();
var consumer = new EventingBasicConsumer(channel);

channel.QueueDeclare(queue: "TaskQueue",
    durable: true, // If the consumer dies - the task will not be lost, still lost if server keels over
    exclusive: false,
    autoDelete: false,
    arguments: null);

channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    string message = Encoding.UTF8.GetString(body);
    
    Thread.Sleep(3000);

    Random random = new Random();
    int nextRandom = random.Next(0, 100);

    if (nextRandom >= 80)
    {
        Console.WriteLine($"[TaskReceiver] Instance id: {instanceId} simulating task process failure at {DateTime.Now.ToString()}");
        Environment.Exit(1);
    }
    
    Console.WriteLine($"[TaskReceiver] Instance id: {instanceId} received '{message}' w/ delivery tag of {ea.DeliveryTag} at {DateTime.Now.ToString()}");
    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
};

consumer.Shutdown += (model, ea) => 
    Console.WriteLine($"[TaskReceiver] Instance id: {instanceId} shutting down at {DateTime.Now.ToString()}");
    
consumer.Registered += (model, ea) => 
    Console.WriteLine($"[TaskReceiver] Instance id: {instanceId} registered at {DateTime.Now.ToString()}");
    
// Running consumer within Task.Run to prevent immediate exit
Task.Run(() =>
{
    channel.BasicConsume(
        queue: "TaskQueue",
        autoAck: false, // Disabling auto ack so tasks can be re-queued if they fail
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