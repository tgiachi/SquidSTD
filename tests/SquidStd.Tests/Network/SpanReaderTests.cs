using SquidStd.Network.Spans;

namespace SquidStd.Tests.Network;

public class SpanReaderTests
{
    [Fact]
    public void ReadAscii_FixedLength_ReadsExactBytes()
    {
        var reader = new SpanReader([(byte)'h', (byte)'i', (byte)'!']);

        Assert.Equal("hi", reader.ReadAscii(2));
        Assert.Equal(1, reader.Remaining);
    }

    [Fact]
    public void ReadAscii_NullTerminated_StopsAtTerminator()
    {
        var reader = new SpanReader([(byte)'h', (byte)'i', 0]);

        Assert.Equal("hi", reader.ReadAscii());
    }

    [Fact]
    public void ReadBoolean_NonZeroIsTrue()
    {
        var reader = new SpanReader([0x01, 0x00]);

        Assert.True(reader.ReadBoolean());
        Assert.False(reader.ReadBoolean());
    }

    [Fact]
    public void ReadByte_AdvancesPosition()
    {
        var reader = new SpanReader([0xAA, 0xBB]);

        Assert.Equal(0xAA, reader.ReadByte());
        Assert.Equal(1, reader.Position);
        Assert.Equal(0xBB, reader.ReadByte());
    }

    [Fact]
    public void ReadByte_BeyondEnd_Throws()
    {
        var reader = new SpanReader([0x01]);
        reader.ReadByte();

        var threw = false;

        try
        {
            reader.ReadByte();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadBytes_ReturnsRequestedSlice()
    {
        var reader = new SpanReader([1, 2, 3, 4, 5]);

        Assert.Equal([1, 2, 3], reader.ReadBytes(3));
        Assert.Equal(2, reader.Remaining);
    }

    [Fact]
    public void ReadInt32_InsufficientData_Throws()
    {
        var reader = new SpanReader([0x01, 0x02]);

        var threw = false;

        try
        {
            reader.ReadInt32();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ReadInt32_ReadsBigEndian()
    {
        var reader = new SpanReader([0x01, 0x02, 0x03, 0x04]);

        Assert.Equal(0x01020304, reader.ReadInt32());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ReadInt32LE_ReadsLittleEndian()
    {
        var reader = new SpanReader([0x04, 0x03, 0x02, 0x01]);

        Assert.Equal(0x01020304, reader.ReadInt32LE());
    }

    [Fact]
    public void RoundTrip_WithSpanWriter_PreservesValues()
    {
        using var writer = new SpanWriter(64);
        writer.Write(0x0A0B0C0D);
        writer.Write((short)0x1122);
        writer.Write(true);
        writer.WriteAsciiNull("squid");

        var reader = new SpanReader(writer.ToArray());

        Assert.Equal(0x0A0B0C0D, reader.ReadInt32());
        Assert.Equal((short)0x1122, reader.ReadInt16());
        Assert.True(reader.ReadBoolean());
        Assert.Equal("squid", reader.ReadAscii());
    }

    [Fact]
    public void Seek_FromBegin_SetsPosition()
    {
        var reader = new SpanReader([1, 2, 3, 4]);
        reader.Seek(2, SeekOrigin.Begin);

        Assert.Equal(2, reader.Position);
        Assert.Equal(3, reader.ReadByte());
    }
}
