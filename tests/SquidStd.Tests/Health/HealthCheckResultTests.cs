using SquidStd.Core.Data.Health;
using SquidStd.Core.Types.Health;

namespace SquidStd.Tests.Health;

public class HealthCheckResultTests
{
    [Fact]
    public void Healthy_SetsStatusAndDescription()
    {
        var result = HealthCheckResult.Healthy("ok");

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("ok", result.Description);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Unhealthy_SetsStatusDescriptionAndException()
    {
        var ex = new InvalidOperationException("boom");
        var result = HealthCheckResult.Unhealthy("down", ex);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("down", result.Description);
        Assert.Same(ex, result.Exception);
    }
}
