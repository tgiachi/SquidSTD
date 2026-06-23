namespace SquidStd.Workers.Abstractions.Data;

/// <summary>
/// A unit of work the manager enqueues on the jobs queue for a worker to execute.
/// </summary>
/// <param name="JobName">Logical name of the job, used by the worker to pick a handler.</param>
/// <param name="Parameters">Opaque string key/value arguments for the job.</param>
public sealed record JobRequest(string JobName, IReadOnlyDictionary<string, string> Parameters);
