using System.Text;
using SquidStd.Core.Utils;

namespace SquidStd.Tests.Core.Utils;

public class ChecksumUtilsTests
{
    [Fact]
    public void Compute_EmptyInput_ReturnsOffsetBasis()
        => Assert.Equal(2166136261u, ChecksumUtils.Compute(ReadOnlySpan<byte>.Empty));

    [Fact]
    public void Compute_KnownVector_MatchesFnv1a()
    {
        // FNV-1a 32-bit of ASCII "a" = 0xE40C292C.
        Assert.Equal(0xE40C292Cu, ChecksumUtils.Compute("a"u8));
    }

    [Fact]
    public void Compute_IsStableAcrossCalls()
    {
        var data = Encoding.UTF8.GetBytes("squidstd-persistence");

        Assert.Equal(ChecksumUtils.Compute(data), ChecksumUtils.Compute(data));
    }

    [Fact]
    public void Compute_DifferentInput_DiffersFromOther()
        => Assert.NotEqual(ChecksumUtils.Compute("a"u8), ChecksumUtils.Compute("b"u8));
}
