using MoonSharp.Interpreter;
using SquidStd.Scripting.Lua.Attributes.Scripts;
using SquidStd.Scripting.Lua.Interfaces.Events;

namespace SquidStd.Scripting.Lua.Modules;

[ScriptModule("events", "Allows Lua scripts to subscribe to named server events.")]
public sealed class EventsModule
{
    private readonly ILuaEventBridge _events;

    public EventsModule(ILuaEventBridge events)
    {
        _events = events;
    }

    [ScriptFunction("on", "Registers a callback for a named server event.")]
    public void On(string eventName, Closure callback)
        => _events.Register(eventName, callback);
}
