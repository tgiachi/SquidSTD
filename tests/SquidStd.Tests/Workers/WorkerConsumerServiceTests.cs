using SquidStd.Tests.Workers.Support;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Data.Config;
using SquidStd.Workers.Services;

namespace SquidStd.Tests.Workers;

public class WorkerConsumerServiceTests
{
    private static JobRequest Job(string name)
        => new(name, new Dictionary<string, string>());

    private static WorkerConsumerService Build(WorkerState state, params RecordingJobHandler[] handlers)
        => new(new FakeMessageQueue(), new JobDispatcher(handlers), state, new WorkersConfig());

    [Fact]
    public async Task HandleAsync_RunsMatchingHandler()
    {
        var handler = new RecordingJobHandler("resize");
        var state = new WorkerState(new() { WorkerId = "w1", MaxConcurrency = 2 });
        var consumer = Build(state, handler);

        await consumer.HandleAsync(Job("resize"), CancellationToken.None);

        Assert.Single(handler.Received);
        Assert.Equal(0, state.ActiveJobs);
    }

    [Fact]
    public async Task HandleAsync_DropsUnknownJobWithoutThrowing()
    {
        var state = new WorkerState(new() { WorkerId = "w1", MaxConcurrency = 2 });
        var consumer = Build(state, new RecordingJobHandler("resize"));

        var ex = await Record.ExceptionAsync(() => consumer.HandleAsync(Job("unknown"), CancellationToken.None));

        Assert.Null(ex);
        Assert.Equal(0, state.ActiveJobs);
    }

    [Fact]
    public async Task HandleAsync_RethrowsHandlerExceptionForRequeue()
    {
        var handler = new RecordingJobHandler("resize") { ThrowOnHandle = new InvalidOperationException("boom") };
        var state = new WorkerState(new() { WorkerId = "w1", MaxConcurrency = 2 });
        var consumer = Build(state, handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => consumer.HandleAsync(Job("resize"), CancellationToken.None));

        Assert.Equal(0, state.ActiveJobs);
    }

    [Fact]
    public async Task HandleAsync_NeverExceedsMaxConcurrency()
    {
        var handler = new RecordingJobHandler("resize") { Gate = new TaskCompletionSource() };
        var state = new WorkerState(new() { WorkerId = "w1", MaxConcurrency = 2 });
        var consumer = Build(state, handler);

        // Launch 5 concurrent dispatches against a handler that blocks on its gate.
        var inFlight = Enumerable.Range(0, 5)
            .Select(_ => consumer.HandleAsync(Job("resize"), CancellationToken.None))
            .ToArray();

        // Give the semaphore time to admit as many as it will, then assert the cap held.
        await Task.Delay(200);
        Assert.True(state.ActiveJobs <= 2, $"ActiveJobs was {state.ActiveJobs}, expected <= 2");

        // Release the gate so everything drains.
        handler.Gate!.SetResult();
        await Task.WhenAll(inFlight);
        Assert.Equal(0, state.ActiveJobs);
        Assert.Equal(5, handler.Received.Count);
    }
}
