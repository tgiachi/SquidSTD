using SquidStd.Core.Json;
using SquidStd.Persistence.Data;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public class PersistenceEntityRegistryTests
{
    private sealed class Player
    {
        public int Id { get; set; }
    }

    private static PersistenceEntityDescriptor<Player, int> Descriptor(ushort typeId = 1)
    {
        var serializer = new JsonDataSerializer();

        return new PersistenceEntityDescriptor<Player, int>(serializer, serializer, typeId, "Player", 1, p => p.Id);
    }

    [Fact]
    public void Register_ThenGetByTypeId_ReturnsDescriptor()
    {
        var registry = new PersistenceEntityRegistry();
        registry.Register(Descriptor());

        Assert.Equal("Player", registry.GetDescriptor(1).TypeName);
        Assert.True(registry.IsRegistered(1));
        Assert.True(registry.IsRegistered<Player, int>());
    }

    [Fact]
    public void GetDescriptor_ByTypePair_ReturnsTyped()
    {
        var registry = new PersistenceEntityRegistry();
        registry.Register(Descriptor());

        Assert.Equal(1, registry.GetDescriptor<Player, int>().TypeId);
    }

    [Fact]
    public void Register_AfterFreeze_Throws()
    {
        var registry = new PersistenceEntityRegistry();
        registry.Freeze();

        Assert.True(registry.IsFrozen);
        Assert.Throws<InvalidOperationException>(() => registry.Register(Descriptor()));
    }

    [Fact]
    public void Register_DuplicateTypeId_Throws()
    {
        var registry = new PersistenceEntityRegistry();
        registry.Register(Descriptor());

        Assert.Throws<InvalidOperationException>(() => registry.Register(Descriptor()));
    }

    [Fact]
    public void GetDescriptor_Unregistered_Throws()
        => Assert.Throws<InvalidOperationException>(() => new PersistenceEntityRegistry().GetDescriptor(99));
}
