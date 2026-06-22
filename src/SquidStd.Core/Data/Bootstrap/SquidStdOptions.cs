namespace SquidStd.Core.Data.Bootstrap;

/// <summary>
/// Defines bootstrap-only options used to locate SquidStd runtime resources.
/// </summary>
public sealed class SquidStdOptions
{
    /// <summary>
    /// Gets or sets the root directory for configuration, logs, and runtime data.
    /// </summary>
    public string RootDirectory { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Gets or sets the logical configuration name or YAML file name.
    /// </summary>
    public string ConfigName { get; set; } = "squidstd";
}
