using DryIoc;
using SquidStd.Plugin.Abstractions.Data;
using SquidStd.Plugin.Abstractions.Interfaces.Plugins;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.PluginAbstractions;

public class SquidStdPluginTests
{
    [Fact]
    public void Configure_ReceivesProvidedContext()
    {
        using var container = new Container();
        var plugin = new FakePlugin();
        var context = new PluginContext();

        ((ISquidStdPlugin)plugin).Configure(container, context);

        Assert.Same(context, plugin.ReceivedContext);
    }

    [Fact]
    public void Configure_RegistersServicesIntoContainer()
    {
        using var container = new Container();
        var plugin = new FakePlugin();

        ((ISquidStdPlugin)plugin).Configure(container, new());

        Assert.Same(plugin, container.Resolve<FakePlugin>());
    }

    [Fact]
    public void Metadata_ExposesPluginIdentity()
    {
        ISquidStdPlugin plugin = new FakePlugin();

        Assert.Equal("squidstd.fake", plugin.Metadata.Id);
        Assert.Equal(new(1, 2, 3), plugin.Metadata.Version);
    }
}
