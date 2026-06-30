using SquidStd.Crypto.Password.Internal;

namespace SquidStd.Tests.Crypto.Password;

public class PasswordKeyDerivationTests
{
    private static readonly byte[] Salt = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

    [Fact]
    public void DeriveKey_IsDeterministic_AndProduces32Bytes()
    {
        var a = PasswordKeyDerivation.DeriveKey("pw", Salt, iterations: 2, memoryKib: 8192, parallelism: 1);
        var b = PasswordKeyDerivation.DeriveKey("pw", Salt, iterations: 2, memoryKib: 8192, parallelism: 1);

        Assert.Equal(32, a.Length);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DeriveKey_DiffersByPasswordAndSalt()
    {
        var baseline = PasswordKeyDerivation.DeriveKey("pw", Salt, 2, 8192, 1);
        var otherPw = PasswordKeyDerivation.DeriveKey("pw2", Salt, 2, 8192, 1);
        var otherSalt = PasswordKeyDerivation.DeriveKey("pw", new byte[16], 2, 8192, 1);

        Assert.NotEqual(baseline, otherPw);
        Assert.NotEqual(baseline, otherSalt);
    }
}
