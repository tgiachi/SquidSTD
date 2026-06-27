using SquidStd.Core.Extensions.Strings;

namespace SquidStd.Tests.Core.Extensions.Strings;

public class Base64ExtensionsTests
{
    [Fact]
    public void ToBase64_FromBase64_RoundTrips()
    {
        const string original = "squid std";

        var encoded = original.ToBase64();

        Assert.Equal(original, encoded.FromBase64());
    }

    [Fact]
    public void ToBase64_OnByteArray_EncodesBytes()
    {
        byte[] bytes = [1, 2, 3, 4];

        Assert.Equal(Convert.ToBase64String(bytes), bytes.ToBase64());
    }

    [Fact]
    public void FromBase64ToByteArray_DecodesBytes()
    {
        byte[] bytes = [10, 20, 30];
        var encoded = bytes.ToBase64();

        Assert.Equal(bytes, encoded.FromBase64ToByteArray());
    }

    [Theory]
    [InlineData("c3F1aWQ=", true)]
    [InlineData("not base64!", false)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    public void IsBase64String_DetectsValidPayloads(string value, bool expected)
    {
        Assert.Equal(expected, value.IsBase64String());
    }
}
