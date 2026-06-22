using System.Reflection;
using DryIoc;
using SquidStd.Abstractions.Data.Internal.Config;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Extensions.Env;
using SquidStd.Core.Interfaces.Config;
using SquidStd.Core.Yaml;

namespace SquidStd.Services.Core.Services;

/// <summary>
/// Loads YAML configuration sections and registers them into DryIoc.
/// </summary>
public sealed class ConfigManagerService : IConfigManagerService, ISquidStdService
{
    private readonly IContainer _container;
    private readonly Dictionary<Type, object> _values = [];
    private int _started;

    /// <inheritdoc />
    public string ConfigName { get; }

    /// <inheritdoc />
    public string ConfigDirectory { get; }

    /// <inheritdoc />
    public string ConfigPath { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<IConfigEntry> Entries => GetEntries();

    /// <summary>
    /// Initializes the config manager service.
    /// </summary>
    /// <param name="container">Container that receives loaded configuration sections.</param>
    /// <param name="configName">Logical configuration name or YAML file name.</param>
    /// <param name="configDirectory">Directory where the configuration file is searched.</param>
    public ConfigManagerService(IContainer container, string configName, string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configName);
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);

        _container = container;
        ConfigName = configName;
        ConfigDirectory = Path.GetFullPath(configDirectory);
        ConfigPath = ResolveConfigPath(configName, ConfigDirectory);
    }

    /// <inheritdoc />
    public string Compose()
        => YamlUtils.SerializeSections(BuildSectionMap());

    /// <inheritdoc />
    public TConfig GetConfig<TConfig>() where TConfig : class
        => _container.Resolve<TConfig>();

    /// <inheritdoc />
    public void Load()
    {
        var entries = GetRegistrations();
        var yaml = File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath) : string.Empty;

        _values.Clear();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var value = string.IsNullOrWhiteSpace(yaml)
                            ? entry.CreateDefault()
                            : YamlUtils.DeserializeSection(yaml, entry.SectionName, entry.ConfigType) ??
                              entry.CreateDefault();

            ApplyEnvSubstitution(value, new HashSet<object>(ReferenceEqualityComparer.Instance));

            _values[entry.ConfigType] = value;
            _container.RegisterInstance(entry.ConfigType, value, IfAlreadyRegistered.Replace);
        }

        if (!File.Exists(ConfigPath))
        {
            Save();
        }
    }

    /// <inheritdoc />
    public void Save()
        => YamlUtils.SerializeToFile(BuildSectionMap(), ConfigPath);

    /// <inheritdoc />
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        Load();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.CompletedTask;
    }

    private Dictionary<string, object> BuildSectionMap()
    {
        var sections = new Dictionary<string, object>(StringComparer.Ordinal);
        var entries = GetRegistrations();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            if (!_values.TryGetValue(entry.ConfigType, out var value))
            {
                value = entry.CreateDefault();
            }

            sections[entry.SectionName] = value;
        }

        return sections;
    }

    private IReadOnlyCollection<IConfigEntry> GetEntries()
        => GetRegistrations().Cast<IConfigEntry>().ToArray();

    private List<ConfigRegistrationData> GetRegistrations()
    {
        if (!_container.IsRegistered<List<ConfigRegistrationData>>())
        {
            return [];
        }

        return
        [
            .. _container.Resolve<List<ConfigRegistrationData>>()
                         .OrderBy(entry => entry.Priority)
                         .ThenBy(entry => entry.SectionName, StringComparer.Ordinal)
        ];
    }

    private static void ApplyEnvSubstitution(object? instance, HashSet<object> visited)
    {
        if (instance is null || !visited.Add(instance))
        {
            return;
        }

        var type = instance.GetType();

        if (type.Namespace is null || !type.Namespace.StartsWith("SquidStd", StringComparison.Ordinal))
        {
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (property.PropertyType == typeof(string) && property.CanRead && property.CanWrite)
            {
                var current = (string?)property.GetValue(instance);

                if (!string.IsNullOrEmpty(current))
                {
                    property.SetValue(instance, current.ReplaceEnv());
                }

                continue;
            }

            if (property.PropertyType.IsClass && property.PropertyType != typeof(string) && property.CanRead)
            {
                ApplyEnvSubstitution(property.GetValue(instance), visited);
            }
        }
    }

    private static string ResolveConfigPath(string configName, string configDirectory)
    {
        var fileName = Path.HasExtension(configName) ? configName : $"{configName}.yaml";

        return Path.Combine(configDirectory, fileName);
    }
}
