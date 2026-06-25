using SquidStd.Telemetry.Abstractions.Data.Config;
using SquidStd.Telemetry.Abstractions.Types.Telemetry;

namespace SquidStd.Tests.Telemetry;

public class TelemetryOptionsTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        var options = new TelemetryOptions();

        Assert.Equal("squidstd", options.ServiceName);
        Assert.True(options.EnableTracing);
        Assert.True(options.EnableMetrics);
        Assert.Equal("http://localhost:4317", options.OtlpEndpoint);
        Assert.Equal(OtlpProtocolType.Grpc, options.OtlpProtocol);
        Assert.False(options.EnableConsoleExporter);
        Assert.Equal(1.0, options.TracingSampleRatio);
        Assert.Null(options.ServiceVersion);
        Assert.Null(options.ResourceAttributes);
    }
}
