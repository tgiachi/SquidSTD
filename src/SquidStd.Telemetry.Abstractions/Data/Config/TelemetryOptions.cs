using SquidStd.Telemetry.Abstractions.Types.Telemetry;

namespace SquidStd.Telemetry.Abstractions.Data.Config;

/// <summary>Configuration for SquidStd OpenTelemetry tracing and metrics export.</summary>
public sealed class TelemetryOptions
{
    /// <summary>Logical service name (resource service.name). Default "squidstd".</summary>
    public string ServiceName { get; init; } = "squidstd";

    /// <summary>Service version (resource service.version). Null -> entry assembly version.</summary>
    public string? ServiceVersion { get; init; }

    /// <summary>Whether to build the tracing pipeline. Default true.</summary>
    public bool EnableTracing { get; init; } = true;

    /// <summary>Whether to build the metrics pipeline. Default true.</summary>
    public bool EnableMetrics { get; init; } = true;

    /// <summary>OTLP endpoint. Default "http://localhost:4317".</summary>
    public string OtlpEndpoint { get; init; } = "http://localhost:4317";

    /// <summary>OTLP protocol. Default Grpc.</summary>
    public OtlpProtocolType OtlpProtocol { get; init; } = OtlpProtocolType.Grpc;

    /// <summary>Also emit to the console exporter (dev/debug). Default false.</summary>
    public bool EnableConsoleExporter { get; init; }

    /// <summary>Trace sampling ratio 0..1 (ParentBased TraceIdRatio). Default 1.0.</summary>
    public double TracingSampleRatio { get; init; } = 1.0;

    /// <summary>Extra resource attributes merged into the OTel resource.</summary>
    public IReadOnlyDictionary<string, string>? ResourceAttributes { get; init; }
}
