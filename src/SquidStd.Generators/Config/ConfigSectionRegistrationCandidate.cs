using Microsoft.CodeAnalysis;

namespace SquidStd.Generators.Config;

internal sealed class ConfigSectionRegistrationCandidate
{
    public string ConfigTypeName { get; }

    public string SectionName { get; }

    public string DisplayName { get; }

    public Location? Location { get; }

    public int Priority { get; }

    public bool IsSupported { get; }

    public ConfigSectionRegistrationCandidate(
        string configTypeName,
        string sectionName,
        string displayName,
        Location? location,
        int priority,
        bool isSupported
    )
    {
        ConfigTypeName = configTypeName;
        SectionName = sectionName;
        DisplayName = displayName;
        Location = location;
        Priority = priority;
        IsSupported = isSupported;
    }
}
