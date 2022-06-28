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

// Attempts to distribute tasks more evenly, can probably tune this on some nodes running a slightly modified
// version of this consumer. Might allow for a somewhat selective allocation of jobs to nodes
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

consumer.Received += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    string message = Encoding.UTF8.GetString(body);

    // Add a random chance of simulating failure - completely exit the process and stop the container if it's high enough
    Random random = new Random();
    int nextRandom = random.Next(0, 100);
    if (nextRandom >= 80)
    {
        Console.WriteLine($"[TaskReceiver] Instance id: {instanceId} simulating task process failure at {DateTime.Now.ToString()}");
        Environment.Exit(1);
    }

    // Add a random sleep to simulate some jobs being tiny and others being massive (e.g. IO-bound workloads)
    Thread.Sleep(nextRandom * 100);
    
    Console.WriteLine($"[TaskReceiver] Instance id: {instanceId} received '{message}' w/ delivery tag of {ea.DeliveryTag} at {DateTime.Now.ToString()}");
    
    // VERY IMPORTANT - MUST manually send an acknowledgement back to RabbitMQ or it'll assume the task failed and re-queue it
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