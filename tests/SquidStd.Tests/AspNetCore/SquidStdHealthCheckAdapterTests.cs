using Microsoft.Extensions.Diagnostics.HealthChecks;
using SquidStd.AspNetCore.Services;
using SquidStd.Tests.Support;
using SquidHealthResult = SquidStd.Core.Data.Health.HealthCheckResult;

namespace SquidStd.Tests.AspNetCore;

public class SquidStdHealthCheckAdapterTests
{
    [Fact]
    public async Task CheckHealthAsync_MapsHealthy()
    {
        var adapter = new SquidStdHealthCheckAdapter(new FakeHealthCheck("ok", SquidHealthResult.Healthy("all good")));

        var result = await adapter.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("all good", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_MapsUnhealthy_WithDescriptionAndException()
    {
        var ex = new InvalidOperationException("boom");
        var adapter = new SquidStdHealthCheckAdapter(new FakeHealthCheck("bad", SquidHealthResult.Unhealthy("down", ex)));

        var result = await adapter.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("down", result.Description);
        Assert.Same(ex, result.Exception);
    }
}
