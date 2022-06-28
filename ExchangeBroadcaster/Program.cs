using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "rabbitmq" };
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    // tl;dr on exchange types;
    // "Direct" - message goes to the queues where the binding key (consumer) matches the routing key (publisher)
    // "Topic" - message goes to one or many queues where the binding key (consumer) matches the routing key (publisher)
    // "Fanout" - anarchy, message goes to all queues regardless of binding/routing keys
    // "Headers" - message goes to the queues where the headers match rather than the keys
    channel.ExchangeDeclare("HelloExchange", ExchangeType.Fanout);

    while (true)
    {
        Guid messageGuid = Guid.NewGuid();
        string message = $"Hello world from exchange broadcaster. Message GUID: {messageGuid}";
        var body = Encoding.UTF8.GetBytes(message);
        
        channel.BasicPublish(exchange: "HelloExchange",
            routingKey: "",
            basicProperties: null,
            body: body);
    
        Console.WriteLine($"[ExchangeBroadcaster] Sent hello world w/ guid: {messageGuid} at {DateTime.Now.ToString()}");
        System.Threading.Thread.Sleep(5000);
    }
}