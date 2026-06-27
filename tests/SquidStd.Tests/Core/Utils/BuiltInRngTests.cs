using SquidStd.Core.Utils;

namespace SquidStd.Tests.Core.Utils;

[Collection("BuiltInRng")]
public class BuiltInRngTests
{
    [Fact]
    public void Reset_WithSeed_ProducesReproducibleSequence()
    {
        BuiltInRng.Reset(12345);
        var first = new[] { BuiltInRng.Next(), BuiltInRng.Next(1000), BuiltInRng.NextLong() };

        BuiltInRng.Reset(12345);
        var second = new[] { BuiltInRng.Next(), BuiltInRng.Next(1000), BuiltInRng.NextLong() };

        Assert.Equal(first, second);
    }

    [Fact]
    public void Next_WithBounds_StaysInRange()
    {
        BuiltInRng.Reset(7);

        for (var i = 0; i < 1000; i++)
        {
            var value = BuiltInRng.Next(10, 5);

            Assert.InRange(value, 10, 14);
        }
    }

    [Fact]
    public void NextDouble_StaysInUnitInterval()
    {
        BuiltInRng.Reset(7);

        for (var i = 0; i < 1000; i++)
        {
            var value = BuiltInRng.NextDouble();

            Assert.InRange(value, 0.0, 1.0);
        }
    }
}
