using DryIoc;
using SquidStd.Plugin.Abstractions.Data;

namespace SquidStd.Plugin.Abstractions.Interfaces.Plugins;

/// <summary>
/// Implemented by trusted .NET plugins loaded by Moongate during server startup.
/// </summary>
public interface ISquidStdPlugin
{
    /// <summary>Plugin identity, descriptive information, and dependency declarations.</summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Registers the plugin's services, handlers, config sections, Lua modules, and other integrations.
    /// Called during container configuration before global server YAML config is loaded.
    /// </summary>
    /// <param name="container">The DryIoc container being configured.</param>
    /// <param name="context">The plugin-specific boot context.</param>
    void Configure(IContainer container, PluginContext context);
}
