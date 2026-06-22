namespace SquidStd.Core.Types;

/// <summary>
/// Defines file log rolling intervals.
/// </summary>
public enum SquidStdLogRollingIntervalType
{
    /// <summary>
    /// Does not roll log files automatically.
    /// </summary>
    Infinite = 0,

    /// <summary>
    /// Rolls log files every year.
    /// </summary>
    Year = 1,

    /// <summary>
    /// Rolls log files every month.
    /// </summary>
    Month = 2,

    /// <summary>
    /// Rolls log files every day.
    /// </summary>
    Day = 3,

    /// <summary>
    /// Rolls log files every hour.
    /// </summary>
    Hour = 4,

    /// <summary>
    /// Rolls log files every minute.
    /// </summary>
    Minute = 5
}
