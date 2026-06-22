using MoonSharp.Interpreter;
using SquidStd.Scripting.Lua.Extensions;

namespace SquidStd.Tests.Scripting.Lua;

public class LuaTableReaderTests
{
    [Fact]
    public void Readers_ReturnTypedValues()
    {
        var script = new Script();
        var table = new Table(script);
        table["name"] = "squid";
        table["count"] = 3;
        table["ratio"] = 1.5;
        table["enabled"] = true;
        table["mode"] = "Second";

        Assert.Equal("squid", LuaTableReader.GetString(table, "name"));
        Assert.Equal(3, LuaTableReader.GetInt(table, "count"));
        Assert.Equal(1.5f, LuaTableReader.GetFloat(table, "ratio"));
        Assert.True(LuaTableReader.GetBool(table, "enabled"));
        Assert.Equal(TestMode.Second, LuaTableReader.GetEnum(table, "mode", TestMode.First));
    }

    [Fact]
    public void Readers_ReturnDefaultsForMissingOrWrongTypes()
    {
        var script = new Script();
        var table = new Table(script);
        table["name"] = 10;
        table["count"] = "wrong";
        table["mode"] = "Unknown";

        Assert.Equal("default", LuaTableReader.GetString(table, "name", "default"));
        Assert.Equal(7, LuaTableReader.GetInt(table, "count", 7));
        Assert.Equal(2.5f, LuaTableReader.GetFloat(table, "missingFloat", 2.5f));
        Assert.True(LuaTableReader.GetBool(table, "missingBool", true));
        Assert.Equal(TestMode.First, LuaTableReader.GetEnum(table, "mode", TestMode.First));
    }

    private enum TestMode
    {
        First = 1,
        Second = 2
    }
}
