using SquidStd.Crypto.Password.Data;

namespace SquidStd.Tests.Crypto.Password;

public class PbkdfCostTests
{
    [Fact]
    public void Moderate_HasVaultEquivalentDefaults()
    {
        Assert.Equal(65536, PbkdfCost.Moderate.MemoryKib);
        Assert.Equal(3, PbkdfCost.Moderate.Iterations);
        Assert.Equal(1, PbkdfCost.Moderate.Parallelism);
    }

    [Fact]
    public void Presets_AreOrderedByCost()
    {
        Assert.True(PbkdfCost.Interactive.MemoryKib < PbkdfCost.Moderate.MemoryKib);
        Assert.True(PbkdfCost.Moderate.MemoryKib < PbkdfCost.Sensitive.MemoryKib);
    }

    [Fact]
    public void Custom_StoresParameters()
    {
        var cost = new PbkdfCost(memoryKib: 131072, iterations: 4, parallelism: 2);

        Assert.Equal(131072, cost.MemoryKib);
        Assert.Equal(4, cost.Iterations);
        Assert.Equal(2, cost.Parallelism);
    }

    [Theory]
    [InlineData(0, 3, 1)]
    [InlineData(1024, 0, 1)]
    [InlineData(1024, 3, 0)]
    public void Custom_RejectsNonPositiveParameters(int memoryKib, int iterations, int parallelism)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new PbkdfCost(memoryKib, iterations, parallelism));
}
