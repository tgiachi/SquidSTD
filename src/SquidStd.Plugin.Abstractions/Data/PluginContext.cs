namespace SquidStd.Plugin.Abstractions.Data;

public class PluginContext
{
    public Dictionary<string, object> Data { get; } = new();

    public TData GetData<TData>(string key)
        => (TData)Data[key];
}
