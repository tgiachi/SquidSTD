using SquidStd.Plugin.Abstractions.Data;

namespace SquidStd.Tests.PluginAbstractions;

public class PluginMetadataTests
{
    [Fact]
    public void Constructor_SetsRequiredProperties()
    {
        var metadata = new PluginMetadata
        {
            Id = "squidstd.weather",
            Name = "Weather",
            Version = new(2, 1, 0),
            Author = "squid"
        };

        Assert.Equal("squidstd.weather", metadata.Id);
        Assert.Equal("Weather", metadata.Name);
        Assert.Equal(new(2, 1, 0), metadata.Version);
        Assert.Equal("squid", metadata.Author);
    }

    [Fact]
    public void Dependencies_CanBeProvided()
    {
        var metadata = new PluginMetadata
        {
            Id = "id",
            Name = "name",
            Version = new(1, 0),
            Author = "author",
            Dependencies = ["squidstd.core", "squidstd.net"]
        };

        Assert.Equal(["squidstd.core", "squidstd.net"], metadata.Dependencies);
    }

    [Fact]
    public void Dependencies_DefaultsToEmpty()
    {
        var metadata = new PluginMetadata
        {
            Id = "id",
            Name = "name",
            Version = new(1, 0),
            Author = "author"
        };

        Assert.Empty(metadata.Dependencies);
    }

    [Fact]
    public void Description_DefaultsToNull()
    {
        var metadata = new PluginMetadata
        {
            Id = "id",
            Name = "name",
            Version = new(1, 0),
            Author = "author"
        };

        Assert.Null(metadata.Description);
    }
}
