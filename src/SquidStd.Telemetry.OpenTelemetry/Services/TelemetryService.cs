using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Telemetry.Abstractions.Data.Config;
using SquidStd.Telemetry.OpenTelemetry.Internal;

namespace SquidStd.Telemetry.OpenTelemetry.Services;

/// <summary>
/// Owns the OpenTelemetry TracerProvider/MeterProvider for non-web (DryIoc/worker) hosts, building
/// them on start and disposing (flushing) them on stop. Telemetry failures never crash the host.
/// </summary>
public sealed class TelemetryService : ISquidStdService, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<TelemetryService>();
    private readonly TelemetryOptions _options;
    private readonly MetricsSnapshotBridge _bridge;
    private TracerProvider? _tracerProvider;
    private MeterProvider? _meterProvider;
    private int _disposed;

    public TelemetryService(TelemetryOptions options, MetricsSnapshotBridge bridge)
    {
        _options = options;
        _bridge = bridge;
    }

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_options.EnableTracing)
            {
                var tracing = Sdk.CreateTracerProviderBuilder();
                TelemetryPipeline.ConfigureTracing(tracing, _options, includeAspNetCore: false);
                TelemetryPipeline.AddTraceExporters(tracing, _options);
                _tracerProvider = tracing.Build();
            }

            if (_options.EnableMetrics)
            {
                _bridge.EnsureInstruments();
                var metrics = Sdk.CreateMeterProviderBuilder();
                TelemetryPipeline.ConfigureMetrics(metrics, _options);
                TelemetryPipeline.AddMetricExporters(metrics, _options);
                _meterProvider = metrics.Build();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Telemetry initialisation failed; continuing without telemetry.");
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        Dispose();

        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _meterProvider?.Dispose();
        _tracerProvider?.Dispose();
        _bridge.Dispose();
    }
}
