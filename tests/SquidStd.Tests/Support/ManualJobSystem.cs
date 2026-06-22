using SquidStd.Core.Interfaces.Jobs;

namespace SquidStd.Tests.Support;

/// <summary>
/// Single-threaded <see cref="IJobSystem" /> for tests: <see cref="ScheduleAsync(Action, CancellationToken)" />
/// queues the work; call <see cref="RunAll" /> to execute it. This keeps overlap and
/// rescheduling tests fully deterministic.
/// </summary>
public sealed class ManualJobSystem : IJobSystem
{
    private readonly List<Action> _pending = new();

    public int WorkerCount => 1;

    public int PendingCount => _pending.Count;

    public int ActiveCount => 0;

    public long CompletedCount { get; private set; }

    public Task ScheduleAsync(Action work, CancellationToken cancellationToken = default)
    {
        _pending.Add(work);

        return Task.CompletedTask;
    }

    public Task<T> ScheduleAsync<T>(Func<T> work, CancellationToken cancellationToken = default)
    {
        var result = work();
        CompletedCount++;

        return Task.FromResult(result);
    }

    /// <summary>Runs and clears all queued work; returns how many items ran.</summary>
    public int RunAll()
    {
        var snapshot = _pending.ToArray();
        _pending.Clear();

        foreach (var action in snapshot)
        {
            action();
            CompletedCount++;
        }

        return snapshot.Length;
    }

    public void Dispose()
    {
    }
}
