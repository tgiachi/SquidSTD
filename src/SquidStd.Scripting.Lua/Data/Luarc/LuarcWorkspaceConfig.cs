using System.Text.Json.Serialization;

namespace SquidStd.Scripting.Lua.Data.Luarc;

/// <summary>
/// Workspace configuration for Lua Language Server
/// </summary>
public class LuarcWorkspaceConfig
{
    [JsonPropertyName("library")]
    public string[] Library { get; set; } = [];

    [JsonPropertyName("checkThirdParty")]
    public bool CheckThirdParty { get; set; } = false;
}
