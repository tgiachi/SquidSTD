using DryIoc;
using SquidStd.Abstractions.Extensions.Container;
using SquidStd.Scripting.Lua.Data.Internal;
using SquidStd.Scripting.Lua.Extensions.Scripts;

namespace SquidStd.Tests.Scripting.Lua;

public class AddScriptModuleExtensionTests
{
    [Fact]
    public void RegisterLuaUserData_AddsUserDataMetadata()
    {
        using var container = new Container();

        container.RegisterLuaUserData<TestUserData>();

        var registration = Assert.Single(container.Resolve<List<ScriptUserData>>());
        Assert.Equal(typeof(TestUserData), registration.UserType);
    }

    [Fact]
    public void RegisterScriptModule_AddsMetadataAndRegistersSingleton()
    {
        using var container = new Container();

        container.RegisterScriptModule<TestModule>();

        var registration = Assert.Single(container.Resolve<List<ScriptModuleData>>());
        Assert.Equal(typeof(TestModule), registration.ModuleType);
        Assert.Same(container.Resolve<TestModule>(), container.Resolve<TestModule>());
    }

    [Fact]
    public void RegisterLuaUserData_NullTypeThrows()
    {
        using var container = new Container();

        Assert.Throws<ArgumentNullException>(() => container.RegisterLuaUserData(null!));
    }

    public sealed class TestUserData;

    public sealed class TestModule;
}
