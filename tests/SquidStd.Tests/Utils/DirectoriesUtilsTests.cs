using SquidStd.Core.Utils;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Utils;

public class DirectoriesUtilsTests
{
    [Fact]
    public void GetFiles_ExtensionFilter_ReturnsMatchingFilesOnly()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(temp.Combine("a.txt"), "a");
        File.WriteAllText(temp.Combine("b.json"), "b");
        File.WriteAllText(temp.Combine("c.json"), "c");

        var files = DirectoriesUtils.GetFiles(temp.Path, "*.json");

        Assert.Equal(2, files.Length);
        Assert.All(files, file => Assert.EndsWith(".json", file));
    }

    [Fact]
    public void GetFiles_NoExtensionFilter_ReturnsAllFiles()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(temp.Combine("a.txt"), "a");
        File.WriteAllText(temp.Combine("b.json"), "b");

        var files = DirectoriesUtils.GetFiles(temp.Path);

        Assert.Equal(2, files.Length);
    }

    [Fact]
    public void GetFiles_NonExistentDirectory_ReturnsEmpty()
        => Assert.Empty(DirectoriesUtils.GetFiles(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));

    [Fact]
    public void GetFiles_NonRecursive_ExcludesNestedFiles()
    {
        using var temp = new TempDirectory();
        var nested = temp.Combine("nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(temp.Combine("root.txt"), "root");
        File.WriteAllText(Path.Combine(nested, "child.txt"), "child");

        var files = DirectoriesUtils.GetFiles(temp.Path, false, "*.txt");

        Assert.Single(files);
        Assert.EndsWith("root.txt", files[0]);
    }

    [Fact]
    public void GetFiles_Recursive_IncludesNestedFiles()
    {
        using var temp = new TempDirectory();
        var nested = temp.Combine("nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(temp.Combine("root.txt"), "root");
        File.WriteAllText(Path.Combine(nested, "child.txt"), "child");

        var files = DirectoriesUtils.GetFiles(temp.Path, true, "*.txt");

        Assert.Equal(2, files.Length);
    }
}
