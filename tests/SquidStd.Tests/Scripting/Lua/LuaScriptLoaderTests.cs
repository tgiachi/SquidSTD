using MoonSharp.Interpreter;
using SquidStd.Scripting.Lua.Loaders;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Scripting.Lua;

public class LuaScriptLoaderTests
{
    [Fact]
    public void ScriptFileExists_FindsModulePathPatterns()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Combine("modules"));
        Directory.CreateDirectory(temp.Combine(Path.Combine("modules", "inventory")));
        File.WriteAllText(temp.Combine("plain.lua"), "return 1");
        File.WriteAllText(temp.Combine(Path.Combine("modules", "combat.lua")), "return 2");
        File.WriteAllText(temp.Combine(Path.Combine("modules", "inventory", "init.lua")), "return 3");
        var loader = new LuaScriptLoader(temp.Path);

        Assert.True(loader.ScriptFileExists("plain"));
        Assert.True(loader.ScriptFileExists("combat"));
        Assert.True(loader.ScriptFileExists("inventory"));
        Assert.False(loader.ScriptFileExists("missing"));
    }

    [Fact]
    public void LoadFile_LoadsContentFromFirstMatchingSearchDirectory()
    {
        using var first = new TempDirectory();
        using var second = new TempDirectory();
        File.WriteAllText(second.Combine("feature.lua"), "return 'loaded'");
        var loader = new LuaScriptLoader([first.Path, second.Path]);

        var content = loader.LoadFile("feature.lua", new Table(new Script()));

        Assert.Equal("return 'loaded'", content);
    }

    [Fact]
    public void AddSearchDirectory_AddsAdditionalLookupRoot()
    {
        using var first = new TempDirectory();
        using var second = new TempDirectory();
        File.WriteAllText(second.Combine("extra.lua"), "return 'extra'");
        var loader = new LuaScriptLoader(first.Path);

        loader.AddSearchDirectory(second.Path);

        Assert.True(loader.ScriptFileExists("extra"));
    }

    [Fact]
    public void Constructor_EmptySearchDirectoriesThrows()
        => Assert.Throws<ArgumentException>(() => new LuaScriptLoader(Array.Empty<string>()));
}
