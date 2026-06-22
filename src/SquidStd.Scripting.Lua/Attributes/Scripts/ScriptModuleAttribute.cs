namespace SquidStd.Scripting.Lua.Attributes.Scripts;

/// <summary>
/// Attribute that marks a class as a script module exposed to scripting languages.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ScriptModuleAttribute : Attribute
{
    /// <summary>Gets the name under which the module will be accessible in Lua.</summary>
    public string Name { get; }

    /// <summary>Gets the optional help text describing the module's purpose.</summary>
    public string? HelpText { get; }

    /// <summary>
    /// Initializes a new instance of the ScriptModuleAttribute class.
    /// </summary>
    /// <param name="name">The name under which the module will be accessible in Lua.</param>
    /// <param name="helpText">The optional help text describing the module's purpose.</param>
    public ScriptModuleAttribute(string name, string? helpText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        HelpText = helpText;
    }
}
