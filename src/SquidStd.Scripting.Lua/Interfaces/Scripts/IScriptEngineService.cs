using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Scripting.Lua.Data.Scripts;

namespace SquidStd.Scripting.Lua.Interfaces.Scripts;

/// <summary>
/// Interface for the script engine service that manages Lua execution.
/// </summary>
public interface IScriptEngineService : ISquidStdService
{
    /// <summary>
    /// Delegate for handling script file change events.
    /// </summary>
    /// <param name="filePath">The path to the changed file.</param>
    /// <returns>True if the file change was handled successfully, false otherwise.</returns>
    delegate bool LuaFileChangedHandler(string filePath);

    /// <summary>
    /// Event raised when a script file is modified.
    /// </summary>
    event LuaFileChangedHandler? FileChanged;

    /// <summary>
    /// Event raised when a script error occurs
    /// </summary>
    event EventHandler<ScriptErrorInfo>? OnScriptError;

    /// <summary>
    /// Fires once during <c>StartAsync</c>, after script modules have been registered
    /// but before bootstrap scripts run. Handlers can install additional UserData types, globals,
    /// or scanners that depend on the script runtime being ready. The argument is the underlying
    /// MoonSharp <c>Script</c>, typed as <see cref="object" /> so the interface stays
    /// implementation-agnostic; callers cast as needed.
    /// </summary>
    event Action<object>? AfterModulesRegistered;

    /// <summary>
    /// Fires when a <c>.lua</c> file under a <c>components/</c> subdirectory of the scripts
    /// directory changes on disk. Carries the full file path. Used by the engine-side
    /// component loader to hot-reload Lua-defined components.
    /// </summary>
    event Action<string>? OnComponentFileChanged;

    /// <summary>
    /// Adds a callback function that can be called from Lua.
    /// </summary>
    /// <param name="name">The name of the callback function in Lua.</param>
    /// <param name="callback">The C# action to execute when the callback is invoked.</param>
    void AddCallback(string name, Action<object[]> callback);

    /// <summary>
    /// Adds a constant value accessible from Lua.
    /// </summary>
    /// <param name="name">The name of the constant in Lua.</param>
    /// <param name="value">The value of the constant.</param>
    void AddConstant(string name, object value);

    /// <summary>
    /// Adds a script to be executed during engine initialization.
    /// </summary>
    /// <param name="script">The Lua code to execute on startup.</param>
    void AddInitScript(string script);

    /// <summary>
    /// Adds a manual module function that can be called from scripts.
    /// </summary>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="callback">The callback to execute when the function is called.</param>
    void AddManualModuleFunction(string moduleName, string functionName, Action<object[]> callback);

    /// <summary>
    /// Adds a typed manual module function that can be called from scripts.
    /// </summary>
    /// <typeparam name="TInput">The input parameter type.</typeparam>
    /// <typeparam name="TOutput">The output return type.</typeparam>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="callback">The callback function to execute.</param>
    void AddManualModuleFunction<TInput, TOutput>(
        string moduleName,
        string functionName,
        Func<TInput?, TOutput> callback
    );

    /// <summary>
    /// Adds a .NET type as a module accessible from Lua.
    /// </summary>
    /// <param name="type">The type to register as a script module.</param>
    void AddScriptModule(Type type);

    /// <summary>
    /// Adds a directory to the Lua script search paths.
    /// </summary>
    /// <param name="path">Directory path to search for scripts.</param>
    void AddSearchDirectory(string path);

    /// <summary>
    /// Clears the script cache
    /// </summary>
    void ClearScriptCache();

    /// <summary>
    /// Executes a previously registered callback function.
    /// </summary>
    /// <param name="name">The name of the callback to execute.</param>
    /// <param name="args">Arguments to pass to the callback.</param>
    void ExecuteCallback(string name, params object[] args);

    /// <summary>
    /// Notifies the script engine that the engine initialization is complete and ready.
    /// </summary>
    void ExecuteEngineReady();

    /// <summary>
    /// Executes a Lua function or expression and returns the result.
    /// </summary>
    /// <param name="command">The Lua function call or expression to execute.</param>
    /// <returns>A ScriptResult containing the execution outcome.</returns>
    ScriptResult ExecuteFunction(string command);

    /// <summary>
    /// Asynchronously executes a Lua function or expression and returns the result.
    /// </summary>
    /// <param name="command">The Lua function call or expression to execute.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task containing a ScriptResult with the execution outcome.</returns>
    Task<ScriptResult> ExecuteFunctionAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function defined in the bootstrap script.
    /// </summary>
    /// <param name="name"></param>
    void ExecuteFunctionFromBootstrap(string name);

    /// <summary>
    /// Executes a Lua script string.
    /// </summary>
    /// <param name="script">The Lua code to execute.</param>
    void ExecuteScript(string script);

    /// <summary>
    /// Executes a Lua file.
    /// </summary>
    /// <param name="scriptFile">The path to the Lua file to execute.</param>
    void ExecuteScriptFile(string scriptFile);

    /// <summary>
    /// Gets execution metrics for performance monitoring
    /// </summary>
    /// <returns>Metrics about script execution</returns>
    ScriptExecutionMetrics GetExecutionMetrics();

    /// <summary>
    /// Registers a global object/value accessible from scripts.
    /// </summary>
    /// <param name="name">The name of the global in scripts.</param>
    /// <param name="value">The object/value to register.</param>
    void RegisterGlobal(string name, object value);

    /// <summary>
    /// Registers a global function that can be called from scripts.
    /// </summary>
    /// <param name="name">The name of the global function in scripts.</param>
    /// <param name="func">The delegate to register as a global function.</param>
    void RegisterGlobalFunction(string name, Delegate func);

    /// <summary>
    /// Converts a .NET method name to a Lua-compatible function name.
    /// </summary>
    /// <param name="name">The .NET method name to convert.</param>
    /// <returns>The Lua-compatible function name.</returns>
    string ToScriptEngineFunctionName(string name);

    /// <summary>
    /// Unregisters a global function or value.
    /// </summary>
    /// <param name="name">The name of the global to unregister.</param>
    /// <returns>True if the global was found and removed, false otherwise.</returns>
    bool UnregisterGlobal(string name);
}
