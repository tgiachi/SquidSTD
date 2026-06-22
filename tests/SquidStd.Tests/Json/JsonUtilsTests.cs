using System.Text.Json;
using System.Text.Json.Serialization;
using SquidStd.Core.Json;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Json;

public class JsonUtilsTests
{
    public JsonUtilsTests()
    {
        // The global options use a source-generated resolver only, so reflection-based
        // (de)serialization of the test DTOs requires the context to be registered.
        JsonUtils.RegisterJsonContext(TestJsonContext.Default);
    }

    // Local type whose name ends with "Entity" to verify suffix stripping.
    private sealed class UserEntity { }

    [Fact]
    public void AddAndRemoveJsonConverter_MutatesConverterList()
    {
        try
        {
            JsonUtils.AddJsonConverter(new DummyGuidConverter());
            Assert.Contains(JsonUtils.GetJsonConverters(), c => c is DummyGuidConverter);

            // Adding a second instance of the same type is ignored.
            JsonUtils.AddJsonConverter(new DummyGuidConverter());
            Assert.Single(JsonUtils.GetJsonConverters(), c => c is DummyGuidConverter);
        }
        finally
        {
            Assert.True(JsonUtils.RemoveJsonConverter<DummyGuidConverter>());
            Assert.DoesNotContain(JsonUtils.GetJsonConverters(), c => c is DummyGuidConverter);
        }
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsJsonException()
        => Assert.Throws<JsonException>(() => JsonUtils.Deserialize<SampleDto>("{ not valid"));

    [Theory, InlineData(""), InlineData("   ")]
    public void Deserialize_NullOrWhitespace_Throws(string json)
        => Assert.Throws<ArgumentException>(() => JsonUtils.Deserialize<SampleDto>(json));

    [Fact]
    public void Deserialize_WithExplicitContext_ReturnsObject()
    {
        var json = JsonUtils.Serialize(new SampleDto { Name = "ctx", Count = 7 });

        var restored = JsonUtils.Deserialize<SampleDto>(json, TestJsonContext.Default);

        Assert.Equal("ctx", restored.Name);
        Assert.Equal(7, restored.Count);
    }

    [Fact]
    public void DeserializeFromFile_MissingFile_Throws()
        => Assert.Throws<FileNotFoundException>(
            () => JsonUtils.DeserializeFromFile<SampleDto>(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"))
        );

    [Fact]
    public void DeserializeOrDefault_EmptyJson_ReturnsDefault()
    {
        var fallback = new SampleDto { Name = "fallback", Count = -1 };

        Assert.Same(fallback, JsonUtils.DeserializeOrDefault("", fallback));
    }

    [Fact]
    public void DeserializeOrDefault_ValidJson_ReturnsObject()
    {
        var json = JsonUtils.Serialize(new SampleDto { Name = "ok", Count = 1 });

        var result = JsonUtils.DeserializeOrDefault<SampleDto>(json);

        Assert.NotNull(result);
        Assert.Equal("ok", result.Name);
    }

    [Fact]
    public void GetJsonConverters_ContainsDefaultEnumConverter()
        => Assert.Contains(JsonUtils.GetJsonConverters(), converter => converter is JsonStringEnumConverter);

    [Theory, InlineData("UserEntity", "user.schema.json"), InlineData("SampleDto", "sample_dto.schema.json")]
    public void GetSchemaFileName_GeneratesSnakeCaseSchemaName(string typeName, string expected)
    {
        // Map the parameterized type name to a real type with the same Name.
        var type = typeName == "SampleDto" ? typeof(SampleDto) : typeof(UserEntity);

        Assert.Equal(expected, JsonUtils.GetSchemaFileName(type));
    }

    [Theory, InlineData("[1,2,3]", true), InlineData("{\"a\":1}", false), InlineData("invalid", false)]
    public void IsArray_VariousInputs_ReturnsExpected(string json, bool expected)
        => Assert.Equal(expected, JsonUtils.IsArray(json));

    [Theory, InlineData("{\"a\":1}", true), InlineData("[1,2,3]", true), InlineData("not json", false),
     InlineData("", false)]
    public void IsValidJson_VariousInputs_ReturnsExpected(string json, bool expected)
        => Assert.Equal(expected, JsonUtils.IsValidJson(json));

    [Fact]
    public void Serialize_NullObject_Throws()
        => Assert.Throws<ArgumentNullException>(() => JsonUtils.Serialize<SampleDto>(null!));

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesValues()
    {
        var original = new SampleDto { Name = "squid", Count = 42 };

        var json = JsonUtils.Serialize(original);
        var restored = JsonUtils.Deserialize<SampleDto>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Count, restored.Count);
    }

    [Fact]
    public void SerializeToFile_DeserializeFromFile_RoundTrips()
    {
        using var temp = new TempDirectory();
        var path = temp.Combine("nested/sample.json");

        JsonUtils.SerializeToFile(new SampleDto { Name = "file", Count = 9 }, path);
        var restored = JsonUtils.DeserializeFromFile<SampleDto>(path);

        Assert.True(File.Exists(path));
        Assert.Equal("file", restored.Name);
        Assert.Equal(9, restored.Count);
    }
}
