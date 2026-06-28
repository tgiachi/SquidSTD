using System.Security.Cryptography;
using System.Text;
using SquidStd.Crypto.Vfs.Data;
using SquidStd.Crypto.Vfs.Services;
using SquidStd.Vfs.Services;

namespace SquidStd.Tests.Crypto.Vfs;

public class CryptoFileSystemTests
{
    private static CryptoVaultOptions FastOptions()
    {
        return new CryptoVaultOptions { Argon2MemoryKib = 8192, Argon2Iterations = 1 };
    }

    [Fact]
    public async Task Write_Read_List_Delete_RoundTrips()
    {
        var vault = new CryptoFileSystem(new InMemoryFileSystem(), FastOptions());
        vault.Unlock("pw");

        await vault.WriteAllBytesAsync("docs/cv.pdf", Encoding.UTF8.GetBytes("hello"));
        Assert.Equal("hello", Encoding.UTF8.GetString((await vault.ReadAllBytesAsync("docs/cv.pdf"))!));

        var paths = new List<string>();
        await foreach (var e in vault.ListAsync())
        {
            paths.Add(e.Path);
        }

        Assert.Equal(["docs/cv.pdf"], paths);

        Assert.True(await vault.DeleteAsync("docs/cv.pdf"));
        Assert.Null(await vault.ReadAllBytesAsync("docs/cv.pdf"));
    }

    [Fact]
    public async Task Persists_AcrossLockUnlock()
    {
        var backend = new InMemoryFileSystem();
        var a = new CryptoFileSystem(backend, FastOptions());
        a.Unlock("pw");
        await a.WriteAllBytesAsync("a.txt", Encoding.UTF8.GetBytes("v1"));
        a.Lock();

        var b = new CryptoFileSystem(backend, FastOptions());
        b.Unlock("pw");
        Assert.Equal("v1", Encoding.UTF8.GetString((await b.ReadAllBytesAsync("a.txt"))!));
    }

    [Fact]
    public async Task BackingStore_DoesNotLeakLogicalNames()
    {
        var backend = new InMemoryFileSystem();
        var vault = new CryptoFileSystem(backend, FastOptions());
        vault.Unlock("pw");
        await vault.WriteAllBytesAsync("docs/secret.pdf", Encoding.UTF8.GetBytes("x"));
        vault.Lock();

        await foreach (var e in backend.ListAsync())
        {
            Assert.DoesNotContain("secret", e.Path, StringComparison.Ordinal);
            Assert.DoesNotContain("docs", e.Path, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task LargePayload_StreamsThroughChunks()
    {
        var vault = new CryptoFileSystem(new InMemoryFileSystem(), FastOptions());
        vault.Unlock("pw");
        var payload = RandomNumberGenerator.GetBytes(300_000);

        await vault.WriteAllBytesAsync("big.bin", payload);
        Assert.Equal(payload, await vault.ReadAllBytesAsync("big.bin"));
    }

    [Fact]
    public async Task Update_ReplacesContent()
    {
        var vault = new CryptoFileSystem(new InMemoryFileSystem(), FastOptions());
        vault.Unlock("pw");

        await vault.WriteAllBytesAsync("a.txt", Encoding.UTF8.GetBytes("first"));
        await vault.WriteAllBytesAsync("a.txt", Encoding.UTF8.GetBytes("second"));

        Assert.Equal("second", Encoding.UTF8.GetString((await vault.ReadAllBytesAsync("a.txt"))!));
    }
}
