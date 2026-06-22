using SquidStd.Network.Buffers;
using SquidStd.Network.Exceptions.Buffers;

namespace SquidStd.Tests.Network;

public class CircularBufferTests
{
    [Fact]
    public void Clear_ResetsSizeButKeepsCapacity()
    {
        var buffer = new CircularBuffer<int>(4, [1, 2, 3]);
        buffer.Clear();

        Assert.Equal(0, buffer.Size);
        Assert.True(buffer.IsEmpty);
        Assert.Equal(4, buffer.Capacity);
    }

    [Fact]
    public void Constructor_NonPositiveCapacity_Throws()
        => Assert.Throws<ArgumentException>(() => new CircularBuffer<int>(0));

    [Fact]
    public void Constructor_TooManyItems_Throws()
        => Assert.Throws<ArgumentException>(() => new CircularBuffer<int>(2, [1, 2, 3]));

    [Fact]
    public void Constructor_WithItems_PopulatesBuffer()
    {
        var buffer = new CircularBuffer<int>(4, [1, 2, 3]);

        Assert.Equal(3, buffer.Size);
        Assert.Equal([1, 2, 3], buffer.ToArray());
    }

    [Fact]
    public void Enumerator_YieldsLogicalOrder()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.PushBack(10);
        buffer.PushBack(20);
        buffer.PushBack(30);

        Assert.Equal([10, 20, 30], buffer.ToList());
    }

    [Fact]
    public void Front_EmptyBuffer_Throws()
    {
        var buffer = new CircularBuffer<int>(3);

        Assert.Throws<InvalidOperationException>(() => { buffer.Front(); });
    }

    [Fact]
    public void Indexer_AccessesLogicalOrderAfterWrap()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4); // wraps, drops 1 -> [2,3,4]

        Assert.Equal(2, buffer[0]);
        Assert.Equal(3, buffer[1]);
        Assert.Equal(4, buffer[2]);
    }

    [Fact]
    public void Indexer_EmptyBuffer_Throws()
    {
        var buffer = new CircularBuffer<int>(3);

        Assert.Throws<CircularBufferEmptyException>(() => { _ = buffer[0]; });
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var buffer = new CircularBuffer<int>(3, [1]);

        Assert.Throws<CircularBufferIndexOutOfRangeException>(() => { _ = buffer[5]; });
    }

    [Fact]
    public void PopBack_RemovesBackElement()
    {
        var buffer = new CircularBuffer<int>(3, [1, 2, 3]);
        buffer.PopBack();

        Assert.Equal([1, 2], buffer.ToArray());
        Assert.Equal(2, buffer.Back());
    }

    [Fact]
    public void PopFront_RemovesFrontElement()
    {
        var buffer = new CircularBuffer<int>(3, [1, 2, 3]);
        buffer.PopFront();

        Assert.Equal([2, 3], buffer.ToArray());
        Assert.Equal(2, buffer.Front());
    }

    [Fact]
    public void PushBack_WhenFull_DropsOldest()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);

        Assert.True(buffer.IsFull);
        Assert.Equal([2, 3, 4], buffer.ToArray());
        Assert.Equal(2, buffer.Front());
        Assert.Equal(4, buffer.Back());
    }

    [Fact]
    public void PushBack_WithinCapacity_AppendsInOrder()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        Assert.Equal(3, buffer.Size);
        Assert.Equal([1, 2, 3], buffer.ToArray());
        Assert.Equal(1, buffer.Front());
        Assert.Equal(3, buffer.Back());
    }

    [Fact]
    public void PushBackRange_LargerThanCapacity_KeepsLastElements()
    {
        var buffer = new CircularBuffer<int>(4);
        buffer.PushBackRange([1, 2, 3, 4, 5, 6]);

        Assert.True(buffer.IsFull);
        Assert.Equal([3, 4, 5, 6], buffer.ToArray());
    }

    [Fact]
    public void PushBackRange_WithinCapacity_Appends()
    {
        var buffer = new CircularBuffer<int>(8);
        buffer.PushBackRange([1, 2, 3]);
        buffer.PushBackRange([4, 5]);

        Assert.Equal([1, 2, 3, 4, 5], buffer.ToArray());
    }

    [Fact]
    public void PushFront_PrependsInReverseOrder()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.PushFront(1);
        buffer.PushFront(2);
        buffer.PushFront(3);

        Assert.Equal([3, 2, 1], buffer.ToArray());
    }
}
