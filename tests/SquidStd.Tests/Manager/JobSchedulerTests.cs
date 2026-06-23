using DryIoc;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Workers.Abstractions;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Services;

namespace SquidStd.Tests.Manager;

public class JobSchedulerTests
{
    [Fact]
    public async Task EnqueueAsync_PublishesJobRequestToConfiguredQueue()
    {
        var container = new Container();
        container.RegisterInstance<IEventBus>(new EventBusService());
        container.AddInMemoryMessaging();
        var queue = container.Resolve<IMessageQueue>();

        var received = new TaskCompletionSource<JobRequest>();
        using var _ = queue.Subscribe<JobRequest>(
            WorkerChannels.JobQueue,
            new DelegateListener(job => received.TrySetResult(job)));

        var scheduler = new JobScheduler(queue, new WorkerManagerConfig());
        await scheduler.EnqueueAsync("resize", new Dictionary<string, string> { ["w"] = "100" });

        var job = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("resize", job.JobName);
        Assert.Equal("100", job.Parameters["w"]);
    }

    private sealed class DelegateListener : IQueueMessageListenerAsync<JobRequest>
    {
        private readonly Action<JobRequest> _onMessage;

        public DelegateListener(Action<JobRequest> onMessage)
        {
            _onMessage = onMessage;
        }

        public Task HandleAsync(JobRequest message, CancellationToken cancellationToken)
        {
            _onMessage(message);

            return Task.CompletedTask;
        }
    }
}
