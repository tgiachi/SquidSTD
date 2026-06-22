using SquidStd.Core.Yaml;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Yaml;

public class YamlUtilsTests
{
    [Theory, InlineData(""), InlineData("   ")]
    public void Deserialize_NullOrWhitespace_Throws(string yaml)
        => Assert.Throws<ArgumentException>(() => YamlUtils.Deserialize<SampleDto>(yaml));

    [Fact]
    public void Deserialize_RuntimeType_ReturnsTypedObject()
    {
        const string yaml = """
                            Name: runtime
                            Count: 8
                            """;

        var runtimeType = Type.GetType(typeof(SampleDto).AssemblyQualifiedName!)!;
        var result = YamlUtils.Deserialize(yaml, runtimeType);
        var dto = Assert.IsType<SampleDto>(result);

        Assert.Equal("runtime", dto.Name);
        Assert.Equal(8, dto.Count);
    }

    [Fact]
    public void DeserializeFromFile_MissingFile_Throws()
        => Assert.Throws<FileNotFoundException>(
            () => YamlUtils.DeserializeFromFile<SampleDto>(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".yaml"))
        );

    [Fact]
    public void DeserializeSection_ExistingSection_ReturnsTypedObject()
    {
        const string yaml = """
                            sample:
                              Name: section
                              Count: 11
                            """;

        var result = YamlUtils.DeserializeSection(yaml, "sample", typeof(SampleDto));
        var dto = Assert.IsType<SampleDto>(result);

        Assert.Equal("section", dto.Name);
        Assert.Equal(11, dto.Count);
    }

    [Fact]
    public void DeserializeSection_MissingSection_ReturnsNull()
    {
        const string yaml = """
                            other:
                              Name: section
                              Count: 11
                            """;

        var result = YamlUtils.DeserializeSection(yaml, "sample", typeof(SampleDto));

        Assert.Null(result);
    }

    [Fact]
    public void Serialize_NullObject_Throws()
        => Assert.Throws<ArgumentNullException>(() => YamlUtils.Serialize<SampleDto>(null!));

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesValues()
    {
        var original = new SampleDto { Name = "squid", Count = 42 };

        var yaml = YamlUtils.Serialize(original);
        var restored = YamlUtils.Deserialize<SampleDto>(yaml);

        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Count, restored.Count);
    }

    [Fact]
    public void SerializeSections_WritesRootSectionNames()
    {
        var sections = new Dictionary<string, object>
        {
            ["sample"] = new SampleDto { Name = "root", Count = 12 }
        };

        var yaml = YamlUtils.SerializeSections(sections);

        Assert.Contains("sample:", yaml);
        Assert.Contains("Name: root", yaml);
        Assert.Contains("Count: 12", yaml);
    }

    [Fact]
    public void SerializeToFile_DeserializeFromFile_RoundTrips()
    {
        using var temp = new TempDirectory();
        var path = temp.Combine("nested/sample.yaml");

        YamlUtils.SerializeToFile(new SampleDto { Name = "file", Count = 9 }, path);
        var restored = YamlUtils.DeserializeFromFile<SampleDto>(path);

        Assert.True(File.Exists(path));
        Assert.Equal("file", restored.Name);
        Assert.Equal(9, restored.Count);
    }
}
