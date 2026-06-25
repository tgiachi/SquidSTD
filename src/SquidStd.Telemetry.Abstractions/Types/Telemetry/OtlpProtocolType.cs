namespace SquidStd.Telemetry.Abstractions.Types.Telemetry;

/// <summary>OTLP wire protocol for the OpenTelemetry exporter.</summary>
public enum OtlpProtocolType
{
    /// <summary>OTLP over gRPC (default, port 4317).</summary>
    Grpc,

    /// <summary>OTLP over HTTP/protobuf (port 4318).</summary>
    HttpProtobuf
}
