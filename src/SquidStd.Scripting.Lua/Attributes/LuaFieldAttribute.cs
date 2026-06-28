namespace SquidStd.Scripting.Lua.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class LuaFieldAttribute : Attribute
{
    public string Name { get; }

    public LuaFieldAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
