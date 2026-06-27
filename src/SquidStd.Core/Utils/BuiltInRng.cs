using System.Runtime.CompilerServices;

namespace SquidStd.Core.Utils;

/// <summary>
///     Ambient pseudo-random generator. Call <see cref="Reset(int)" /> with a fixed seed for reproducible
///     sequences (useful for deterministic simulations and game logic).
/// </summary>
public static class BuiltInRng
{
    /// <summary>The underlying generator instance.</summary>
    public static Random Generator { get; private set; } = new();

    /// <summary>Replaces the generator with a new, non-deterministically seeded instance.</summary>
    public static void Reset()
    {
        Generator = new Random();
    }

    /// <summary>Replaces the generator with one seeded for reproducible sequences.</summary>
    /// <param name="seed">The seed value.</param>
    public static void Reset(int seed)
    {
        Generator = new Random(seed);
    }

    /// <summary>Returns a non-negative random integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Next()
    {
        return Generator.Next();
    }

    /// <summary>Returns a non-negative random integer below <paramref name="maxValue" />.</summary>
    /// <param name="maxValue">Exclusive upper bound.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Next(int maxValue)
    {
        return Generator.Next(maxValue);
    }

    /// <summary>Returns a random integer in <c>[minValue, minValue + count)</c>.</summary>
    /// <param name="minValue">Inclusive lower bound.</param>
    /// <param name="count">Number of distinct values.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Next(int minValue, int count)
    {
        return minValue + Generator.Next(count);
    }

    /// <summary>Returns a non-negative random long below <paramref name="maxValue" />.</summary>
    /// <param name="maxValue">Exclusive upper bound.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Next(long maxValue)
    {
        return Generator.NextInt64(maxValue);
    }

    /// <summary>Returns a random long in <c>[minValue, minValue + count)</c>.</summary>
    /// <param name="minValue">Inclusive lower bound.</param>
    /// <param name="count">Number of distinct values.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Next(long minValue, long count)
    {
        return minValue + Generator.NextInt64(count);
    }

    /// <summary>Returns a non-negative random long.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NextLong()
    {
        return Generator.NextInt64();
    }

    /// <summary>Returns a random double in <c>[0, 1)</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NextDouble()
    {
        return Generator.NextDouble();
    }

    /// <summary>Fills the buffer with random bytes.</summary>
    /// <param name="buffer">The buffer to fill.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NextBytes(Span<byte> buffer)
    {
        Generator.NextBytes(buffer);
    }
}
