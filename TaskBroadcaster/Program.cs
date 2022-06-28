using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "rabbitmq" };
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare(
        queue: "TaskQueue",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    while (true)
    {
        Guid messageGuid = Guid.NewGuid();
        string message = $"Hello world from task broadcaster. Message GUID: {messageGuid}";
        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        
        channel.BasicPublish(exchange: "",
            routingKey: "TaskQueue",
            basicProperties: properties,
            body: body);
    
        Console.WriteLine($"[TaskBroadcaster] Sent hello world w/ guid: {messageGuid} at {DateTime.Now.ToString()}");
        System.Threading.Thread.Sleep(1000);
    }
}