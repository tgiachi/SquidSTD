using DryIoc;
using SquidStd.Abstractions.Data.Internal.Services;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Abstractions;

public class RegisterStdServiceExtensionTests
{
    [Fact]
    public void RegisterStdService_AddsRegistrationDataWithPriority()
    {
        using var container = new Container();

        container.RegisterStdService<ISquidStdService, FakeStdService>(5);

        var entry = Assert.Single(container.Resolve<List<ServiceRegistrationData>>());
        Assert.Equal(typeof(ISquidStdService), entry.ServiceType);
        Assert.Equal(typeof(FakeStdService), entry.ImplementationType);
        Assert.Equal(5, entry.Priority);
    }

    [Fact]
    public void RegisterStdService_DefaultPriority_IsZero()
    {
        using var container = new Container();

        container.RegisterStdService<ISquidStdService, FakeStdService>();

        var entry = Assert.Single(container.Resolve<List<ServiceRegistrationData>>());
        Assert.Equal(0, entry.Priority);
    }

    [Fact]
    public void RegisterStdService_RegistersAsSingleton()
    {
        using var container = new Container();

        container.RegisterStdService<ISquidStdService, FakeStdService>();

        var first = container.Resolve<ISquidStdService>();
        var second = container.Resolve<ISquidStdService>();

        Assert.Same(first, second);
    }

    [Fact]
    public void RegisterStdService_ResolvesImplementation()
    {
        using var container = new Container();

        container.RegisterStdService<ISquidStdService, FakeStdService>();

        Assert.IsType<FakeStdService>(container.Resolve<ISquidStdService>());
    }

    [Fact]
    public void RegisterStdService_ReturnsSameContainerForChaining()
    {
        using var container = new Container();

        var result = container.RegisterStdService<ISquidStdService, FakeStdService>();

        Assert.Same(container, result);
    }
}
