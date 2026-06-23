using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Interfaces;

namespace SquidStd.Tests.Workers.Support;

/// <summary>
/// Configurable <see cref="IJobHandler" /> for tests: records the jobs it received and can be told
/// to throw, or to block until released, so concurrency and error paths can be exercised.
/// </summary>
public sealed class RecordingJobHandler : IJobHandler
{
    private readonly List<JobRequest> _received = [];
    private readonly Lock _sync = new();

    public RecordingJobHandler(string jobName)
    {
        JobName = jobName;
    }

    public string JobName { get; }

    public Exception? ThrowOnHandle { get; set; }

    /// <summary>When set, <see cref="HandleAsync" /> awaits this before returning.</summary>
    public TaskCompletionSource? Gate { get; set; }

    public IReadOnlyList<JobRequest> Received
    {
        get
        {
            lock (_sync)
            {
                return _received.ToArray();
            }
        }
    }

    public async Task HandleAsync(JobRequest job, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _received.Add(job);
        }

        if (ThrowOnHandle is not null)
        {
            throw ThrowOnHandle;
        }

        if (Gate is not null)
        {
            await Gate.Task.WaitAsync(cancellationToken);
        }
    }
}
