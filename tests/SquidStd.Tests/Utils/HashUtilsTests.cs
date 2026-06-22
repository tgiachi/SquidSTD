using SquidStd.Core.Utils;

namespace SquidStd.Tests.Utils;

public class HashUtilsTests
{
    [Theory, InlineData(""), InlineData("   "), InlineData(null)]
    public void HashPassword_NullOrWhitespace_Throws(string? password)
        => Assert.Throws<ArgumentException>(() => HashUtils.HashPassword(password!));

    [Fact]
    public void HashPassword_SamePasswordTwice_ProducesDifferentHashes()
    {
        var first = HashUtils.HashPassword("s3cret");
        var second = HashUtils.HashPassword("s3cret");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsSerializedPayload()
    {
        var hash = HashUtils.HashPassword("s3cret");

        var parts = hash.Split('$');

        Assert.Equal(4, parts.Length);
        Assert.Equal("pbkdf2-sha256", parts[0]);
        Assert.Equal("100000", parts[1]);
        Assert.NotEmpty(parts[2]);
        Assert.NotEmpty(parts[3]);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var hash = HashUtils.HashPassword("s3cret");

        Assert.True(HashUtils.VerifyPassword("s3cret", hash));
    }

    [Theory, InlineData("not-a-hash"), InlineData("md5$100000$c2FsdA==$aGFzaA=="),
     InlineData("pbkdf2-sha256$abc$c2FsdA==$aGFzaA=="), InlineData("pbkdf2-sha256$0$c2FsdA==$aGFzaA=="),
     InlineData("pbkdf2-sha256$100000$not-base64$aGFzaA==")]
    public void VerifyPassword_MalformedStoredHash_ReturnsFalse(string storedHash)
        => Assert.False(HashUtils.VerifyPassword("s3cret", storedHash));

    [Theory, InlineData(""), InlineData("   "), InlineData(null)]
    public void VerifyPassword_NullOrWhitespacePassword_ReturnsFalse(string? password)
        => Assert.False(HashUtils.VerifyPassword(password!, "pbkdf2-sha256$100000$c2FsdA==$aGFzaA=="));

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = HashUtils.HashPassword("s3cret");

        Assert.False(HashUtils.VerifyPassword("wrong", hash));
    }
}
