using SquidStd.Core.Interfaces.Jobs;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Services.Core;

public class JobSystemServiceTests
{
    [Fact]
    public async Task ScheduleAsync_Action_RunsAndCompletes()
    {
        using var jobs = NewService(1);
        IJobSystem system = jobs;
        var called = false;
        await jobs.StartAsync(CancellationToken.None);

        await system.ScheduleAsync(() => called = true);

        Assert.True(called);
        await WaitForCompletedAsync(system, 1);
        Assert.Equal(1, system.CompletedCount);
    }

    [Fact]
    public async Task ScheduleAsync_Func_ReturnsValue()
    {
        using var jobs = NewService(2);
        IJobSystem system = jobs;
        await jobs.StartAsync(CancellationToken.None);

        var result = await system.ScheduleAsync(() => 42);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ScheduleAsync_ManyJobs_AllComplete()
    {
        using var jobs = NewService(4);
        IJobSystem system = jobs;
        var sum = 0;
        var sync = new Lock();
        var tasks = new List<Task>();
        await jobs.StartAsync(CancellationToken.None);

        for (var i = 1; i <= 100; i++)
        {
            var value = i;
            tasks.Add(
                system.ScheduleAsync(
                    () =>
                    {
                        lock (sync)
                        {
                            sum += value;
                        }
                    }
                )
            );
        }

        await Task.WhenAll(tasks);

        Assert.Equal(5050, sum);
        await WaitForCompletedAsync(system, 100);
        Assert.Equal(100, system.CompletedCount);
    }

    [Fact]
    public async Task ScheduleAsync_ThrowingAction_PropagatesExceptionToAwaiter()
    {
        using var jobs = NewService(1);
        IJobSystem system = jobs;
        await jobs.StartAsync(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => system.ScheduleAsync(() => throw new InvalidOperationException("boom"))
        );
    }

    [Fact]
    public async Task ScheduleAsync_TokenAlreadyCancelled_ReturnsCanceledTask()
    {
        using var jobs = NewService(1);
        IJobSystem system = jobs;
        using var cancellationTokenSource = new CancellationTokenSource();
        await jobs.StartAsync(CancellationToken.None);
        await cancellationTokenSource.CancelAsync();

        var task = system.ScheduleAsync(() => { }, cancellationTokenSource.Token);

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        Assert.Equal(TaskStatus.Canceled, task.Status);
    }

    [Fact]
    public async Task ScheduleAsync_TokenCancelledBeforePickup_TransitionsToCanceled()
    {
        using var jobs = NewService(1);
        IJobSystem system = jobs;
        using var gate = new ManualResetEventSlim(false);
        using var firstStarted = new ManualResetEventSlim(false);
        using var cancellationTokenSource = new CancellationTokenSource();
        await jobs.StartAsync(CancellationToken.None);

        _ = system.ScheduleAsync(
            () =>
            {
                firstStarted.Set();
                gate.Wait(TimeSpan.FromSeconds(2));
            }
        );
        Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(2)), "first job did not start");

        var task = system.ScheduleAsync(() => Assert.Fail("queued job should not run"), cancellationTokenSource.Token);
        await cancellationTokenSource.CancelAsync();
        gate.Set();

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        Assert.Equal(TaskStatus.Canceled, task.Status);
    }

    [Fact]
    public async Task StopAsync_CancelsQueuedJobsAndPreventsNewSchedules()
    {
        var jobs = NewService(1);
        IJobSystem system = jobs;
        using var gate = new ManualResetEventSlim(false);
        using var firstStarted = new ManualResetEventSlim(false);
        await jobs.StartAsync(CancellationToken.None);

        _ = system.ScheduleAsync(
            () =>
            {
                firstStarted.Set();
                gate.Wait(TimeSpan.FromSeconds(2));
            }
        );
        Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(2)), "first job did not start");
        var queued = system.ScheduleAsync(() => Assert.Fail("queued job should not run"));

        await jobs.StopAsync(CancellationToken.None);
        gate.Set();

        await Assert.ThrowsAsync<TaskCanceledException>(() => queued);
        Assert.Throws<ObjectDisposedException>(
            () =>
            {
                _ = system.ScheduleAsync(() => { });
            }
        );
        jobs.Dispose();
    }

    [Fact]
    public void WorkerCount_AutoDetectsAtLeastOne()
    {
        using var jobs = NewService(0);

        Assert.True(jobs.WorkerCount >= 1);
    }

    [Fact]
    public void WorkerCount_UsesExplicitValue()
    {
        using var jobs = NewService(3);

        Assert.Equal(3, jobs.WorkerCount);
    }

    private static JobSystemService NewService(int workerCount)
        => new(
            new()
            {
                WorkerThreadCount = workerCount,
                ShutdownTimeoutSeconds = 1.0
            }
        );

    // CompletedCount is incremented by the worker after the scheduled task's completion fires, so
    // awaiting ScheduleAsync does not guarantee the counter is updated yet. Wait for it (bounded).
    private static async Task WaitForCompletedAsync(IJobSystem system, long expected)
    {
        for (var i = 0; i < 200 && system.CompletedCount < expected; i++)
        {
            await Task.Delay(10);
        }
    }
}
