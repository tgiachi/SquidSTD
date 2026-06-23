using DryIoc;
using SquidStd.Core.Data.Config;
using SquidStd.Core.Interfaces.Health;
using SquidStd.Services.Core.Extensions;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Health;

public class HealthCheckRegistrationTests
{
    [Fact]
    public async Task RegisterHealthChecksService_ResolvesAndAggregatesRegisteredChecks()
    {
        using var container = new Container();
        container.RegisterInstance(new HealthCheckOptions());
        container.RegisterInstance<IHealthCheck>(new FakeHealthCheck("a"), IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterInstance<IHealthCheck>(new FakeHealthCheck("b"), IfAlreadyRegistered.AppendNotKeyed);

        container.RegisterHealthChecksService();

        var service = container.Resolve<IHealthCheckService>();
        var report = await service.CheckHealthAsync();

        Assert.Equal(2, report.Entries.Count);
        Assert.True(report.Entries.ContainsKey("a"));
        Assert.True(report.Entries.ContainsKey("b"));
    }
}
