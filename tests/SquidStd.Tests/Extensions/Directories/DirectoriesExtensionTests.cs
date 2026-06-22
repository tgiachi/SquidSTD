using SquidStd.Core.Extensions.Directories;

namespace SquidStd.Tests.Extensions.Directories;

public class DirectoriesExtensionTests
{
    [Theory, InlineData(""), InlineData("   "), InlineData(null)]
    public void ResolvePathAndEnvs_NullOrWhitespace_ReturnsNull(string? path)
        => Assert.Null(path!.ResolvePathAndEnvs());

    [Fact]
    public void ResolvePathAndEnvs_TildePrefix_ExpandsToUserProfile()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var result = "~/sub".ResolvePathAndEnvs();

        Assert.Equal(home + "/sub", result);
    }
}
