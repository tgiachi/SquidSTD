using SquidStd.Telemetry.Abstractions;

namespace SquidStd.Tests.Telemetry;

public class SquidStdActivityTests
{
    [Fact]
    public void Source_IsNamedWithThePrefix()
    {
        Assert.Equal("SquidStd", SquidStdActivity.SourcePrefix);
        Assert.Equal("SquidStd", SquidStdActivity.Source.Name);
    }
}
