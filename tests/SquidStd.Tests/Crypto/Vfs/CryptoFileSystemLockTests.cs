using System.Security.Cryptography;
using SquidStd.Crypto.Vfs.Data;
using SquidStd.Crypto.Vfs.Services;
using SquidStd.Vfs.Services;

namespace SquidStd.Tests.Crypto.Vfs;

public class CryptoFileSystemLockTests
{
    private static CryptoVaultOptions FastOptions()
    {
        return new CryptoVaultOptions { Argon2MemoryKib = 8192, Argon2Iterations = 1 };
    }

    [Fact]
    public async Task LockedOperations_Throw_UntilUnlocked()
    {
        var vault = new CryptoFileSystem(new InMemoryFileSystem(), FastOptions());

        Assert.False(vault.IsUnlocked);
        await Assert.ThrowsAsync<InvalidOperationException>(() => vault.WriteAllBytesAsync("a", new byte[] { 1 }).AsTask()
        );

        vault.Unlock("pw");
        Assert.True(vault.IsUnlocked);

        vault.Lock();
        Assert.False(vault.IsUnlocked);
    }

    [Fact]
    public void Unlock_WrongPassphrase_OnExistingVault_Throws()
    {
        var backend = new InMemoryFileSystem();
        var first = new CryptoFileSystem(backend, FastOptions());
        first.Unlock("correct");
        first.Lock(); // writes header + index

        var second = new CryptoFileSystem(backend, FastOptions());
        Assert.Throws<AuthenticationTagMismatchException>(() => second.Unlock("wrong"));
    }
}
