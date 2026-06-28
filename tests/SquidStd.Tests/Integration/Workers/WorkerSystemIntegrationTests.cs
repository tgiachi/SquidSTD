using DryIoc;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Messaging.Abstractions.Interfaces;
using SquidStd.Messaging.RabbitMq.Data.Config;
using SquidStd.Messaging.RabbitMq.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Tests.Messaging.RabbitMq;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Data.Config;
using SquidStd.Workers.Interfaces;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Services;
using SquidStd.Workers.Services;

namespace SquidStd.Tests.Integration.Workers;

/// <summary>
///     End-to-end test of the workers system over a real RabbitMQ broker (Testcontainers): the manager
///     enqueues a job that the worker consumes and runs (real queue), and the worker's heartbeat reaches the
///     manager's registry (real fan-out topic).
/// </summary>
[Collection(RabbitMqCollection.Name)]
public class WorkerSystemIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly RabbitMqContainerFixture _fixture;

    public WorkerSystemIntegrationTests(RabbitMqContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Manager_EnqueuesJob_WorkerRunsIt_AndHeartbeatReachesRegistry()
    {
        using var container = new Container();
        container.RegisterInstance<IEventBus>(new EventBusService());
        container.AddRabbitMqMessaging(new RabbitMqOptions { Uri = new Uri(_fixture.AmqpUri) });

        // Unique channel names per run so parallel/other tests on the shared broker do not interfere.
        var suffix = Guid.NewGuid().ToString("N");
        var jobQueue = $"it.jobs.{suffix}";
        var heartbeatTopic = $"it.hb.{suffix}";

        var workersConfig = new WorkersConfig
        {
            WorkerId = "it-worker",
            MaxConcurrency = 2,
            HeartbeatIntervalSeconds = 1,
            JobQueue = jobQueue,
            HeartbeatTopic = heartbeatTopic
        };
        var managerConfig = new WorkerManagerConfig
        {
            JobQueue = jobQueue,
            HeartbeatTopic = heartbeatTopic
        };

        // Open the RabbitMQ connections.
        var queueProvider = container.Resolve<IQueueProvider>();
        var topicProvider = container.Resolve<ITopicProvider>();
        await queueProvider.StartAsync();
        await topicProvider.StartAsync();

        var queue = container.Resolve<IMessageQueue>();
        var topic = container.Resolve<IMessageTopic>();
        var eventBus = container.Resolve<IEventBus>();

        // Worker side.
        var jobRan = new TaskCompletionSource<JobRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new CapturingJobHandler("greet", jobRan);
        var workerState = new WorkerState(workersConfig);
        var dispatcher = new JobDispatcher([handler]);
        var consumer = new WorkerConsumerService(queue, dispatcher, workerState, workersConfig);
        var heartbeatService = new WorkerHeartbeatService(topic, workerState, workersConfig);

        // Manager side.
        var registry = new WorkerRegistry(managerConfig);
        var collector = new HeartbeatCollectorService(topic, registry, eventBus, managerConfig);
        var scheduler = new JobScheduler(queue, managerConfig);

        try
        {
            await collector.StartAsync();
            await consumer.StartAsync();
            await heartbeatService.StartAsync();

            // Let the broker register the queue consumer and the topic binding before publishing.
            await Task.Delay(500);

            await scheduler.EnqueueAsync("greet", new Dictionary<string, string> { ["name"] = "squid" });

            // The worker consumed the job from the real queue and ran the handler.
            var ranJob = await jobRan.Task.WaitAsync(Timeout);
            Assert.Equal("greet", ranJob.JobName);
            Assert.Equal("squid", ranJob.Parameters["name"]);

            // The worker's heartbeat reached the manager's registry over the real topic.
            var seen = await PollForWorker(registry, "it-worker");
            Assert.NotNull(seen);
            Assert.Equal("it-worker", seen!.WorkerId);
            Assert.Equal(2, seen.MaxConcurrency);
        }
        finally
        {
            await heartbeatService.StopAsync();
            await consumer.StopAsync();
            await collector.StopAsync();
            await topicProvider.StopAsync();
            await queueProvider.StopAsync();
        }
    }

    private static async Task<WorkerInfo?> PollForWorker(WorkerRegistry registry, string workerId)
    {
        var deadline = DateTime.UtcNow + Timeout;

        while (DateTime.UtcNow < deadline)
        {
            var info = registry.Get(workerId);

            if (info is not null)
            {
                return info;
            }

            await Task.Delay(200);
        }

        return null;
    }

    private sealed class CapturingJobHandler : IJobHandler
    {
        private readonly TaskCompletionSource<JobRequest> _completion;

        public string JobName { get; }

        public CapturingJobHandler(string jobName, TaskCompletionSource<JobRequest> completion)
        {
            JobName = jobName;
            _completion = completion;
        }

        public Task HandleAsync(JobRequest job, CancellationToken cancellationToken)
        {
            _completion.TrySetResult(job);

            return Task.CompletedTask;
        }
    }
}
