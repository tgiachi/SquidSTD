using System.Net;
using SquidStd.Core.Utils;

namespace SquidStd.Tests.Utils;

public class NetworkUtilsTests
{
    [Fact]
    public void GetListeningAddresses_NullEndpoint_Throws()
        => Assert.Throws<ArgumentNullException>(() => NetworkUtils.GetListeningAddresses(null!).ToList());

    [Fact]
    public void GetListeningAddresses_ReturnsEndpointsMatchingFamilyAndPort()
    {
        var template = new IPEndPoint(IPAddress.Loopback, 6667);

        var results = NetworkUtils.GetListeningAddresses(template).ToList();

        Assert.All(
            results,
            endpoint =>
            {
                Assert.Equal(6667, endpoint.Port);
                Assert.Equal(template.AddressFamily, endpoint.AddressFamily);
            }
        );
    }

    [Theory, InlineData(""), InlineData("   ")]
    public void ParseIpAddress_NullOrWhitespace_Throws(string ipAddress)
        => Assert.Throws<ArgumentException>(() => NetworkUtils.ParseIpAddress(ipAddress));

    [Fact]
    public void ParseIpAddress_ValidAddress_ReturnsParsedAddress()
        => Assert.Equal(IPAddress.Parse("127.0.0.1"), NetworkUtils.ParseIpAddress("127.0.0.1"));

    [Fact]
    public void ParseIpAddress_Wildcard_ReturnsAny()
        => Assert.Equal(IPAddress.Any, NetworkUtils.ParseIpAddress("*"));

    [Theory, InlineData("99999"), InlineData("-1"), InlineData("abc")]
    public void ParsePorts_InvalidPort_ThrowsFormatException(string ports)
        => Assert.Throws<FormatException>(() => NetworkUtils.ParsePorts(ports));

    [Theory, InlineData("8000-7000"), InlineData("1-2-3")]
    public void ParsePorts_InvalidRange_ThrowsFormatException(string ports)
        => Assert.Throws<FormatException>(() => NetworkUtils.ParsePorts(ports));

    [Fact]
    public void ParsePorts_MixedRangeAndList_ReturnsAllPorts()
        => Assert.Equal([6666, 6667, 6668, 6669, 8000], NetworkUtils.ParsePorts("6666-6668,6669,8000"));

    [Theory, InlineData(""), InlineData("   ")]
    public void ParsePorts_NullOrWhitespace_ThrowsArgumentException(string ports)
        => Assert.Throws<ArgumentException>(() => NetworkUtils.ParsePorts(ports));

    [Fact]
    public void ParsePorts_Range_ReturnsExpandedRange()
        => Assert.Equal([6666, 6667, 6668], NetworkUtils.ParsePorts("6666-6668"));

    [Fact]
    public void ParsePorts_SinglePort_ReturnsSingleEntry()
        => Assert.Equal([8000], NetworkUtils.ParsePorts("8000"));

    [Fact]
    public void ParsePorts_TrimsWhitespaceEntries()
        => Assert.Equal([80, 443], NetworkUtils.ParsePorts(" 80 , 443 "));
}
