using DryIoc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using SquidStd.AspNetCore.Extensions;
using SquidStd.Tests.Support;
using SquidHealthCheck = SquidStd.Core.Interfaces.Health.IHealthCheck;
using SquidHealthResult = SquidStd.Core.Data.Health.HealthCheckResult;

namespace SquidStd.Tests.AspNetCore;

public class SquidStdHealthChecksBridgeTests
{
    [Fact]
    public async Task AddSquidStdHealthChecks_BridgesEachCheckToStandardReport()
    {
        using var temp = new TempDirectory();
        var builder = CreateBuilder(temp.Path);
        builder.UseSquidStd(
            options => options.ConfigName = "app",
            container =>
            {
                container.RegisterInstance<SquidHealthCheck>(new FakeHealthCheck("alpha"), IfAlreadyRegistered.AppendNotKeyed);
                container.RegisterInstance<SquidHealthCheck>(
                    new FakeHealthCheck("beta", SquidHealthResult.Unhealthy("bad")),
                    IfAlreadyRegistered.AppendNotKeyed
                );

                return container;
            }
        );
        builder.AddSquidStdHealthChecks();

        await using var app = builder.Build();
        var health = app.Services.GetRequiredService<HealthCheckService>();
        var report = await health.CheckHealthAsync();

        Assert.True(report.Entries.ContainsKey("alpha"));
        Assert.True(report.Entries.ContainsKey("beta"));
        Assert.Equal(HealthStatus.Healthy, report.Entries["alpha"].Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Entries["beta"].Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
    }

    [Fact]
    public void AddSquidStdHealthChecks_WithoutUseSquidStd_Throws()
    {
        using var temp = new TempDirectory();
        var builder = CreateBuilder(temp.Path);

        Assert.Throws<InvalidOperationException>(() => builder.AddSquidStdHealthChecks());
    }

    private static WebApplicationBuilder CreateBuilder(string contentRootPath)
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                ContentRootPath = contentRootPath,
                EnvironmentName = Environments.Development
            }
        );

        builder.WebHost.UseTestServer();

        return builder;
    }
}
