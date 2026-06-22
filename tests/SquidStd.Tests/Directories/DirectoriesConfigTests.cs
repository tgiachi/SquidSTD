using SquidStd.Core.Directories;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Directories;

public class DirectoriesConfigTests
{
    [Fact]
    public void Constructor_CreatesRootAndConfiguredSubdirectories()
    {
        using var temp = new TempDirectory();
        var root = temp.Combine("app");

        _ = new DirectoriesConfig(root, ["Logs", "ConfigFiles"]);

        Assert.True(Directory.Exists(root));
        Assert.True(Directory.Exists(Path.Combine(root, "logs")));
        Assert.True(Directory.Exists(Path.Combine(root, "config_files")));
    }

    [Fact]
    public void EnumIndexer_ResolvesPathFromEnumName()
    {
        using var temp = new TempDirectory();
        var config = new DirectoriesConfig(temp.Combine("app"), []);

        Assert.Equal(Path.Combine(config.Root, "plugins"), config[TestDirectoryType.Plugins]);
    }

    [Fact]
    public void GetPath_CreatesMissingDirectoryOnDemand()
    {
        using var temp = new TempDirectory();
        var config = new DirectoriesConfig(temp.Combine("app"), []);

        var path = config.GetPath("Scripts");

        Assert.True(Directory.Exists(path));
        Assert.EndsWith("scripts", path);
    }

    [Fact]
    public void GetPath_String_ReturnsSnakeCasedPath()
    {
        using var temp = new TempDirectory();
        var config = new DirectoriesConfig(temp.Combine("app"), ["Logs"]);

        Assert.Equal(Path.Combine(config.Root, "logs"), config.GetPath("Logs"));
    }

    [Fact]
    public void GetPathGeneric_ResolvesPathFromEnumValue()
    {
        using var temp = new TempDirectory();
        var config = new DirectoriesConfig(temp.Combine("app"), []);

        Assert.Equal(Path.Combine(config.Root, "config_files"), config.GetPath(TestDirectoryType.ConfigFiles));
    }

    [Fact]
    public void StringIndexer_ReturnsSamePathAsGetPath()
    {
        using var temp = new TempDirectory();
        var config = new DirectoriesConfig(temp.Combine("app"), ["Logs"]);

        Assert.Equal(config.GetPath("Logs"), config["Logs"]);
    }

    [Fact]
    public void ToString_ReturnsRoot()
    {
        using var temp = new TempDirectory();
        var root = temp.Combine("app");
        var config = new DirectoriesConfig(root, []);

        Assert.Equal(root, config.ToString());
    }
}
