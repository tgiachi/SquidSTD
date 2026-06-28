namespace SquidStd.Scripting.Lua.Data.Internal;

/// <summary>
///     Record containing data about a script module for internal processing.
/// </summary>
public sealed record ScriptModuleData
{
    /// <summary>
    ///     The .NET type of the script module.
    /// </summary>
    public Type ModuleType { get; }

    /// <summary>
    ///     Initializes a new instance of the ScriptModuleData record.
    /// </summary>
    /// <param name="moduleType">The .NET type of the script module.</param>
    public ScriptModuleData(Type moduleType)
    {
        ArgumentNullException.ThrowIfNull(moduleType);

        ModuleType = moduleType;
    }
}
