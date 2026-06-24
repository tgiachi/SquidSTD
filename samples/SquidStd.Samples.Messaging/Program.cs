using SquidStd.Core.Data.Bootstrap;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services.Bootstrap;

var bootstrap = SquidStdBootstrap.Create(new SquidStdOptions
{
    ConfigName = "squidstd",
    RootDirectory = AppContext.BaseDirectory
});

#region step-1
bootstrap.ConfigureServices(container => container.AddInMemoryMessaging());
#endregion

await bootstrap.StartAsync();

#region step-2
var queue = bootstrap.Resolve<IMessageQueue>();
using (queue.Subscribe<OrderPlaced>("orders", new OrderListener()))
{
    await queue.PublishAsync("orders", new OrderPlaced("order-1"));
    await Task.Delay(200);
}
#endregion

#region step-3
var topic = bootstrap.Resolve<IMessageTopic>();
using (topic.Subscribe<OrderPlaced>("order-events", (order, _) =>
{
    Console.WriteLine($"topic saw {order.Id}");

    return Task.CompletedTask;
}))
{
    await topic.PublishAsync("order-events", new OrderPlaced("order-2"));
    await Task.Delay(200);
}
#endregion

await bootstrap.StopAsync();

/// <summary>A sample message.</summary>
public sealed record OrderPlaced(string Id);

/// <summary>Competing-consumers listener for the orders queue.</summary>
public sealed class OrderListener : IQueueMessageListenerAsync<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"queue handled {message.Id}");

        return Task.CompletedTask;
    }
}
