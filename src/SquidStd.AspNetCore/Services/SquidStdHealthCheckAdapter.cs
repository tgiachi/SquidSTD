using Microsoft.Extensions.Diagnostics.HealthChecks;
using SquidHealthCheck = SquidStd.Core.Interfaces.Health.IHealthCheck;
using SquidHealthStatus = SquidStd.Core.Types.Health.HealthStatus;

namespace SquidStd.AspNetCore.Services;

/// <summary>
/// Adapts a SquidStd <see cref="SquidHealthCheck" /> to the standard ASP.NET Core
/// <see cref="IHealthCheck" /> contract.
/// </summary>
internal sealed class SquidStdHealthCheckAdapter : IHealthCheck
{
    private readonly SquidHealthCheck _check;

    public SquidStdHealthCheckAdapter(SquidHealthCheck check)
    {
        _check = check;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _check.CheckAsync(cancellationToken);

        return result.Status == SquidHealthStatus.Unhealthy
                   ? HealthCheckResult.Unhealthy(result.Description, result.Exception)
                   : HealthCheckResult.Healthy(result.Description);
    }
}
