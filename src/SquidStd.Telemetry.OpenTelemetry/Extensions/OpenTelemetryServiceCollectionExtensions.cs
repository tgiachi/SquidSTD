using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SquidStd.Telemetry.Abstractions.Data.Config;
using SquidStd.Telemetry.OpenTelemetry.Internal;
using SquidStd.Telemetry.OpenTelemetry.Services;

namespace SquidStd.Telemetry.OpenTelemetry.Extensions;

/// <summary>IServiceCollection registration for SquidStd OpenTelemetry (ASP.NET Core hosts).</summary>
public static class OpenTelemetryServiceCollectionExtensions
{
    /// <summary>Registers OpenTelemetry tracing/metrics on the ASP.NET Core service collection.</summary>
    public static IServiceCollection AddSquidStdTelemetry(this IServiceCollection services, TelemetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<MetricsSnapshotBridge>();

        var builder = services.AddOpenTelemetry();

        if (options.EnableTracing)
        {
            builder.WithTracing(tracing =>
            {
                TelemetryPipeline.ConfigureTracing(tracing, options, includeAspNetCore: true);
                TelemetryPipeline.AddTraceExporters(tracing, options);
            });
        }

        if (options.EnableMetrics)
        {
            services.AddHostedService<MetricsBridgeActivator>();
            builder.WithMetrics(metrics =>
            {
                TelemetryPipeline.ConfigureMetrics(metrics, options);
                TelemetryPipeline.AddMetricExporters(metrics, options);
            });
        }

        return services;
    }
}
