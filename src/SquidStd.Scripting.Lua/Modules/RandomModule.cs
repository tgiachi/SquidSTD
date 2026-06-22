using MoonSharp.Interpreter;
using SquidStd.Scripting.Lua.Attributes.Scripts;

namespace SquidStd.Scripting.Lua.Modules;

[ScriptModule("random", "Provides safe random helpers to Lua scripts.")]
public sealed class RandomModule
{
    [ScriptFunction("chance", "Returns true when a random roll is within the given percentage.")]
    public bool Chance(double percent)
    {
        if (percent <= 0)
        {
            return false;
        }

        if (percent >= 100)
        {
            return true;
        }

        return Random.Shared.NextDouble() * 100 < percent;
    }

    [ScriptFunction("float", "Returns a random floating-point number between 0 and 1.")]
    public double Float()
        => Random.Shared.NextDouble();

    [ScriptFunction("int", "Returns a random integer in the inclusive range.")]
    public int Int(int min, int max)
    {
        if (max < min)
        {
            throw new ArgumentOutOfRangeException(nameof(max), "Max must be greater than or equal to min.");
        }

        if (max == int.MaxValue)
        {
            return Random.Shared.Next(min, max) + Random.Shared.Next(0, 2);
        }

        return Random.Shared.Next(min, max + 1);
    }

    [ScriptFunction("pick", "Returns one random value from a Lua array table.")]
    public DynValue Pick(Table values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var length = values.Length;

        if (length <= 0)
        {
            throw new ArgumentException("Values table cannot be empty.", nameof(values));
        }

        return values.Get(Random.Shared.Next(1, length + 1));
    }

    [ScriptFunction("weighted", "Returns one value from weighted Lua entries.")]
    public DynValue Weighted(Table entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var length = entries.Length;
        var total = 0d;

        for (var i = 1; i <= length; i++)
        {
            var entry = entries.Get(i).Table;
            total += ReadWeight(entry);
        }

        if (total <= 0)
        {
            throw new ArgumentException("At least one weighted entry must have a positive weight.", nameof(entries));
        }

        var roll = Random.Shared.NextDouble() * total;
        var cursor = 0d;

        for (var i = 1; i <= length; i++)
        {
            var entry = entries.Get(i).Table;
            cursor += ReadWeight(entry);

            if (roll <= cursor)
            {
                return entry.Get("value");
            }
        }

        return entries.Get(length).Table.Get("value");
    }

    private static double ReadWeight(Table entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var weight = entry.Get("weight");

        if (weight.Type != DataType.Number)
        {
            throw new ArgumentException("Weighted entries must contain a numeric weight.");
        }

        return Math.Max(0, weight.Number);
    }
}
