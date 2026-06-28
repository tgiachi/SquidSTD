using SquidStd.Vfs.Abstractions;

namespace SquidStd.Tests.Vfs;

public class VfsPathTests
{
    [Theory]
    [InlineData("docs/cv.pdf", "docs/cv.pdf")]
    [InlineData("/docs//cv.pdf", "docs/cv.pdf")]
    [InlineData("docs\\cv.pdf", "docs/cv.pdf")]
    public void Normalize_ProducesForwardSlashRelativePath(string input, string expected)
    {
        Assert.Equal(expected, VfsPath.Normalize(input));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("a/../../b")]
    public void Normalize_RejectsTraversal(string input)
    {
        Assert.Throws<ArgumentException>(() => VfsPath.Normalize(input));
    }
}
