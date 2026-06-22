using SquidStd.Plugin.Abstractions.Data;

namespace SquidStd.Tests.PluginAbstractions;

public class PluginContextTests
{
    [Fact]
    public void Data_IsEmptyByDefault()
        => Assert.Empty(new PluginContext().Data);

    [Fact]
    public void GetData_MissingKey_Throws()
        => Assert.Throws<KeyNotFoundException>(() => { _ = new PluginContext().GetData<int>("missing"); });

    [Fact]
    public void GetData_ReferenceType_ReturnsStoredValue()
    {
        var context = new PluginContext();
        var payload = new object();
        context.Data["payload"] = payload;

        Assert.Same(payload, context.GetData<object>("payload"));
    }

    [Fact]
    public void GetData_ValueType_ReturnsStoredValue()
    {
        var context = new PluginContext();
        context.Data["count"] = 42;

        Assert.Equal(42, context.GetData<int>("count"));
    }

    [Fact]
    public void GetData_WrongType_Throws()
    {
        var context = new PluginContext();
        context.Data["count"] = 42;

        Assert.Throws<InvalidCastException>(() => { _ = context.GetData<string>("count"); });
    }
}
