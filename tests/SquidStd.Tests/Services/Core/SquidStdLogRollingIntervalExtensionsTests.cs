using Serilog;
using SquidStd.Core.Types;
using SquidStd.Services.Core.Extensions.Logger;

namespace SquidStd.Tests.Services.Core;

public class SquidStdLogRollingIntervalExtensionsTests
{
    [Theory, InlineData(SquidStdLogRollingIntervalType.Infinite, RollingInterval.Infinite),
     InlineData(SquidStdLogRollingIntervalType.Year, RollingInterval.Year),
     InlineData(SquidStdLogRollingIntervalType.Month, RollingInterval.Month),
     InlineData(SquidStdLogRollingIntervalType.Day, RollingInterval.Day),
     InlineData(SquidStdLogRollingIntervalType.Hour, RollingInterval.Hour),
     InlineData(SquidStdLogRollingIntervalType.Minute, RollingInterval.Minute)]
    public void ToSerilogRollingInterval_KnownIntervals_MapExpected(
        SquidStdLogRollingIntervalType input,
        RollingInterval expected
    )
        => Assert.Equal(expected, input.ToSerilogRollingInterval());

    [Fact]
    public void ToSerilogRollingInterval_UnmappedInterval_FallsBackToDay()
        => Assert.Equal(RollingInterval.Day, ((SquidStdLogRollingIntervalType)255).ToSerilogRollingInterval());
}
