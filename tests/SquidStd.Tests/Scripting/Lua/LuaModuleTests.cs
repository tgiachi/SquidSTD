using MoonSharp.Interpreter;
using SquidStd.Scripting.Lua.Interfaces.Events;
using SquidStd.Scripting.Lua.Modules;

namespace SquidStd.Tests.Scripting.Lua;

public class LuaModuleTests
{
    [Fact]
    public void RandomModule_ChanceHandlesBoundaries()
    {
        var module = new RandomModule();

        Assert.False(module.Chance(0));
        Assert.False(module.Chance(-1));
        Assert.True(module.Chance(100));
        Assert.True(module.Chance(101));
    }

    [Fact]
    public void RandomModule_IntRejectsInvalidRange()
    {
        var module = new RandomModule();

        Assert.Throws<ArgumentOutOfRangeException>(() => module.Int(5, 4));
    }

    [Fact]
    public void RandomModule_PickRejectsEmptyTable()
    {
        var module = new RandomModule();

        Assert.Throws<ArgumentException>(() => module.Pick(new Table(new Script())));
    }

    [Fact]
    public void RandomModule_WeightedRejectsEntriesWithoutPositiveWeight()
    {
        var script = new Script();
        var entries = script.DoString(
            """
            return {
                { value = 'a', weight = 0 },
                { value = 'b', weight = -3 }
            }
            """
        ).Table;
        var module = new RandomModule();

        Assert.Throws<ArgumentException>(() => module.Weighted(entries));
    }

    [Fact]
    public void EventsModule_RegistersCallbackWithBridge()
    {
        var script = new Script();
        var bridge = new CapturingLuaEventBridge();
        var callback = script.DoString("return function() end").Function;
        var module = new EventsModule(bridge);

        module.On("spawned", callback);

        Assert.Equal("spawned", bridge.EventName);
        Assert.Same(callback, bridge.Callback);
    }

    private sealed class CapturingLuaEventBridge : ILuaEventBridge
    {
        public Closure? Callback { get; private set; }

        public string? EventName { get; private set; }

        public void Attach(Script script)
        {
        }

        public DynValue Invoke(Closure callback, IReadOnlyDictionary<string, object?> payload)
            => DynValue.Nil;

        public void Publish(string eventName, IReadOnlyDictionary<string, object?> payload)
        {
        }

        public void Register(string eventName, Closure callback)
        {
            EventName = eventName;
            Callback = callback;
        }
    }
}
