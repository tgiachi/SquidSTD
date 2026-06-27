using SquidStd.Core.Utils;

namespace SquidStd.Tests.Core.Utils;

[Collection("BuiltInRng")]
public class RandomUtilsTests
{
    [Fact]
    public void Random_WithRange_StaysWithinBounds()
    {
        BuiltInRng.Reset(1);

        for (var i = 0; i < 1000; i++)
        {
            Assert.InRange(RandomUtils.Random(100, 50), 100, 149);
        }
    }

    [Fact]
    public void Random_WithNegativeCount_MirrorsRange()
    {
        BuiltInRng.Reset(1);

        for (var i = 0; i < 1000; i++)
        {
            Assert.InRange(RandomUtils.Random(-10), -9, 0);
        }
    }

    [Fact]
    public void Dice_StaysWithinExpectedBounds()
    {
        BuiltInRng.Reset(1);

        for (var i = 0; i < 1000; i++)
        {
            Assert.InRange(RandomUtils.Dice(3, 6, 2), 3 + 2, 18 + 2);
        }
    }

    [Fact]
    public void Dice_WithNonPositiveInput_ReturnsZero()
    {
        Assert.Equal(0, RandomUtils.Dice(0, 6, 5));
        Assert.Equal(0, RandomUtils.Dice(3, 0, 5));
    }

    [Fact]
    public void CoinFlips_NeverExceedsMaximum()
    {
        BuiltInRng.Reset(1);

        for (var i = 0; i < 1000; i++)
        {
            Assert.InRange(RandomUtils.CoinFlips(100, 10), 0, 10);
        }
    }
}
