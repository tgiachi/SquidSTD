using SquidStd.Tests.Support;

namespace SquidStd.Tests.Network;

public class TransportCodecFakeTests
{
    [Fact]
    public void EncodeThenDecode_WithMatchingCodecs_RestoresOriginal()
    {
        var original = new byte[] { 10, 20, 30, 40, 50 };
        var buffer = (byte[])original.Clone();

        new CountingXorCodec(7).Encode(buffer);
        Assert.NotEqual(original, buffer);

        new CountingXorCodec(7).Decode(buffer);
        Assert.Equal(original, buffer);
    }

    [Fact]
    public void Decode_AdvancesIndependentlyFromEncode()
    {
        var codec = new CountingXorCodec(0);
        var encodeProbe = new byte[] { 0, 0, 0 };
        var decodeProbe = new byte[] { 0, 0, 0 };

        codec.Encode(encodeProbe);
        codec.Decode(decodeProbe);

        // Both directions started at position 0, so they produce the same keystream.
        Assert.Equal(encodeProbe, decodeProbe);
    }
}
