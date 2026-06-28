using DryIoc;
using MoonSharp.Interpreter;
using SquidStd.Core.Directories;
using SquidStd.Scripting.Lua.Data.Config;
using SquidStd.Scripting.Lua.Data.Internal;
using SquidStd.Scripting.Lua.Data.Scripts;
using SquidStd.Scripting.Lua.Interfaces.Events;
using SquidStd.Scripting.Lua.Services;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Scripting.Lua;

public class LuaScriptEngineServiceTests
{
    [Fact]
    public void AddCallback_AndExecuteCallback_NormalizeNameAndPassArguments()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        using var engine = CreateEngine(temp, container);
        object[]? captured = null;

        engine.AddCallback("onSpawn", args => captured = args);
        engine.ExecuteCallback("on_spawn", "squid", 7);

        Assert.NotNull(captured);
        Assert.Equal(["squid", 7], captured);
    }

    [Fact]
    public void AddConstant_MakesSimpleAndObjectValuesAvailableToLua()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        using var engine = CreateEngine(temp, container);

        engine.AddConstant("engineName", "SquidStd");
        engine.AddConstant("limits", new LimitConfig("jobs", 4));

        Assert.Equal("SquidStd", engine.ExecuteFunction("ENGINE_NAME").Data);
        Assert.Equal("jobs:4", engine.ExecuteFunction("LIMITS.Name .. ':' .. LIMITS.Count").Data);
    }

    [Fact]
    public void AddManualModuleFunction_RegistersActionAndTypedFunction()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        using var engine = CreateEngine(temp, container);
        object[]? captured = null;

        engine.AddManualModuleFunction("testModule", "captureValue", args => captured = args);
        engine.AddManualModuleFunction<double, double>("testModule", "doubleValue", value => value * 2);

        engine.ExecuteScript("test_module.capture_value('abc', 12)");
        var result = engine.ExecuteFunction("test_module.double_value(6)");

        Assert.NotNull(captured);
        Assert.Equal("abc", captured[0]);
        Assert.Equal(12d, captured[1]);
        Assert.Equal(12d, result.Data);
    }

    [Fact]
    public void AddSearchDirectory_AllowsRequireFromAdditionalDirectory()
    {
        using var temp = new TempDirectory();
        using var extra = new TempDirectory();
        using var container = new Container();
        File.WriteAllText(extra.Combine("feature.lua"), "return { value = 42 }");
        using var engine = CreateEngine(temp, container);

        engine.AddSearchDirectory(extra.Path);

        Assert.Equal(42d, engine.ExecuteFunction("require('feature').value").Data);
    }

    [Fact]
    public void ExecuteFunction_RaisesScriptErrorForInvalidExpression()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        using var engine = CreateEngine(temp, container);
        ScriptErrorInfo? captured = null;
        engine.OnScriptError += (_, error) => captured = error;

        var result = engine.ExecuteFunction("missing.value");

        Assert.False(result.Success);
        Assert.NotNull(captured);
        Assert.Contains("nil", captured.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExecuteFunction_ReturnsSuccessForValidExpression()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        using var engine = CreateEngine(temp, container);

        var result = engine.ExecuteFunction("2 + 3");

        Assert.True(result.Success);
        Assert.Equal(5d, result.Data);
    }

    [Fact]
    public void ExecuteScript_TracksCacheHitsAndMisses()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        using var engine = CreateEngine(temp, container);

        engine.ExecuteScript("cached_value = 1");
        engine.ExecuteScript("cached_value = 1");

        var metrics = engine.GetExecutionMetrics();
        Assert.Equal(1, metrics.CacheMisses);
        Assert.Equal(1, metrics.CacheHits);
        Assert.Equal(1, metrics.TotalScriptsCached);
    }

    [Fact]
    public void RegisteredUserData_CanBeConstructedFromLuaWithMoreThanFourArguments()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        using var engine = CreateEngine(
            temp,
            container,
            loadedUserData:
            [
                new ScriptUserData { UserType = typeof(FiveArgumentUserData) }
            ]
        );

        var result = engine.LuaScript.DoString(
            """
            local value = FiveArgumentUserData(1, 2, 3, 4, 5)
            return value.Total
            """
        );

        Assert.Equal(15, (int)result.Number);
    }

    [Fact]
    public void RegisterGlobalAndUnregisterGlobal_UpdateLuaGlobals()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        using var engine = CreateEngine(temp, container);

        engine.RegisterGlobal("answer", 42);

        Assert.Equal(42d, engine.ExecuteFunction("answer").Data);
        Assert.True(engine.UnregisterGlobal("answer"));
        Assert.False(engine.UnregisterGlobal("answer"));
    }

    [Fact]
    public async Task StartAsync_AttachesEventBridgeAndAddsBootstrapConstants()
    {
        using var temp = new TempDirectory();
        using var container = new Container();
        var bridge = new CapturingLuaEventBridge();
        container.RegisterInstance<ILuaEventBridge>(bridge);
        using var engine = CreateEngine(temp, container);
        object? hookArgument = null;
        engine.AfterModulesRegistered += value => hookArgument = value;

        await engine.StartAsync(CancellationToken.None);
        await engine.StartAsync(CancellationToken.None);

        var stats = engine.GetStats();
        Assert.True(stats.IsInitialized);
        Assert.Same(engine.LuaScript, bridge.AttachedScript);
        Assert.Same(engine.LuaScript, hookArgument);
        Assert.Equal("1.0.0", engine.ExecuteFunction("VERSION").Data);
        Assert.Equal("SquidStd", engine.ExecuteFunction("ENGINE").Data);
    }

    private static LuaScriptEngineService CreateEngine(
        TempDirectory temp,
        IContainer container,
        List<ScriptModuleData>? scriptModules = null,
        List<ScriptUserData>? loadedUserData = null
    )
    {
        var scriptsDirectory = temp.Combine("scripts");
        var luarcDirectory = temp.Combine("luarc");
        Directory.CreateDirectory(scriptsDirectory);

        return new LuaScriptEngineService(
            new DirectoriesConfig(temp.Path, []),
            container,
            new LuaEngineConfig(luarcDirectory, scriptsDirectory, "SquidStd", "1.0.0"),
            scriptModules,
            loadedUserData
        );
    }

    public sealed class FiveArgumentUserData(int first, int second, int third, int fourth, int fifth)
    {
        public int Total { get; } = first + second + third + fourth + fifth;
    }

    private sealed class CapturingLuaEventBridge : ILuaEventBridge
    {
        public Script? AttachedScript { get; private set; }

        public void Attach(Script script)
        {
            AttachedScript = script;
        }

        public DynValue Invoke(
            Closure callback,
            IReadOnlyDictionary<string, object?> payload
        )
        {
            return DynValue.Nil;
        }

        public void Publish(string eventName, IReadOnlyDictionary<string, object?> payload)
        {
        }

        public void Register(string eventName, Closure callback)
        {
        }
    }

    private sealed record LimitConfig(string Name, int Count);
}
