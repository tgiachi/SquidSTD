using SquidStd.Abstractions.Data.Internal.Services;

namespace SquidStd.Tests.Abstractions;

public class ServiceRegistrationDataTests
{
    [Fact]
    public void Constructor_DefaultPriority_IsZero()
    {
        var data = new ServiceRegistrationData(typeof(IDisposable), typeof(string));

        Assert.Equal(0, data.Priority);
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var data = new ServiceRegistrationData(typeof(IDisposable), typeof(string), 3);

        Assert.Equal(typeof(IDisposable), data.ServiceType);
        Assert.Equal(typeof(string), data.ImplementationType);
        Assert.Equal(3, data.Priority);
    }

    [Fact]
    public void Records_WithDifferentPriority_AreNotEqual()
    {
        var first = new ServiceRegistrationData(typeof(IDisposable), typeof(string), 1);
        var second = new ServiceRegistrationData(typeof(IDisposable), typeof(string), 2);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        var first = new ServiceRegistrationData(typeof(IDisposable), typeof(string), 1);
        var second = new ServiceRegistrationData(typeof(IDisposable), typeof(string), 1);

        Assert.Equal(first, second);
    }
}
