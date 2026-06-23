using DryIoc;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Templating.Interfaces;
using SquidStd.Templating.Services;

namespace SquidStd.Templating.Extensions;

/// <summary>
/// DryIoc registration helpers for the templating module.
/// </summary>
public static class TemplatingRegistrationExtensions
{
    /// <summary>
    /// Registers the Scriban template renderer as a singleton SquidStd service (so its startup
    /// auto-load of <c>templates/*.tmpl</c> runs with the host). Requires a registered <c>DirectoriesConfig</c>.
    /// </summary>
    public static IContainer AddTemplating(this IContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return container.RegisterStdService<ITemplateRenderer, ScribanTemplateRenderer>(-1);
    }
}
