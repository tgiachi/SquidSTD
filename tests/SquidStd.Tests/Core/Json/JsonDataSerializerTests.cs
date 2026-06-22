using SquidStd.Core.Json;

namespace SquidStd.Tests.Core.Json;

public class JsonDataSerializerTests
{
    private sealed class Sample
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    [Fact]
    public void RoundTrip_PreservesValue()
    {
        var serializer = new JsonDataSerializer();
        var bytes = serializer.Serialize(new Sample { Name = "abc", Count = 7 });

        var result = serializer.Deserialize<Sample>(bytes);

        Assert.Equal("abc", result.Name);
        Assert.Equal(7, result.Count);
    }

    [Fact]
    public void Deserialize_NullJson_Throws()
    {
        var serializer = new JsonDataSerializer();
        var bytes = serializer.Serialize<Sample?>(null);

        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize<Sample>(bytes));
    }
}
