using SquidStd.Abstractions.Data.Internal.Config;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Abstractions;

public class ConfigRegistrationDataTests
{
    [Fact]
    public void CreateDefault_IncompatibleFactory_Throws()
    {
        var entry = new ConfigRegistrationData("test", typeof(TestConfig), () => new());

        Assert.Throws<InvalidOperationException>(() => entry.CreateDefault());
    }

    [Fact]
    public void CreateDefault_UsesFactory()
    {
        var entry = new ConfigRegistrationData(
            "test",
            typeof(TestConfig),
            () => new TestConfig { Name = "default", Count = 7 },
            3
        );

        var config = Assert.IsType<TestConfig>(entry.CreateDefault());

        Assert.Equal("test", entry.SectionName);
        Assert.Equal(typeof(TestConfig), entry.ConfigType);
        Assert.Equal(3, entry.Priority);
        Assert.Equal("default", config.Name);
        Assert.Equal(7, config.Count);
    }

    [Fact]
    public void Ctor_InvalidSectionName_Throws()
        => Assert.Throws<ArgumentException>(
            () => new ConfigRegistrationData(string.Empty, typeof(TestConfig), () => new TestConfig())
        );

    [Fact]
    public void Ctor_NullType_Throws()
        => Assert.Throws<ArgumentNullException>(() => new ConfigRegistrationData("test", null!, () => new TestConfig()));
}
