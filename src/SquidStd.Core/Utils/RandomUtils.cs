using System.Numerics;
using System.Runtime.CompilerServices;

namespace SquidStd.Core.Utils;

/// <summary>
///     Random value helpers built on <see cref="BuiltInRng" />, including dice and coin-flip mechanics.
/// </summary>
public static class RandomUtils
{
    /// <summary>Returns a random integer in <c>[from, from + count)</c>.</summary>
    /// <param name="from">Inclusive lower bound.</param>
    /// <param name="count">Number of distinct values.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Random(int from, int count)
    {
        return BuiltInRng.Next(from, count);
    }

    /// <summary>Returns a random integer below <paramref name="count" /> (sign-preserving).</summary>
    /// <param name="count">Exclusive bound; negative values mirror the range.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Random(int count)
    {
        return count < 0 ? -BuiltInRng.Next(-count) : BuiltInRng.Next(count);
    }

    /// <summary>Returns a random long in <c>[from, from + count)</c>.</summary>
    /// <param name="from">Inclusive lower bound.</param>
    /// <param name="count">Number of distinct values.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Random(long from, long count)
    {
        return BuiltInRng.Next(from, count);
    }

    /// <summary>Returns a random long below <paramref name="count" /> (sign-preserving).</summary>
    /// <param name="count">Exclusive bound; negative values mirror the range.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Random(long count)
    {
        return count < 0 ? -BuiltInRng.Next(-count) : BuiltInRng.Next(count);
    }

    /// <summary>Fills the buffer with random bytes.</summary>
    /// <param name="buffer">The buffer to fill.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RandomBytes(Span<byte> buffer)
    {
        BuiltInRng.NextBytes(buffer);
    }

    /// <summary>Returns a random double in <c>[0, 1)</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double RandomDouble()
    {
        return BuiltInRng.NextDouble();
    }

    /// <summary>
    ///     Counts heads across <paramref name="amount" /> coin flips, stopping early once
    ///     <paramref name="maximum" /> heads are reached.
    /// </summary>
    /// <param name="amount">Number of coins to flip.</param>
    /// <param name="maximum">Cap on the number of heads to count.</param>
    /// <returns>The number of heads, capped at <paramref name="maximum" />.</returns>
    public static int CoinFlips(int amount, int maximum)
    {
        var heads = 0;

        while (amount > 0)
        {
            var num = amount >= 62
                ? (ulong)BuiltInRng.NextLong()
                : (ulong)BuiltInRng.Next(1L << amount);

            heads += BitOperations.PopCount(num);

            if (heads >= maximum)
            {
                return maximum;
            }

            amount -= 62;
        }

        return heads;
    }

    /// <summary>Counts heads across <paramref name="amount" /> coin flips.</summary>
    /// <param name="amount">Number of coins to flip.</param>
    /// <returns>The number of heads.</returns>
    public static int CoinFlips(int amount)
    {
        var heads = 0;

        while (amount > 0)
        {
            var num = amount >= 62
                ? (ulong)BuiltInRng.NextLong()
                : (ulong)BuiltInRng.Next(1L << amount);

            heads += BitOperations.PopCount(num);

            amount -= 62;
        }

        return heads;
    }

    /// <summary>Rolls <paramref name="amount" /> dice of <paramref name="sides" /> sides plus a bonus.</summary>
    /// <param name="amount">Number of dice.</param>
    /// <param name="sides">Sides per die.</param>
    /// <param name="bonus">Flat bonus added to the total.</param>
    /// <returns>The dice total plus bonus, or <c>0</c> when inputs are non-positive.</returns>
    public static int Dice(int amount, int sides, int bonus)
    {
        if (amount <= 0 || sides <= 0)
        {
            return 0;
        }

        int total;

        if (sides == 2)
        {
            total = CoinFlips(amount);
        }
        else
        {
            total = 0;

            for (var i = 0; i < amount; ++i)
            {
                total += BuiltInRng.Next(1, sides);
            }
        }

        return total + bonus;
    }
}
