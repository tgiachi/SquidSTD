namespace SquidStd.Scripting.Lua.Attributes.Scripts;

/// <summary>
/// Attribute that marks a method as a script function exposed to scripting languages.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ScriptFunctionAttribute : Attribute
{
    /// <summary>
    /// Gets the optional name override for the script function.
    /// </summary>
    public string? FunctionName { get; }

    /// <summary>
    /// Gets the optional help text describing the function's purpose.
    /// </summary>
    public string? HelpText { get; }

    /// <summary>
    /// Initializes a new instance of the ScriptFunctionAttribute class.
    /// </summary>
    /// <param name="functionName">The optional name override for the script function.</param>
    /// <param name="helpText">The optional help text describing the function's purpose.</param>
    public ScriptFunctionAttribute(string? functionName = null, string? helpText = null)
    {
        FunctionName = functionName;
        HelpText = helpText;
    }
}
