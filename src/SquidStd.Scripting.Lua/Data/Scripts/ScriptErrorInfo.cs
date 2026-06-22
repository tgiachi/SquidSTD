namespace SquidStd.Scripting.Lua.Data.Scripts;

/// <summary>
/// Detailed information about a Lua execution error.
/// </summary>
public class ScriptErrorInfo
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Gets or sets the stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the column number.
    /// </summary>
    public int? ColumnNumber { get; set; }

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the error type.
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Gets or sets the source code.
    /// </summary>
    public string? SourceCode { get; set; }

    /// <summary>
    /// Original source file name when a mapped source is available.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Original line number when a mapped source is available.
    /// </summary>
    public int? OriginalLineNumber { get; set; }

    /// <summary>
    /// Original column number when a mapped source is available.
    /// </summary>
    public int? OriginalColumnNumber { get; set; }

    /// <summary>
    /// Optional origin label — e.g. the Lua component name when the error came from a lifecycle hook.
    /// </summary>
    public string? Source { get; set; }
}
