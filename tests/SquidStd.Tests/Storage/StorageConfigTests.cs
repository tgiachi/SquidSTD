using SquidStd.Core.Data.Storage;
using SquidStd.Core.Interfaces.Config;

namespace SquidStd.Tests.Storage;

public class StorageConfigTests
{
    [Fact]
    public void StorageConfig_ImplementsConfigEntry()
    {
        IConfigEntry entry = new StorageConfig();

        Assert.Equal("storage", entry.SectionName);
        Assert.Equal(typeof(StorageConfig), entry.ConfigType);
        Assert.IsType<StorageConfig>(entry.CreateDefault());
    }

    [Fact]
    public void SecretsConfig_ImplementsConfigEntry()
    {
        IConfigEntry entry = new SecretsConfig();

        Assert.Equal("secrets", entry.SectionName);
        Assert.Equal(typeof(SecretsConfig), entry.ConfigType);
        Assert.IsType<SecretsConfig>(entry.CreateDefault());
    }
}
