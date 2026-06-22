namespace SquidStd.Core.Interfaces.Config;

/// <summary>
/// Manages the YAML configuration file and registers loaded sections into DI.
/// </summary>
public interface IConfigManagerService
{
    /// <summary>
    /// Gets the logical configuration name.
    /// </summary>
    string ConfigName { get; }

    /// <summary>
    /// Gets the directory where the configuration file is searched.
    /// </summary>
    string ConfigDirectory { get; }

    /// <summary>
    /// Gets the resolved YAML configuration file path.
    /// </summary>
    string ConfigPath { get; }

    /// <summary>
    /// Gets the registered configuration entries.
    /// </summary>
    IReadOnlyCollection<IConfigEntry> Entries { get; }

    /// <summary>
    /// Composes the currently loaded sections into YAML.
    /// </summary>
    /// <returns>The composed YAML document.</returns>
    string Compose();

    /// <summary>
    /// Gets a loaded configuration section from DI.
    /// </summary>
    /// <typeparam name="TConfig">The configuration type.</typeparam>
    /// <returns>The loaded configuration section.</returns>
    TConfig GetConfig<TConfig>() where TConfig : class;

    /// <summary>
    /// Loads or creates the configured YAML file and registers every section into DI.
    /// </summary>
    void Load();

    /// <summary>
    /// Saves the currently loaded sections to the configured YAML file.
    /// </summary>
    void Save();
}
