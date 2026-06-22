using DryIoc;
using SquidStd.Plugin.Abstractions.Data;
using SquidStd.Plugin.Abstractions.Interfaces.Plugins;

namespace SquidStd.Tests.Support;

/// <summary>
/// Minimal <see cref="ISquidStdPlugin" /> implementation used to exercise the plugin contract.
/// </summary>
public class FakePlugin : ISquidStdPlugin
{
    /// <summary>
    /// Gets the plugin metadata.
    /// </summary>
    public PluginMetadata Metadata { get; } = new()
    {
        Id = "squidstd.fake",
        Name = "Fake Plugin",
        Version = new(1, 2, 3),
        Author = "tests"
    };

    /// <summary>
    /// Gets the context received during the last <see cref="Configure" /> call.
    /// </summary>
    public PluginContext? ReceivedContext { get; private set; }

    public void Configure(IContainer container, PluginContext context)
    {
        ReceivedContext = context;
        container.RegisterInstance(this);
    }
}
