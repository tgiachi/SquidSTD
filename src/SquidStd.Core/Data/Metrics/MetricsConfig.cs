using SquidStd.Core.Types;
using SquidStd.Core.Interfaces.Config;

namespace SquidStd.Core.Data.Metrics;

/// <summary>
/// Configuration for periodic metrics collection.
/// </summary>
public sealed class MetricsConfig : IConfigEntry
{
    /// <summary>
    /// Gets or sets whether metrics collection is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the collection interval in milliseconds.
    /// </summary>
    public int IntervalMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether each provider collection is logged.
    /// </summary>
    public bool LogEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the log level used for metrics collection logs.
    /// </summary>
    public LogLevelType LogLevel { get; set; } = LogLevelType.Trace;

    string IConfigEntry.SectionName => "metrics";

    Type IConfigEntry.ConfigType => typeof(MetricsConfig);

    object IConfigEntry.CreateDefault()
        => new MetricsConfig();
}
