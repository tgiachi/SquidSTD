using SquidStd.Persistence.MessagePack;

namespace SquidStd.Tests.Persistence;

public class MessagePackDataSerializerTests
{
    public sealed class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void SerializeDeserialize_RoundTrips()
    {
        var serializer = new MessagePackDataSerializer();

        var bytes = serializer.Serialize(new Player { Id = 3, Name = "Zed" });
        var restored = serializer.Deserialize<Player>(bytes);

        Assert.Equal(3, restored.Id);
        Assert.Equal("Zed", restored.Name);
    }

    [Fact]
    public void Serialize_ProducesBinaryNotJsonText()
    {
        var serializer = new MessagePackDataSerializer();

        var bytes = serializer.Serialize(new Player { Id = 1, Name = "A" }).ToArray();

        // MessagePack of an object is not a JSON '{' opener.
        Assert.NotEqual((byte)'{', bytes[0]);
    }
}
