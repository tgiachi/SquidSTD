using System.Text;
using SquidStd.Vfs.Services;

namespace SquidStd.Tests.Vfs;

public class InMemoryFileSystemTests
{
    [Fact]
    public async Task Write_Read_List_Delete_RoundTrips()
    {
        var fs = new InMemoryFileSystem();
        await fs.WriteAllBytesAsync("a/b.txt", Encoding.UTF8.GetBytes("x"));

        Assert.True(await fs.ExistsAsync("a/b.txt"));
        Assert.Equal("x", Encoding.UTF8.GetString((await fs.ReadAllBytesAsync("a/b.txt"))!));

        var paths = new List<string>();
        await foreach (var e in fs.ListAsync("a"))
        {
            paths.Add(e.Path);
        }

        Assert.Equal(["a/b.txt"], paths);

        Assert.True(await fs.DeleteAsync("a/b.txt"));
        Assert.Null(await fs.ReadAllBytesAsync("a/b.txt"));
    }
}
