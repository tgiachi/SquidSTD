using SquidStd.Core.Types;

namespace SquidStd.Core.Data.Bootstrap;

/// <summary>
/// Defines YAML-backed logger options for SquidStd bootstrap.
/// </summary>
public sealed class SquidStdLoggerOptions
{
    /// <summary>
    /// Gets or sets the minimum logger level.
    /// </summary>
    public LogLevelType MinimumLevel { get; set; } = LogLevelType.Information;

    /// <summary>
    /// Gets or sets whether console logging is enabled.
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// Gets or sets whether file logging is enabled.
    /// </summary>
    public bool EnableFile { get; set; }

    /// <summary>
    /// Gets or sets the file log directory. Relative paths are resolved from the SquidStd root directory.
    /// </summary>
    public string LogDirectory { get; set; } = "logs";

    /// <summary>
    /// Gets or sets the file log name or rolling file pattern.
    /// </summary>
    public string FileName { get; set; } = "squidstd-.log";

    /// <summary>
    /// Gets or sets the file log rolling interval.
    /// </summary>
    public SquidStdLogRollingIntervalType RollingInterval { get; set; } = SquidStdLogRollingIntervalType.Day;
}
