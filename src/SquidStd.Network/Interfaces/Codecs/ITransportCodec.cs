namespace SquidStd.Network.Interfaces.Codecs;

/// <summary>
/// An in-place, length-preserving transport transform applied at the wire — for example a stream cipher.
/// </summary>
/// <remarks>
/// Implementations MUST preserve the buffer length and transform in place. <see cref="Decode" /> is invoked
/// only from a connection's receive loop (serial), and <see cref="Encode" /> only from its send path (serial
/// under the send lock). The two directions may run concurrently (one each), so an implementation MUST NOT
/// share mutable state between decode and encode (real ciphers keep separate receive/send positions).
/// </remarks>
public interface ITransportCodec
{
    /// <summary>Transforms inbound bytes in place, before the middleware pipeline.</summary>
    /// <param name="buffer">The bytes to transform in place.</param>
    void Decode(Span<byte> buffer);

    /// <summary>Transforms outbound bytes in place, after the middleware pipeline.</summary>
    /// <param name="buffer">The bytes to transform in place.</param>
    void Encode(Span<byte> buffer);
}
