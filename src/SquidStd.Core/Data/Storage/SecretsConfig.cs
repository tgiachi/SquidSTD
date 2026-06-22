using SquidStd.Core.Interfaces.Config;

namespace SquidStd.Core.Data.Storage;

/// <summary>
/// Configuration for encrypted local secret storage.
/// </summary>
public sealed class SecretsConfig : IConfigEntry
{
    /// <summary>
    /// Gets or sets the root directory used by local secret storage.
    /// </summary>
    public string RootDirectory { get; set; } = "secrets";

    /// <summary>
    /// Gets or sets the environment variable that contains the base64 AES key.
    /// </summary>
    public string KeyEnvironmentVariable { get; set; } = "SQUIDSTD_SECRETS_KEY";

    string IConfigEntry.SectionName => "secrets";

    Type IConfigEntry.ConfigType => typeof(SecretsConfig);

    object IConfigEntry.CreateDefault()
        => new SecretsConfig();
}
