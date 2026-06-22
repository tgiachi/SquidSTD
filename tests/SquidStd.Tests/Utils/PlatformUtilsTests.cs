using SquidStd.Core.Types;
using SquidStd.Core.Utils;

namespace SquidStd.Tests.Utils;

public class PlatformUtilsTests
{
    [Fact]
    public void GetCurrentPlatform_MatchesOperatingSystemDetection()
    {
        var expected = OperatingSystem.IsWindows() ? PlatformType.Windows :
                       OperatingSystem.IsMacOS() ? PlatformType.MacOS :
                       OperatingSystem.IsLinux() ? PlatformType.Linux : PlatformType.Unknown;

        Assert.Equal(expected, PlatformUtils.GetCurrentPlatform());
    }

    [Fact]
    public void IsRunningOnLinux_MatchesOperatingSystem()
        => Assert.Equal(OperatingSystem.IsLinux(), PlatformUtils.IsRunningOnLinux());

    [Fact]
    public void IsRunningOnMacOS_MatchesOperatingSystem()
        => Assert.Equal(OperatingSystem.IsMacOS(), PlatformUtils.IsRunningOnMacOS());

    [Fact]
    public void IsRunningOnWindows_MatchesOperatingSystem()
        => Assert.Equal(OperatingSystem.IsWindows(), PlatformUtils.IsRunningOnWindows());
}
