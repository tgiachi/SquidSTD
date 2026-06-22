using Serilog;
using SquidStd.Core.Types;

namespace SquidStd.Services.Core.Extensions.Logger;

/// <summary>
/// Extension methods for converting SquidStd logger options to Serilog values.
/// </summary>
public static class SquidStdLogRollingIntervalExtensions
{
    /// <summary>
    /// Converts a SquidStd rolling interval to a Serilog rolling interval.
    /// </summary>
    /// <param name="interval">The rolling interval to convert.</param>
    /// <returns>The corresponding Serilog rolling interval.</returns>
    public static RollingInterval ToSerilogRollingInterval(this SquidStdLogRollingIntervalType interval)
        => interval switch
        {
            SquidStdLogRollingIntervalType.Infinite => RollingInterval.Infinite,
            SquidStdLogRollingIntervalType.Year     => RollingInterval.Year,
            SquidStdLogRollingIntervalType.Month    => RollingInterval.Month,
            SquidStdLogRollingIntervalType.Day      => RollingInterval.Day,
            SquidStdLogRollingIntervalType.Hour     => RollingInterval.Hour,
            SquidStdLogRollingIntervalType.Minute   => RollingInterval.Minute,
            _                                       => RollingInterval.Day
        };
}
