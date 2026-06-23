namespace SquidStd.Workers.Manager.Data;

/// <summary>
/// Request body for enqueuing a job via the manager's HTTP surface.
/// </summary>
/// <param name="JobName">Logical name of the job.</param>
/// <param name="Parameters">Optional string key/value arguments; treated as empty when null.</param>
public sealed record EnqueueJobRequest(string JobName, IReadOnlyDictionary<string, string>? Parameters);
