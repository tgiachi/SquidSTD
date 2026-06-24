using SquidStd.Core.Data.Bootstrap;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Interfaces.Jobs;
using SquidStd.Core.Interfaces.Scheduling;
using SquidStd.Services.Core.Extensions;
using SquidStd.Services.Core.Services.Bootstrap;

var bootstrap = SquidStdBootstrap.Create(new SquidStdOptions
{
    ConfigName = "squidstd",
    RootDirectory = AppContext.BaseDirectory
});

// The cron scheduler and timer wheel are opt-in.
bootstrap.ConfigureServices(container => container.RegisterSchedulerServices());

await bootstrap.StartAsync();

#region step-1
var eventBus = bootstrap.Resolve<IEventBus>();
eventBus.RegisterAsyncListener(new PingListener());
await eventBus.PublishAsync(new PingEvent("hello"), CancellationToken.None);
#endregion

#region step-2
var jobs = bootstrap.Resolve<IJobSystem>();
await jobs.ScheduleAsync(() => Console.WriteLine("job ran on a worker thread"));
#endregion

#region step-3
var cron = bootstrap.Resolve<ICronScheduler>();
cron.Schedule("heartbeat", "*/5 * * * *", _ =>
{
    Console.WriteLine("cron tick");

    return Task.CompletedTask;
});
#endregion

await bootstrap.StopAsync();

/// <summary>A sample event published on the bus.</summary>
public sealed record PingEvent(string Message) : IEvent;

/// <summary>Handles <see cref="PingEvent" />.</summary>
public sealed class PingListener : IAsyncEventListener<PingEvent>
{
    public Task HandleAsync(PingEvent eventData, CancellationToken cancellationToken)
    {
        Console.WriteLine($"received: {eventData.Message}");

        return Task.CompletedTask;
    }
}
