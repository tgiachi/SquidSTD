using DryIoc;
using SquidStd.Plugin.Abstractions.Data;
using SquidStd.Plugin.Abstractions.Interfaces.Plugins;

namespace SquidStd.Samples.Plugins;

#region step-1
public interface IGreeter
{
    string Greet(string name);
}

public sealed class WeatherGreeter : IGreeter
{
    public string Greet(string name)
        => $"Hello {name}, the weather plugin is online.";
}

public sealed class WeatherPlugin : ISquidStdPlugin
{
    public PluginMetadata Metadata { get; } = new()
    {
        Id = "squidstd.weather",
        Name = "Weather Plugin",
        Version = new Version(1, 0, 0),
        Author = "SquidStd Samples",
        Description = "Registers a greeter service.",
        Dependencies = []
    };

    public void Configure(IContainer container, PluginContext context)
    {
        container.Register<IGreeter, WeatherGreeter>(Reuse.Singleton);
    }
}
#endregion

internal static class Program
{
    private static void Main()
    {
        #region step-2
        var container = new Container();
        var context = new PluginContext();
        context.Data["startedAt"] = DateTimeOffset.UtcNow;

        ISquidStdPlugin plugin = new WeatherPlugin();

        Console.WriteLine($"Loading {plugin.Metadata.Name} v{plugin.Metadata.Version} by {plugin.Metadata.Author}");
        plugin.Configure(container, context);

        var greeter = container.Resolve<IGreeter>();
        Console.WriteLine(greeter.Greet("squid"));
        #endregion
    }
}
