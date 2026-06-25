using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using SquidStd.Telemetry.Abstractions.Data.Config;
using SquidStd.Telemetry.OpenTelemetry.Internal;

namespace SquidStd.Tests.Telemetry;

public class TracingTests
{
    [Fact]
    public void Pipeline_ExportsSpans()
    {
        // A unique source name (outside the "SquidStd.*" wildcard) so other test providers running in
        // parallel that listen on "SquidStd.*" cannot record this activity and break isolation.
        const string sourceName = "TracingTests.Export";
        var exported = new List<Activity>();
        using var source = new ActivitySource(sourceName);

        using (var provider = BuildProvider(new TelemetryOptions { ServiceName = "test-svc" }, sourceName, exported))
        {
            using (var activity = source.StartActivity("do-work"))
            {
                activity?.SetTag("k", "v");
            }

            provider.ForceFlush();
        }

        var span = Assert.Single(exported);
        Assert.Equal("do-work", span.DisplayName);
    }

    [Fact]
    public void Pipeline_WithZeroSampling_RecordsNothing()
    {
        const string sourceName = "TracingTests.ZeroSample";
        var exported = new List<Activity>();
        using var source = new ActivitySource(sourceName);

        using (var provider = BuildProvider(new TelemetryOptions { TracingSampleRatio = 0.0 }, sourceName, exported))
        {
            using (source.StartActivity("dropped"))
            {
            }

            provider.ForceFlush();
        }

        Assert.Empty(exported);
    }

    private static TracerProvider BuildProvider(TelemetryOptions options, string sourceName, List<Activity> sink)
    {
        var builder = Sdk.CreateTracerProviderBuilder();
        TelemetryPipeline.ConfigureTracing(builder, options, includeAspNetCore: false);
        builder.AddSource(sourceName);
        builder.AddInMemoryExporter(sink);

        return builder.Build();
    }
}
