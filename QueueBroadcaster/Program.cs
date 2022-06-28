using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "rabbitmq" };
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare(
        queue: "HelloQueue",
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    while (true)
    {
        Guid messageGuid = Guid.NewGuid();
        string message = $"Hello world from queue broadcaster. Message GUID: {messageGuid}";
        var body = Encoding.UTF8.GetBytes(message);
        
        channel.BasicPublish(exchange: "",
            routingKey: "HelloQueue",
            basicProperties: null,
            body: body);
    
        Console.WriteLine($"[QueueBroadcaster] Sent hello world w/ guid: {messageGuid} at {DateTime.Now.ToString()}");
        System.Threading.Thread.Sleep(5000);
    }
}