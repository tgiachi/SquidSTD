using DryIoc;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Core.Interfaces.Config;
using SquidStd.Services.Core.Services;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Services.Core;

public class ConfigManagerServiceTests
{
    [Fact]
    public void ConfigPath_AppendsYamlExtension()
    {
        using var container = new Container();
        IConfigManagerService manager = new ConfigManagerService(container, "app", "/tmp/config");

        Assert.Equal(Path.Combine("/tmp/config", "app.yaml"), manager.ConfigPath);
    }

    [Fact]
    public async Task GetConfig_ReturnsRegisteredSectionAfterStart()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        container.RegisterConfigSection<TestConfig>("test");
        IConfigManagerService manager = new ConfigManagerService(container, "app", temp.Path);

        await ((ConfigManagerService)manager).StartAsync(CancellationToken.None);

        Assert.Same(container.Resolve<TestConfig>(), manager.GetConfig<TestConfig>());
    }

    [Fact]
    public async Task StartAsync_ExistingFile_LoadsAndRegistersSection()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(
            temp.Combine("app.yaml"),
            """
            test:
              Name: loaded
              Count: 9
            """
        );
        using var container = new Container();
        container.RegisterConfigSection("test", static () => new TestConfig { Name = "default", Count = 3 });
        IConfigManagerService manager = new ConfigManagerService(container, "app", temp.Path);

        await ((ConfigManagerService)manager).StartAsync(CancellationToken.None);

        var config = container.Resolve<TestConfig>();
        Assert.Equal("loaded", config.Name);
        Assert.Equal(9, config.Count);
    }

    [Fact]
    public async Task StartAsync_MissingFile_CreatesDefaultFileAndRegistersSection()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        container.RegisterConfigSection("test", static () => new TestConfig { Name = "default", Count = 3 });
        IConfigManagerService manager = new ConfigManagerService(container, "app", temp.Path);

        await ((ConfigManagerService)manager).StartAsync(CancellationToken.None);

        var path = Path.Combine(temp.Path, "app.yaml");
        var config = container.Resolve<TestConfig>();

        Assert.True(File.Exists(path));
        Assert.Equal("default", config.Name);
        Assert.Equal(3, config.Count);
        Assert.Contains("test:", File.ReadAllText(path));
    }

    [Fact]
    public async Task StartAsync_MissingSection_UsesDefaultAndSavesIt()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(
            temp.Combine("app.yaml"),
            """
            other:
              Enabled: true
            """
        );
        using var container = new Container();
        container.RegisterConfigSection("test", static () => new TestConfig { Name = "default", Count = 3 });
        IConfigManagerService manager = new ConfigManagerService(container, "app", temp.Path);

        await ((ConfigManagerService)manager).StartAsync(CancellationToken.None);
        manager.Save();

        var config = container.Resolve<TestConfig>();
        var yaml = File.ReadAllText(temp.Combine("app.yaml"));

        Assert.Equal("default", config.Name);
        Assert.Equal(3, config.Count);
        Assert.Contains("test:", yaml);
        Assert.DoesNotContain("other:", yaml);
    }
}
