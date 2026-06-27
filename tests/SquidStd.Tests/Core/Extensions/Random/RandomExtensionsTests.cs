using SquidStd.Core.Extensions.Random;
using SquidStd.Core.Utils;

namespace SquidStd.Tests.Core.Extensions.Random;

[Collection("BuiltInRng")]
public class RandomExtensionsTests
{
    [Fact]
    public void Shuffle_PreservesElements()
    {
        BuiltInRng.Reset(1);
        var array = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        array.Shuffle();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], array.OrderBy(static x => x));
    }

    [Fact]
    public void Shuffle_OnList_PreservesElements()
    {
        BuiltInRng.Reset(1);
        var list = new List<int> { 1, 2, 3, 4, 5 };

        list.Shuffle();

        Assert.Equal([1, 2, 3, 4, 5], list.OrderBy(static x => x));
    }

    [Fact]
    public void RandomElement_ReturnsElementFromSource()
    {
        BuiltInRng.Reset(1);
        var array = new[] { 10, 20, 30 };

        for (var i = 0; i < 100; i++)
        {
            Assert.Contains(array.RandomElement(), array);
        }
    }

    [Fact]
    public void RandomElement_OnEmpty_ReturnsDefault()
    {
        Assert.Equal(0, Array.Empty<int>().RandomElement());
    }

    [Fact]
    public void RandomElement_OnList_ReturnsElementFromSource()
    {
        BuiltInRng.Reset(1);
        IList<string> list = ["a", "b", "c"];

        Assert.Contains(list.RandomElement(), list);
    }

    [Fact]
    public void RandomSample_ReturnsDistinctElementsFromSource()
    {
        BuiltInRng.Reset(1);
        var source = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var sample = source.RandomSample(4);

        Assert.Equal(4, sample.Length);
        Assert.Equal(4, sample.Distinct().Count());
        Assert.All(sample, value => Assert.Contains(value, source));
    }

    [Fact]
    public void RandomSample_OnList_ReturnsDistinctElementsFromSource()
    {
        BuiltInRng.Reset(1);
        var source = new List<int> { 1, 2, 3, 4, 5, 6 };

        var sample = source.RandomSample(3);

        Assert.Equal(3, sample.Count);
        Assert.Equal(3, sample.Distinct().Count());
        Assert.All(sample, value => Assert.Contains(value, source));
    }
}
