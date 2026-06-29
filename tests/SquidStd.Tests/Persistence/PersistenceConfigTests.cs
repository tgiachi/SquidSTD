using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Types.Persistence;

namespace SquidStd.Tests.Persistence;

public class PersistenceConfigTests
{
    [Fact]
    public void DurabilityMode_DefaultsToBuffered()
    {
        Assert.Equal(DurabilityMode.Buffered, new PersistenceConfig().DurabilityMode);
    }

    [Fact]
    public void DurabilityMode_CanBeSetToDurable()
    {
        var config = new PersistenceConfig { DurabilityMode = DurabilityMode.Durable };
        Assert.Equal(DurabilityMode.Durable, config.DurabilityMode);
    }
}
