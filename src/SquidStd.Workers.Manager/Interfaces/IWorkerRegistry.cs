using SquidStd.Workers.Abstractions.Data;

namespace SquidStd.Workers.Manager.Interfaces;

/// <summary>
/// Read access to the manager's in-memory view of known workers.
/// </summary>
public interface IWorkerRegistry
{
    /// <summary>Returns a snapshot of all known workers.</summary>
    IReadOnlyCollection<WorkerInfo> GetAll();

    /// <summary>Returns the worker with the given id, or <c>null</c> when unknown.</summary>
    WorkerInfo? Get(string workerId);
}
