using MoonSharp.Interpreter;

namespace SquidStd.Scripting.Lua.Extensions;

public static class LuaTableReader
{
    public static bool GetBool(Table table, string key, bool defaultValue = false)
    {
        var value = GetValue(table, key);

        return value.Type == DataType.Boolean ? value.Boolean : defaultValue;
    }

    public static TEnum GetEnum<TEnum>(Table table, string key, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        var value = GetValue(table, key);

        if (value.Type == DataType.String &&
            Enum.TryParse(value.String, true, out TEnum parsedByName))
        {
            return parsedByName;
        }

        if (value.Type == DataType.Number)
        {
            var numericValue = (int)value.Number;

            if (Enum.IsDefined(typeof(TEnum), numericValue))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
            }
        }

        return defaultValue;
    }

    public static float GetFloat(Table table, string key, float defaultValue = 0f)
    {
        var value = GetValue(table, key);

        return value.Type == DataType.Number ? (float)value.Number : defaultValue;
    }

    public static int GetInt(Table table, string key, int defaultValue = 0)
    {
        var value = GetValue(table, key);

        return value.Type == DataType.Number ? (int)value.Number : defaultValue;
    }

    public static string GetString(Table table, string key, string defaultValue = "")
    {
        var value = GetValue(table, key);

        return value.Type == DataType.String ? value.String : defaultValue;
    }

    private static DynValue GetValue(Table table, string key)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return table.Get(key);
    }
}
