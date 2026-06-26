using Microsoft.CodeAnalysis;

namespace SquidStd.Generators.Scripting.Lua;

internal sealed class ScriptModuleRegistrationCandidate
{
    public ScriptModuleRegistrationCandidate(
        string scriptModuleTypeName,
        string displayName,
        Location? location,
        bool isSupported
    )
    {
        ScriptModuleTypeName = scriptModuleTypeName;
        DisplayName = displayName;
        Location = location;
        IsSupported = isSupported;
    }

    public string ScriptModuleTypeName { get; }

    public string DisplayName { get; }

    public Location? Location { get; }

    public bool IsSupported { get; }
}
