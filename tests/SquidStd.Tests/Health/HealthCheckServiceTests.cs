using SquidStd.Core.Data.Config;
using SquidStd.Core.Data.Health;
using SquidStd.Core.Interfaces.Health;
using SquidStd.Core.Types.Health;
using SquidStd.Services.Core.Services;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Health;

public class HealthCheckServiceTests
{
    private static HealthCheckService NewService(HealthCheckOptions options, params IHealthCheck[] checks)
        => new(checks, options);

    private static HealthCheckOptions Options(double timeoutSeconds = 5)
        => new() { CheckTimeout = TimeSpan.FromSeconds(timeoutSeconds) };

    [Fact]
    public async Task NoChecks_ReturnsHealthyEmptyReport()
    {
        var report = await NewService(Options()).CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Empty(report.Entries);
    }

    [Fact]
    public async Task AllHealthy_ReportsHealthyWithEntries()
    {
        var service = NewService(Options(), new FakeHealthCheck("a"), new FakeHealthCheck("b"));

        var report = await service.CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal(2, report.Entries.Count);
        Assert.Equal(HealthStatus.Healthy, report.Entries["a"].Status);
    }

    [Fact]
    public async Task OneUnhealthy_MakesOverallUnhealthy()
    {
        var service = NewService(
            Options(),
            new FakeHealthCheck("ok"),
            new FakeHealthCheck("bad", HealthCheckResult.Unhealthy("nope"))
        );

        var report = await service.CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Entries["bad"].Status);
        Assert.Equal(HealthStatus.Healthy, report.Entries["ok"].Status);
    }

    [Fact]
    public async Task CheckThatThrows_IsCapturedAsUnhealthy_AndDoesNotBreakOthers()
    {
        var service = NewService(
            Options(),
            new FakeHealthCheck("boom", throwException: new InvalidOperationException("kaboom")),
            new FakeHealthCheck("ok")
        );

        var report = await service.CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Entries["boom"].Status);
        Assert.Equal("kaboom", report.Entries["boom"].Description);
        Assert.NotNull(report.Entries["boom"].Exception);
        Assert.Equal(HealthStatus.Healthy, report.Entries["ok"].Status);
    }

    [Fact]
    public async Task CheckThatExceedsTimeout_IsUnhealthy_OthersUnaffected()
    {
        var service = NewService(
            Options(timeoutSeconds: 0.05),
            new FakeHealthCheck("slow", delay: TimeSpan.FromSeconds(2)),
            new FakeHealthCheck("fast")
        );

        var report = await service.CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.Entries["slow"].Status);
        Assert.Contains("imed out", report.Entries["slow"].Description);
        Assert.Equal(HealthStatus.Healthy, report.Entries["fast"].Status);
    }

    [Fact]
    public async Task DuplicateNames_AreMadeUnique()
    {
        var service = NewService(Options(), new FakeHealthCheck("db"), new FakeHealthCheck("db"));

        var report = await service.CheckHealthAsync();

        Assert.Equal(2, report.Entries.Count);
        Assert.True(report.Entries.ContainsKey("db"));
        Assert.True(report.Entries.ContainsKey("db#2"));
    }

    [Fact]
    public async Task ExternalCancellation_Propagates()
    {
        var service = NewService(Options(), new FakeHealthCheck("slow", delay: TimeSpan.FromSeconds(2)));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await service.CheckHealthAsync(cts.Token));
    }

    [Fact]
    public void Ctor_NonPositiveTimeout_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HealthCheckService([], new HealthCheckOptions { CheckTimeout = TimeSpan.Zero })
        );
    }
}
