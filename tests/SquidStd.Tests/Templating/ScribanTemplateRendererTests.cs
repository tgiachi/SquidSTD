using SquidStd.Core.Directories;
using SquidStd.Templating;
using SquidStd.Templating.Services;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Templating;

public class ScribanTemplateRendererTests
{
    private static ScribanTemplateRenderer NewRenderer(string root)
        => new(new DirectoriesConfig(root, []));

    [Fact]
    public async Task RenderAsync_RendersModel()
    {
        using var temp = new TempDirectory();
        var renderer = NewRenderer(temp.Path);

        var result = await renderer.RenderAsync("Hi {{ user.name }}", new { User = new { Name = "squid" } });

        Assert.Equal("Hi squid", result);
    }

    [Fact]
    public async Task RegisterThenRenderByName_Works()
    {
        using var temp = new TempDirectory();
        var renderer = NewRenderer(temp.Path);
        renderer.Register("greet", "Hi {{ user.name }}");

        var result = await renderer.RenderByNameAsync("greet", new { User = new { Name = "squid" } });

        Assert.Equal("Hi squid", result);
    }

    [Fact]
    public async Task RenderByNameAsync_UnknownName_Throws()
    {
        using var temp = new TempDirectory();
        var renderer = NewRenderer(temp.Path);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.RenderByNameAsync("nope", null));
    }

    [Fact]
    public async Task RenderAsync_MalformedTemplate_Throws()
    {
        using var temp = new TempDirectory();
        var renderer = NewRenderer(temp.Path);

        await Assert.ThrowsAsync<TemplateException>(async () => await renderer.RenderAsync("{{ for x in }}", null));
    }

    [Fact]
    public void Register_MalformedTemplate_Throws()
    {
        using var temp = new TempDirectory();
        var renderer = NewRenderer(temp.Path);

        Assert.Throws<TemplateException>(() => renderer.Register("bad", "{{ for x in }}"));
    }

    [Fact]
    public async Task RenderAsync_EmptyTemplate_Throws()
    {
        using var temp = new TempDirectory();
        var renderer = NewRenderer(temp.Path);

        await Assert.ThrowsAsync<ArgumentException>(async () => await renderer.RenderAsync(string.Empty, null));
    }

    [Fact]
    public async Task StartAsync_AutoLoadsTemplatesFromDirectory()
    {
        using var temp = new TempDirectory();
        var emails = Path.Combine(temp.Path, "templates", "emails");
        Directory.CreateDirectory(emails);
        await File.WriteAllTextAsync(Path.Combine(emails, "welcome.tmpl"), "Welcome {{ user.name }}");

        var renderer = NewRenderer(temp.Path);
        await renderer.StartAsync();

        var result = await renderer.RenderByNameAsync("emails/welcome", new { User = new { Name = "squid" } });

        Assert.Equal("Welcome squid", result);
    }
}
