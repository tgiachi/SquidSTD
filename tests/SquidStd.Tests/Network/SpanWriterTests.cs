using SquidStd.Network.Spans;

namespace SquidStd.Tests.Network;

public class SpanWriterTests
{
    [Fact]
    public void NoResize_Overflow_Throws()
    {
        var buffer = new byte[2];
        var writer = new SpanWriter(buffer);

        var threw = false;

        try
        {
            writer.Write(0x01020304);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void Resize_GrowsBeyondInitialCapacity()
    {
        using var writer = new SpanWriter(2, true);
        writer.Write(0x01020304);
        writer.Write(0x05060708);

        Assert.Equal(8, writer.BytesWritten);
    }

    [Fact]
    public void ToArray_EmptyWriter_ReturnsEmpty()
    {
        using var writer = new SpanWriter(8);

        Assert.Empty(writer.ToArray());
    }

    [Fact]
    public void Write_Int32_WritesBigEndian()
    {
        using var writer = new SpanWriter(8);
        writer.Write(0x01020304);

        Assert.Equal([0x01, 0x02, 0x03, 0x04], writer.ToArray());
    }

    [Fact]
    public void Write_TracksPositionAndBytesWritten()
    {
        using var writer = new SpanWriter(16);
        writer.Write((byte)1);
        writer.Write((short)2);

        Assert.Equal(3, writer.Position);
        Assert.Equal(3, writer.BytesWritten);
    }

    [Fact]
    public void WriteAsciiNull_AppendsNullTerminator()
    {
        using var writer = new SpanWriter(16);
        writer.WriteAsciiNull("hi");

        Assert.Equal([(byte)'h', (byte)'i', 0], writer.ToArray());
    }

    [Fact]
    public void WriteLE_Int32_WritesLittleEndian()
    {
        using var writer = new SpanWriter(8);
        writer.WriteLE(0x01020304);

        Assert.Equal([0x04, 0x03, 0x02, 0x01], writer.ToArray());
    }
}
