using SquidStd.Network.Extensions;

namespace SquidStd.Tests.Network;

public class PacketExtensionsTests
{
    [Theory, InlineData(0x00, "0x00"), InlineData(0x0F, "0x0F"), InlineData(0xAB, "0xAB"), InlineData(0xFF, "0xFF")]
    public void ToPacketString_FormatsAsTwoDigitHex(byte opCode, string expected)
        => Assert.Equal(expected, opCode.ToPacketString());
}
