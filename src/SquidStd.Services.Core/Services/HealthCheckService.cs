using System.Diagnostics;
using Serilog;
using SquidStd.Core.Data.Config;
using SquidStd.Core.Data.Health;
using SquidStd.Core.Interfaces.Health;
using SquidStd.Core.Types.Health;

namespace SquidStd.Services.Core.Services;

/// <summary>
/// Runs every registered <see cref="IHealthCheck" /> in parallel with a per-check timeout and
/// exception isolation, then aggregates the results into a single <see cref="HealthReport" />.
/// </summary>
public sealed class HealthCheckService : IHealthCheckService
{
    private readonly ILogger _logger = Log.ForContext<HealthCheckService>();
    private readonly IHealthCheck[] _checks;
    private readonly TimeSpan _checkTimeout;

    public HealthCheckService(IEnumerable<IHealthCheck> checks, HealthCheckOptions options)
    {
        if (options.CheckTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "CheckTimeout must be positive.");
        }

        _checks = [.. checks];
        _checkTimeout = options.CheckTimeout;
    }

    /// <inheritdoc />
    public async ValueTask<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var timestampUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        if (_checks.Length == 0)
        {
            return new HealthReport
            {
                Status = HealthStatus.Healthy,
                Entries = new Dictionary<string, HealthCheckResult>(StringComparer.Ordinal),
                TotalDuration = stopwatch.Elapsed,
                TimestampUtc = timestampUtc
            };
        }

        var tasks = new Task<(string Name, HealthCheckResult Result)>[_checks.Length];

        for (var i = 0; i < _checks.Length; i++)
        {
            tasks[i] = RunCheckAsync(_checks[i], cancellationToken);
        }

        var results = await Task.WhenAll(tasks);

        var entries = new Dictionary<string, HealthCheckResult>(StringComparer.Ordinal);
        var overall = HealthStatus.Healthy;

        foreach (var (name, result) in results)
        {
            var key = name;
            var suffix = 2;

            while (entries.ContainsKey(key))
            {
                key = $"{name}#{suffix}";
                suffix++;
                _logger.Warning("Duplicate health check name '{Name}'; reported as '{Key}'", name, key);
            }

            entries[key] = result;

            if (result.Status == HealthStatus.Unhealthy)
            {
                overall = HealthStatus.Unhealthy;
            }
        }

        return new HealthReport
        {
            Status = overall,
            Entries = entries,
            TotalDuration = stopwatch.Elapsed,
            TimestampUtc = timestampUtc
        };
    }

    private async Task<(string Name, HealthCheckResult Result)> RunCheckAsync(IHealthCheck check, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_checkTimeout);

        try
        {
            var result = await check.CheckAsync(timeoutSource.Token);

            return (check.Name, result with { Duration = stopwatch.Elapsed });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (check.Name, HealthCheckResult.Unhealthy($"Timed out after {_checkTimeout}.") with { Duration = stopwatch.Elapsed });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (check.Name, HealthCheckResult.Unhealthy(ex.Message, ex) with { Duration = stopwatch.Elapsed });
        }
    }
}
