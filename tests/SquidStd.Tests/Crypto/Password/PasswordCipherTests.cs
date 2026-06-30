using System.Text;
using SquidStd.Crypto.Password;
using SquidStd.Crypto.Password.Data;
using SquidStd.Crypto.Password.Internal;

namespace SquidStd.Tests.Crypto.Password;

public class PasswordCipherTests
{
    // Keep tests fast: a low-cost preset is plenty to exercise the round-trip.
    private static readonly PbkdfCost Fast = new(memoryKib: 8192, iterations: 1, parallelism: 1);

    [Fact]
    public void Encrypt_Decrypt_RoundTripsBytes()
    {
        var data = Encoding.UTF8.GetBytes("top secret payload");

        var blob = PasswordCipher.Encrypt(data, "correct horse", Fast);

        Assert.Equal(data, PasswordCipher.Decrypt(blob, "correct horse"));
    }

    [Fact]
    public void EncryptString_DecryptString_RoundTrips()
    {
        var blob = PasswordCipher.EncryptString("ciao mondo", "pw", Fast);

        Assert.Equal("ciao mondo", PasswordCipher.DecryptString(blob, "pw"));
    }

    [Fact]
    public void Decrypt_WithWrongPassword_Throws()
    {
        var blob = PasswordCipher.Encrypt([1, 2, 3], "right", Fast);

        Assert.Throws<PasswordDecryptionException>(() => PasswordCipher.Decrypt(blob, "wrong"));
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_Throws()
    {
        var blob = PasswordCipher.Encrypt([1, 2, 3, 4], "pw", Fast);
        blob[^1] ^= 0xFF;

        Assert.Throws<PasswordDecryptionException>(() => PasswordCipher.Decrypt(blob, "pw"));
    }

    [Fact]
    public void Decrypt_TamperedHeader_Throws()
    {
        var blob = PasswordCipher.Encrypt([1, 2, 3, 4], "pw", Fast);
        blob[14] ^= 0xFF; // flip a salt byte (part of the AAD)

        Assert.Throws<PasswordDecryptionException>(() => PasswordCipher.Decrypt(blob, "pw"));
    }

    [Fact]
    public void Encrypt_ProducesDifferentBlobsEachTime()
    {
        var a = PasswordCipher.Encrypt([9, 9, 9], "pw", Fast);
        var b = PasswordCipher.Encrypt([9, 9, 9], "pw", Fast);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void DefaultCost_RoundTrips()
    {
        var blob = PasswordCipher.Encrypt([7], "pw"); // PbkdfCost.Moderate

        Assert.Equal(new byte[] { 7 }, PasswordCipher.Decrypt(blob, "pw"));
    }
}
