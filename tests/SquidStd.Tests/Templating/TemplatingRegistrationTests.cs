using DryIoc;
using SquidStd.Core.Directories;
using SquidStd.Templating.Extensions;
using SquidStd.Templating.Interfaces;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Templating;

public class TemplatingRegistrationTests
{
    [Fact]
    public async Task AddTemplating_ResolvesRendererAndRenders()
    {
        using var temp = new TempDirectory();
        var container = new Container();
        container.RegisterInstance(new DirectoriesConfig(temp.Path, []));

        container.AddTemplating();

        var renderer = container.Resolve<ITemplateRenderer>();
        var result = await renderer.RenderAsync("Hi {{ user.name }}", new { User = new { Name = "squid" } });

        Assert.Equal("Hi squid", result);
    }
}
