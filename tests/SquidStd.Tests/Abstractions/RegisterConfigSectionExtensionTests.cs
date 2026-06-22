using DryIoc;
using SquidStd.Abstractions.Data.Internal.Config;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Abstractions;

public class RegisterConfigSectionExtensionTests
{
    [Fact]
    public void RegisterConfigSection_AddsRegistrationData()
    {
        using var container = new Container();

        container.RegisterConfigSection(
            "test",
            static () => new TestConfig { Name = "default", Count = 2 },
            4
        );

        var entry = Assert.Single(container.Resolve<List<ConfigRegistrationData>>());
        var config = Assert.IsType<TestConfig>(entry.CreateDefault());

        Assert.Equal("test", entry.SectionName);
        Assert.Equal(typeof(TestConfig), entry.ConfigType);
        Assert.Equal(4, entry.Priority);
        Assert.Equal("default", config.Name);
        Assert.Equal(2, config.Count);
        Assert.False(container.IsRegistered<TestConfig>());
    }

    [Fact]
    public void RegisterConfigSection_DuplicateSectionWithDifferentType_Throws()
    {
        using var container = new Container();

        container.RegisterConfigSection<TestConfig>("test");

        Assert.Throws<InvalidOperationException>(() => container.RegisterConfigSection<SampleDto>("test"));
    }

    [Fact]
    public void RegisterConfigSection_DuplicateTypeWithDifferentSection_Throws()
    {
        using var container = new Container();

        container.RegisterConfigSection<TestConfig>("first");

        Assert.Throws<InvalidOperationException>(() => container.RegisterConfigSection<TestConfig>("second"));
    }

    [Fact]
    public void RegisterConfigSection_ReturnsSameContainerForChaining()
    {
        using var container = new Container();

        var result = container.RegisterConfigSection<TestConfig>("test");

        Assert.Same(container, result);
    }

    [Fact]
    public void RegisterConfigSection_SameTypeAndSection_IsIdempotent()
    {
        using var container = new Container();

        container.RegisterConfigSection<TestConfig>("test");
        container.RegisterConfigSection<TestConfig>("test");

        Assert.Single(container.Resolve<List<ConfigRegistrationData>>());
    }
}
