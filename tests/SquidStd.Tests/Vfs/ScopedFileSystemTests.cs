using SquidStd.Vfs.Services;

namespace SquidStd.Tests.Vfs;

public class ScopedFileSystemTests
{
    [Fact]
    public async Task Scopes_ReadWrite_UnderPrefix()
    {
        var inner = new InMemoryFileSystem();
        var fs = new ScopedFileSystem(inner, "tenant1");

        await fs.WriteAllBytesAsync("docs/a.txt", new byte[] { 1 });

        Assert.True(await inner.ExistsAsync("tenant1/docs/a.txt")); // stored under the prefix
        Assert.Equal(new byte[] { 1 }, await fs.ReadAllBytesAsync("docs/a.txt"));
    }

    [Fact]
    public async Task List_StripsPrefix_FromReturnedPaths()
    {
        var inner = new InMemoryFileSystem();
        var fs = new ScopedFileSystem(inner, "tenant1");
        await fs.WriteAllBytesAsync("docs/a.txt", new byte[] { 1 });
        await inner.WriteAllBytesAsync("tenant2/secret.txt", new byte[] { 9 }); // outside the scope

        var paths = new List<string>();
        await foreach (var e in fs.ListAsync())
        {
            paths.Add(e.Path);
        }

        Assert.Contains("docs/a.txt", paths);
        Assert.DoesNotContain(paths, p => p.Contains("tenant2"));
    }

    [Fact]
    public async Task List_DoesNotLeak_SiblingScopeSharingNamePrefix()
    {
        var inner = new InMemoryFileSystem();
        var fs = new ScopedFileSystem(inner, "tenant1");
        await fs.WriteAllBytesAsync("a.txt", new byte[] { 1 });           // -> tenant1/a.txt
        await inner.WriteAllBytesAsync("tenant10/b.txt", new byte[] { 9 }); // sibling scope, shares the "tenant1" prefix

        var paths = new List<string>();
        await foreach (var e in fs.ListAsync())
        {
            paths.Add(e.Path);
        }

        Assert.Equal(new[] { "a.txt" }, paths); // only our scope, and stripped
    }
}
