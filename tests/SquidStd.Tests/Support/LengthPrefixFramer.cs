using SquidStd.Network.Interfaces.Framing;

namespace SquidStd.Tests.Support;

/// <summary>
/// Test framer: a single length-prefix byte followed by that many payload bytes. The emitted frame
/// length includes the prefix byte.
/// </summary>
public sealed class LengthPrefixFramer : INetFramer
{
    public bool TryReadFrame(ReadOnlySpan<byte> buffer, out int frameLength)
    {
        frameLength = 0;

        if (buffer.Length < 1)
        {
            return false;
        }

        var total = 1 + buffer[0];

        if (buffer.Length < total)
        {
            return false;
        }

        frameLength = total;

        return true;
    }
}
