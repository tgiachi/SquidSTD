using SquidStd.Core.Interfaces.Config;

namespace SquidStd.Abstractions.Data.Internal.Config;

public sealed class ConfigRegistrationData : IConfigEntry
{
    private readonly Func<object> _createDefault;

    public int Priority { get; }

    public string SectionName { get; }

    public Type ConfigType { get; }

    public ConfigRegistrationData(
        string sectionName,
        Type configType,
        Func<object> createDefault,
        int priority = 0
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        ArgumentNullException.ThrowIfNull(configType);
        ArgumentNullException.ThrowIfNull(createDefault);

        SectionName = sectionName;
        ConfigType = configType;
        _createDefault = createDefault;
        Priority = priority;
    }

    public object CreateDefault()
    {
        var config = _createDefault();

        if (!ConfigType.IsInstanceOfType(config))
        {
            throw new InvalidOperationException("Default config factory returned an incompatible object.");
        }

        return config;
    }
}
