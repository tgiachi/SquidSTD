using SquidStd.Core.Data.Bootstrap;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services.Bootstrap;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Extensions;
using SquidStd.Workers.Interfaces;
using SquidStd.Workers.Manager.Extensions;
using SquidStd.Workers.Manager.Interfaces;

var bootstrap = SquidStdBootstrap.Create(new SquidStdOptions
{
    ConfigName = "squidstd",
    RootDirectory = AppContext.BaseDirectory
});

#region step-1
bootstrap.ConfigureServices(c =>
{
    c.AddInMemoryMessaging();
    c.AddWorkers();
    c.AddJobHandler<GreetJobHandler>();
    c.AddWorkerManager();
    return c;
});
#endregion

#region step-3
await bootstrap.StartAsync();

var scheduler = bootstrap.Resolve<IJobScheduler>();
await scheduler.EnqueueAsync("greet", new Dictionary<string, string> { ["name"] = "squid" });

await Task.Delay(500);
await bootstrap.StopAsync();
#endregion

#region step-2
internal sealed class GreetJobHandler : IJobHandler
{
    public string JobName
    {
        get { return "greet"; }
    }

    public Task HandleAsync(JobRequest job, CancellationToken cancellationToken)
    {
        var name = job.Parameters.TryGetValue("name", out var value) ? value : "world";
        Console.WriteLine($"Hello, {name}! (job: {job.JobName})");

        return Task.CompletedTask;
    }
}
#endregion
