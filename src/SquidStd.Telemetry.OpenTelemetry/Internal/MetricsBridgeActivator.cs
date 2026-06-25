using Microsoft.Extensions.Hosting;
using SquidStd.Telemetry.OpenTelemetry.Services;

namespace SquidStd.Telemetry.OpenTelemetry.Internal;

/// <summary>
/// Warms the <see cref="MetricsSnapshotBridge" /> at host start so its observable instruments exist
/// for the MeterProvider's first collection (ASP.NET Core host path).
/// </summary>
internal sealed class MetricsBridgeActivator : IHostedService
{
    private readonly MetricsSnapshotBridge _bridge;

    public MetricsBridgeActivator(MetricsSnapshotBridge bridge)
    {
        _bridge = bridge;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _bridge.EnsureInstruments();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
