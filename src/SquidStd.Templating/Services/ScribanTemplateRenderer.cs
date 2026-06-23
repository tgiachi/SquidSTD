using System.Collections.Concurrent;
using Scriban;
using Scriban.Syntax;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Directories;
using SquidStd.Templating.Interfaces;

namespace SquidStd.Templating.Services;

/// <summary>
/// Scriban-backed <see cref="ITemplateRenderer" />. Named templates are compiled and cached; ad-hoc
/// strings are parsed per call. On start it auto-loads <c>templates/**/*.tmpl</c>.
/// </summary>
public sealed class ScribanTemplateRenderer : ITemplateRenderer, ISquidStdService
{
    private const string TemplateExtension = ".tmpl";

    private readonly DirectoriesConfig _directories;
    private readonly ConcurrentDictionary<string, Template> _named = new(StringComparer.Ordinal);

    public ScribanTemplateRenderer(DirectoriesConfig directories)
    {
        _directories = directories;
    }

    /// <inheritdoc />
    public async ValueTask<string> RenderAsync(string template, object? model, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        return await RenderCompiledAsync(Parse(template, "(inline)"), model, "(inline)");
    }

    /// <inheritdoc />
    public void Register(string name, string template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        _named[name] = Parse(template, name);
    }

    /// <inheritdoc />
    public async ValueTask<string> RenderByNameAsync(string name, object? model, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_named.TryGetValue(name, out var compiled))
        {
            throw new InvalidOperationException($"No template registered with name '{name}'.");
        }

        return await RenderCompiledAsync(compiled, model, name);
    }

    /// <inheritdoc />
    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        var directory = _directories["templates"];

        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*" + TemplateExtension, SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(directory, file).Replace('\\', '/');
            var name = relative[..^TemplateExtension.Length];
            var content = await File.ReadAllTextAsync(file, cancellationToken);

            Register(name, content);
        }
    }

    /// <inheritdoc />
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    private static Template Parse(string text, string name)
    {
        var template = Template.Parse(text);

        if (template.HasErrors)
        {
            throw new TemplateException($"Template '{name}' has parse errors: {string.Join("; ", template.Messages)}");
        }

        return template;
    }

    private static async ValueTask<string> RenderCompiledAsync(Template template, object? model, string name)
    {
        try
        {
            return await template.RenderAsync(model);
        }
        catch (ScriptRuntimeException ex)
        {
            throw new TemplateException($"Template '{name}' failed to render: {ex.Message}", ex);
        }
    }
}
