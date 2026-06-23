namespace SquidStd.Templating;

/// <summary>
/// Raised when a template fails to parse or render.
/// </summary>
public sealed class TemplateException : Exception
{
    public TemplateException(string message)
        : base(message)
    {
    }

    public TemplateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
