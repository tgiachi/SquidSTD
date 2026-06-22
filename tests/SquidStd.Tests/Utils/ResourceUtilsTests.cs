using System.Reflection;
using SquidStd.Core.Utils;

namespace SquidStd.Tests.Utils;

public class ResourceUtilsTests
{
    private static readonly Assembly TestAssembly = typeof(ResourceUtilsTests).Assembly;
    private const string SampleResourceSuffix = "Support.Resources.sample.txt";

    [Fact]
    public void ConvertResourceNameToPath_NestedName_ConvertsDotsToSeparators()
    {
        var expected = string.Join(Path.DirectorySeparatorChar, "Folder", "Sub", "file") + ".txt";

        Assert.Equal(expected, ResourceUtils.ConvertResourceNameToPath("Asm.Folder.Sub.file.txt", "Asm"));
    }

    [Fact]
    public void ConvertResourceNameToPath_NoExtension_Throws()
        => Assert.Throws<ArgumentException>(() => ResourceUtils.ConvertResourceNameToPath("Asm.file", "Asm"));

    [Fact]
    public void ConvertResourceNameToPath_ValidName_ReturnsFilePath()
    {
        var expected = Path.Combine("cfg") + ".json";

        Assert.Equal(expected, ResourceUtils.ConvertResourceNameToPath("Asm.cfg.json", "Asm"));
    }

    [Fact]
    public void ConvertResourceNameToPath_WrongNamespace_Throws()
        => Assert.Throws<ArgumentException>(() => ResourceUtils.ConvertResourceNameToPath("Other.cfg.json", "Asm"));

    [Fact]
    public void EmbeddedNameToPath_StripsPrefixAndConvertsDots()
        => Assert.Equal("Folder/file/txt", ResourceUtils.EmbeddedNameToPath("Asm.Folder.file.txt", "Asm"));

    [Fact]
    public void GetDirectoryPathFromResourceName_RemovesBaseNamespace()
    {
        var expected = string.Join(Path.DirectorySeparatorChar, "Fonts");

        Assert.Equal(expected, ResourceUtils.GetDirectoryPathFromResourceName("Asm.Fonts.Font.ttf", "Asm"));
    }

    [Fact]
    public void GetDirectoryPathFromResourceName_ReturnsDirectoryPart()
    {
        var expected = string.Join(Path.DirectorySeparatorChar, "Assets", "Fonts");

        Assert.Equal(expected, ResourceUtils.GetDirectoryPathFromResourceName("Assets.Fonts.DefaultUiFont.ttf"));
    }

    [Fact]
    public void GetEmbeddedResourceNames_NoFilter_IncludesSampleResource()
    {
        var names = ResourceUtils.GetEmbeddedResourceNames(TestAssembly);

        Assert.Contains(names, name => name.EndsWith(SampleResourceSuffix, StringComparison.Ordinal));
    }

    [Fact]
    public void GetEmbeddedResourceStream_MissingResource_Throws()
        => Assert.Throws<FileNotFoundException>(
            () => ResourceUtils.GetEmbeddedResourceStream(TestAssembly, "does-not-exist.bin")
        );

    [Fact]
    public void GetEmbeddedResourceString_ExistingResource_ReturnsContent()
    {
        var content = ResourceUtils.GetEmbeddedResourceString(TestAssembly, SampleResourceSuffix);

        Assert.Contains("embedded-resource-content", content);
    }

    [Fact]
    public void GetFileNameFromResourceName_ReturnsFileNameWithExtension()
        => Assert.Equal("DefaultUiFont.ttf", ResourceUtils.GetFileNameFromResourceName("Assets.Fonts.DefaultUiFont.ttf"));

    [Theory, InlineData("a/b/c.txt", "c.txt"), InlineData("a\\b\\c.txt", "c.txt"), InlineData("c.txt", "c.txt")]
    public void GetFileNameFromResourcePath_ReturnsFinalSegment(string input, string expected)
        => Assert.Equal(expected, ResourceUtils.GetFileNameFromResourcePath(input));

    [Fact]
    public void ReadEmbeddedResource_ExistingResource_ReturnsContent()
    {
        var content = ResourceUtils.ReadEmbeddedResource("sample.txt", TestAssembly);

        Assert.NotNull(content);
        Assert.Contains("embedded-resource-content", content);
    }

    [Fact]
    public void ReadEmbeddedResource_MissingResource_Throws()
        => Assert.Throws<FileNotFoundException>(
            () => ResourceUtils.ReadEmbeddedResource("does-not-exist.bin", TestAssembly)
        );
}
