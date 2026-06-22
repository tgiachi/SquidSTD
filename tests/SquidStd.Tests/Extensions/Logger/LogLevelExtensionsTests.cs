using Serilog.Events;
using SquidStd.Core.Extensions.Logger;
using SquidStd.Core.Types;

namespace SquidStd.Tests.Extensions.Logger;

public class LogLevelExtensionsTests
{
    [Theory, InlineData(LogLevelType.Trace, LogEventLevel.Verbose), InlineData(LogLevelType.Debug, LogEventLevel.Debug),
     InlineData(LogLevelType.Information, LogEventLevel.Information),
     InlineData(LogLevelType.Warning, LogEventLevel.Warning), InlineData(LogLevelType.Error, LogEventLevel.Error),
     InlineData(LogLevelType.Critical, LogEventLevel.Fatal)]
    public void ToSerilogLogLevel_KnownLevels_MapsExpected(LogLevelType input, LogEventLevel expected)
        => Assert.Equal(expected, input.ToSerilogLogLevel());

    [Fact]
    public void ToSerilogLogLevel_UnmappedLevels_FallsBackToInformation()
        => Assert.Equal(LogEventLevel.Information, ((LogLevelType)255).ToSerilogLogLevel());
}
