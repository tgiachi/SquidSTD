using SquidStd.Core.Data.Bootstrap;
using SquidStd.Core.Types;

namespace SquidStd.Tests.Bootstrap;

public class SquidStdOptionsTests
{
    [Fact]
    public void SquidStdOptions_Defaults_AreUsable()
    {
        var options = new SquidStdOptions();

        Assert.False(string.IsNullOrWhiteSpace(options.RootDirectory));
        Assert.Equal("squidstd", options.ConfigName);
    }

    [Fact]
    public void SquidStdLoggerOptions_Defaults_EnableConsoleAndDisableFile()
    {
        var options = new SquidStdLoggerOptions();

        Assert.Equal(LogLevelType.Information, options.MinimumLevel);
        Assert.True(options.EnableConsole);
        Assert.False(options.EnableFile);
        Assert.Equal("logs", options.LogDirectory);
        Assert.Equal("squidstd-.log", options.FileName);
        Assert.Equal(SquidStdLogRollingIntervalType.Day, options.RollingInterval);
    }
}
