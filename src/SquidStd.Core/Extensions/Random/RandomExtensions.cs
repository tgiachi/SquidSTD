using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SquidStd.Core.Utils;

namespace SquidStd.Core.Extensions.Random;

/// <summary>
///     Shuffling and random-selection helpers over collections, built on <see cref="BuiltInRng" />.
/// </summary>
public static class RandomExtensions
{
    /// <param name="array">The array to operate on.</param>
    extension<T>(T[] array)
    {
        /// <summary>Shuffles the array in place.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Shuffle()
        {
            BuiltInRng.Generator.Shuffle(array.AsSpan());
        }

        /// <summary>Returns a random element from the array.</summary>
        /// <returns>A random element, or <c>default</c> when the array is empty.</returns>
        public T? RandomElement()
        {
            return array.Length == 0 ? default : array[RandomUtils.Random(array.Length)];
        }

        /// <summary>Returns a random sample of distinct elements without modifying the source.</summary>
        /// <param name="count">Number of elements to sample.</param>
        /// <returns>The sampled elements.</returns>
        public T[] RandomSample(int count)
        {
            if (count <= 0)
            {
                return [];
            }

            var length = array.Length;
            var picked = new bool[length];
            var sample = new T[count];

            var i = 0;

            do
            {
                var index = RandomUtils.Random(length);

                if (!picked[index])
                {
                    picked[index] = true;
                    sample[i++] = array[index];
                }
            } while (i < count);

            return sample;
        }
    }

    /// <param name="list">The list to operate on.</param>
    extension<T>(List<T> list)
    {
        /// <summary>Shuffles the list in place.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Shuffle()
        {
            BuiltInRng.Generator.Shuffle(CollectionsMarshal.AsSpan(list));
        }

        /// <summary>Returns a random sample of distinct elements without modifying the source.</summary>
        /// <param name="count">Number of elements to sample.</param>
        /// <returns>The sampled elements.</returns>
        public List<T> RandomSample(int count)
        {
            if (count <= 0)
            {
                return [];
            }

            var length = list.Count;
            var picked = new bool[length];
            var sample = new List<T>(count);

            var i = 0;

            do
            {
                var index = RandomUtils.Random(length);

                if (!picked[index])
                {
                    picked[index] = true;
                    sample.Add(list[index]);
                    i++;
                }
            } while (i < count);

            return sample;
        }
    }

    /// <param name="list">The list to read from.</param>
    extension<T>(IList<T> list)
    {
        /// <summary>Returns a random element from the list.</summary>
        /// <returns>A random element, or <c>default</c> when the list is empty.</returns>
        public T? RandomElement()
        {
            return list.Count == 0 ? default : list[RandomUtils.Random(list.Count)];
        }
    }
}
