namespace SquidStd.Core.Interfaces.Config;

/// <summary>
/// Describes a configuration section that can be composed into YAML and registered into DI.
/// </summary>
public interface IConfigEntry
{
    /// <summary>
    /// Gets the top-level YAML section name.
    /// </summary>
    string SectionName { get; }

    /// <summary>
    /// Gets the concrete configuration type for this section.
    /// </summary>
    Type ConfigType { get; }

    /// <summary>
    /// Creates a default configuration value for this section.
    /// </summary>
    /// <returns>The default configuration object.</returns>
    object CreateDefault();
}
