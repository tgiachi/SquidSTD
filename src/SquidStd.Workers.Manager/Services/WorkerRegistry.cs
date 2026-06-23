using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Abstractions.Types;
using SquidStd.Workers.Manager.Data.Config;
using SquidStd.Workers.Manager.Data.Events;
using SquidStd.Workers.Manager.Interfaces;

namespace SquidStd.Workers.Manager.Services;

/// <summary>
/// In-memory registry of workers, folded from heartbeats. Pure: it returns status transitions for the
/// caller to publish, and never touches the event bus or a real clock (the sweep takes the time as a parameter).
/// </summary>
public sealed class WorkerRegistry : IWorkerRegistry
{
    private readonly Lock _sync = new();
    private readonly Dictionary<string, WorkerInfo> _workers = new(StringComparer.Ordinal);
    private readonly TimeSpan _offlineTimeout;

    public WorkerRegistry(WorkerManagerConfig config)
    {
        var seconds = config.OfflineTimeoutSeconds > 0 ? config.OfflineTimeoutSeconds : 30;
        _offlineTimeout = TimeSpan.FromSeconds(seconds);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<WorkerInfo> GetAll()
    {
        lock (_sync)
        {
            return _workers.Values.ToArray();
        }
    }

    /// <inheritdoc />
    public WorkerInfo? Get(string workerId)
    {
        lock (_sync)
        {
            return _workers.GetValueOrDefault(workerId);
        }
    }

    /// <summary>
    /// Folds a heartbeat into the registry. Returns a transition only when the worker is newly discovered
    /// or has returned from <see cref="WorkerStatusType.Offline" />; otherwise <c>null</c>.
    /// </summary>
    public WorkerStatusChangedEvent? Record(WorkerHeartbeat heartbeat)
    {
        var now = DateTime.UtcNow;

        lock (_sync)
        {
            if (!_workers.TryGetValue(heartbeat.WorkerId, out var existing))
            {
                _workers[heartbeat.WorkerId] = new WorkerInfo(
                    heartbeat.WorkerId,
                    heartbeat.Status,
                    heartbeat.ActiveJobs,
                    heartbeat.MaxConcurrency,
                    now,
                    now);

                return new WorkerStatusChangedEvent(heartbeat.WorkerId, null, heartbeat.Status);
            }

            var old = existing.Status;
            _workers[heartbeat.WorkerId] = existing with
            {
                Status = heartbeat.Status,
                ActiveJobs = heartbeat.ActiveJobs,
                MaxConcurrency = heartbeat.MaxConcurrency,
                LastSeenUtc = now
            };

            return old == WorkerStatusType.Offline && heartbeat.Status != WorkerStatusType.Offline
                ? new WorkerStatusChangedEvent(heartbeat.WorkerId, old, heartbeat.Status)
                : null;
        }
    }

    /// <summary>
    /// Marks workers Offline whose last heartbeat is older than the configured timeout relative to
    /// <paramref name="nowUtc" />. Returns the resulting transitions.
    /// </summary>
    public IReadOnlyList<WorkerStatusChangedEvent> Sweep(DateTime nowUtc)
    {
        var changes = new List<WorkerStatusChangedEvent>();

        lock (_sync)
        {
            foreach (var (id, info) in _workers.ToArray())
            {
                if (info.Status == WorkerStatusType.Offline || nowUtc - info.LastSeenUtc <= _offlineTimeout)
                {
                    continue;
                }

                _workers[id] = info with { Status = WorkerStatusType.Offline };
                changes.Add(new WorkerStatusChangedEvent(id, info.Status, WorkerStatusType.Offline));
            }
        }

        return changes;
    }
}
