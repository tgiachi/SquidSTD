using SquidStd.Messaging.Abstractions;
using SquidStd.Messaging.Abstractions.Interfaces;

namespace SquidStd.Tests.Messaging;

public class JsonMessageSerializerTests
{
    private sealed class Sample
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    [Fact]
    public void SerializeDeserialize_RoundTrips()
    {
        IMessageSerializer serializer = new JsonMessageSerializer();
        var original = new Sample { Name = "squid", Count = 7 };

        var bytes = serializer.Serialize(original);
        var restored = serializer.Deserialize<Sample>(bytes);

        Assert.Equal("squid", restored.Name);
        Assert.Equal(7, restored.Count);
    }
}
