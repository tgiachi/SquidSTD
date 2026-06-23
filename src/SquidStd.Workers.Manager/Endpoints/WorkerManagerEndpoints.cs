using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SquidStd.Workers.Abstractions.Data;
using SquidStd.Workers.Manager.Data;
using SquidStd.Workers.Manager.Interfaces;

namespace SquidStd.Workers.Manager.Endpoints;

/// <summary>
/// Typed minimal-API handlers for the worker manager surface. Static so they can be unit-tested directly.
/// </summary>
public static class WorkerManagerEndpoints
{
    /// <summary>Lists all known workers.</summary>
    public static Ok<IReadOnlyCollection<WorkerInfo>> GetWorkers(IWorkerRegistry registry)
        => TypedResults.Ok(registry.GetAll());

    /// <summary>Returns a single worker, or 404 when unknown.</summary>
    public static Results<Ok<WorkerInfo>, NotFound> GetWorker(string id, IWorkerRegistry registry)
    {
        var info = registry.Get(id);

        return info is null ? TypedResults.NotFound() : TypedResults.Ok(info);
    }

    /// <summary>Enqueues a job; 400 on a blank name, 503 when the queue is unavailable.</summary>
    public static async Task<Results<Accepted, BadRequest<string>, ProblemHttpResult>> EnqueueJob(
        EnqueueJobRequest request,
        IJobScheduler scheduler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.JobName))
        {
            return TypedResults.BadRequest("JobName is required.");
        }

        try
        {
            await scheduler.EnqueueAsync(request.JobName, request.Parameters ?? new Dictionary<string, string>(), cancellationToken);

            return TypedResults.Accepted((string?)null);
        }
        catch (Exception)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Job queue unavailable.");
        }
    }
}
