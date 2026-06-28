using SquidStd.Core.Json;
using SquidStd.Persistence.Data;

namespace SquidStd.Tests.Persistence;

public class PersistenceEntityDescriptorTests
{
    private static PersistenceEntityDescriptor<Player, int> CreateDescriptor()
    {
        var serializer = new JsonDataSerializer();

        return new PersistenceEntityDescriptor<Player, int>(serializer, serializer, 1, "Player", 1, p => p.Id);
    }

    [Fact]
    public void GetKey_UsesSelector()
        => Assert.Equal(5, CreateDescriptor().GetKey(new Player { Id = 5 }));

    [Fact]
    public void SerializeDeserializeEntity_RoundTrips()
    {
        var descriptor = CreateDescriptor();
        var player = new Player { Id = 1, Name = "Bob", Tags = ["a", "b"] };

        var restored = descriptor.DeserializeEntity(descriptor.SerializeEntity(player));

        Assert.Equal("Bob", restored.Name);
        Assert.Equal(["a", "b"], restored.Tags);
    }

    [Fact]
    public void Clone_IsDeep()
    {
        var descriptor = CreateDescriptor();
        var original = new Player { Id = 1, Name = "Bob", Tags = ["a"] };

        var clone = descriptor.Clone(original);
        clone.Tags.Add("mutated");
        clone.Name = "Changed";

        Assert.Equal(["a"], original.Tags);
        Assert.Equal("Bob", original.Name);
    }

    [Fact]
    public void SerializeDeserializeBucket_RoundTrips()
    {
        var descriptor = CreateDescriptor();
        Player[] players = [new() { Id = 1, Name = "A" }, new() { Id = 2, Name = "B" }];

        var restored = descriptor.DeserializeBucket(descriptor.SerializeBucket(players));

        Assert.Equal(2, restored.Count);
    }

    [Fact]
    public void SerializeDeserializeKey_RoundTrips()
    {
        var descriptor = CreateDescriptor();

        Assert.Equal(99, descriptor.DeserializeKey(descriptor.SerializeKey(99)));
    }

    private sealed class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = [];
    }
}
