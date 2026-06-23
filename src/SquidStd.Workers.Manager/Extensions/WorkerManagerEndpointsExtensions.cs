using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SquidStd.Workers.Manager.Endpoints;

namespace SquidStd.Workers.Manager.Extensions;

/// <summary>
/// Maps the opt-in worker manager HTTP endpoints.
/// </summary>
public static class WorkerManagerEndpointsExtensions
{
    /// <summary>
    /// Maps <c>GET /workers</c>, <c>GET /workers/{id}</c>, and <c>POST /jobs</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapWorkerManagerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/workers", WorkerManagerEndpoints.GetWorkers);
        endpoints.MapGet("/workers/{id}", WorkerManagerEndpoints.GetWorker);
        endpoints.MapPost("/jobs", WorkerManagerEndpoints.EnqueueJob);

        return endpoints;
    }
}
