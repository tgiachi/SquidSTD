namespace SquidStd.Templating.Interfaces;

/// <summary>
/// Renders Scriban templates from strings or registered named templates.
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>Renders an ad-hoc template string against a model.</summary>
    ValueTask<string> RenderAsync(string template, object? model, CancellationToken cancellationToken = default);

    /// <summary>Compiles and registers a named template (replacing any existing one with the same name).</summary>
    void Register(string name, string template);

    /// <summary>Renders a previously registered template by name.</summary>
    ValueTask<string> RenderByNameAsync(string name, object? model, CancellationToken cancellationToken = default);
}
