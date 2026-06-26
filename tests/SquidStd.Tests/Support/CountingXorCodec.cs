using SquidStd.Network.Interfaces.Codecs;

namespace SquidStd.Tests.Support;

/// <summary>
/// Deterministic stateful test codec: XORs each byte with a counter-based keystream (seed + position).
/// Decode and Encode keep independent positions, mirroring the separate receive/send state of real
/// stream ciphers. Two codecs created with the same seed cancel out (one encodes, the other decodes).
/// </summary>
public sealed class CountingXorCodec : ITransportCodec
{
    private readonly byte _seed;
    private int _decodePosition;
    private int _encodePosition;

    public CountingXorCodec(byte seed = 0)
    {
        _seed = seed;
    }

    public void Decode(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] ^= (byte)(_seed + _decodePosition++);
        }
    }

    public void Encode(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] ^= (byte)(_seed + _encodePosition++);
        }
    }
}
