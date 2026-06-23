# Health Checks

The health-check aggregator (`IHealthCheckService` in `SquidStd.Core`, implemented in
`SquidStd.Services.Core`) runs every registered `IHealthCheck` and returns a single `HealthReport`.

- Implement `IHealthCheck` (`Name` + `CheckAsync`) and register it as `IHealthCheck`.
- `CheckHealthAsync()` runs all checks **in parallel**, each with a per-check timeout and exception
  isolation — a failing or timed-out check becomes an `Unhealthy` entry without breaking the others.
- The overall `HealthReport.Status` is `Unhealthy` if any check is `Unhealthy`, otherwise `Healthy`.

```csharp
using DryIoc;
using SquidStd.Core.Data.Health;
using SquidStd.Core.Interfaces.Health;
using SquidStd.Services.Core.Extensions;

container.RegisterHealthChecksService();

var health = container.Resolve<IHealthCheckService>();
HealthReport report = await health.CheckHealthAsync();

if (report.Status == SquidStd.Core.Types.Health.HealthStatus.Unhealthy)
{
    foreach (var (name, result) in report.Entries)
    {
        Console.WriteLine($"{name}: {result.Status} {result.Description}");
    }
}
```
