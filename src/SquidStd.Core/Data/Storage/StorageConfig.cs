using SquidStd.Core.Interfaces.Config;

namespace SquidStd.Core.Data.Storage;

/// <summary>
/// Configuration for local file storage.
/// </summary>
public sealed class StorageConfig : IConfigEntry
{
    /// <summary>
    /// Gets or sets the root directory used by local storage.
    /// </summary>
    public string RootDirectory { get; set; } = "storage";

    string IConfigEntry.SectionName => "storage";

    Type IConfigEntry.ConfigType => typeof(StorageConfig);

    object IConfigEntry.CreateDefault()
        => new StorageConfig();
}
