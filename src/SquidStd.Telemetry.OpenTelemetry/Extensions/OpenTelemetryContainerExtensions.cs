using DryIoc;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Telemetry.Abstractions.Data.Config;
using SquidStd.Telemetry.OpenTelemetry.Services;

namespace SquidStd.Telemetry.OpenTelemetry.Extensions;

/// <summary>DryIoc registration for SquidStd OpenTelemetry (worker / non-web hosts).</summary>
public static class OpenTelemetryContainerExtensions
{
    /// <summary>Registers OpenTelemetry tracing/metrics as a managed SquidStd service.</summary>
    public static IContainer AddSquidStdTelemetry(this IContainer container, TelemetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(options);

        container.RegisterInstance(options);
        container.Register<MetricsSnapshotBridge>(Reuse.Singleton);
        container.RegisterStdService<TelemetryService, TelemetryService>(1000);

        return container;
    }
}
