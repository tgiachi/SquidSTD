using System.Text;
using SquidStd.Vfs.Services;

namespace SquidStd.Tests.Vfs;

public class PhysicalFileSystemTests
{
    [Fact]
    public async Task Write_Read_List_Delete_RoundTrips()
    {
        var root = Path.Combine(Path.GetTempPath(), "squidstd-vfs-" + Guid.NewGuid().ToString("N"));

        try
        {
            var fs = new PhysicalFileSystem(root);
            await fs.WriteAllBytesAsync("docs/cv.pdf", Encoding.UTF8.GetBytes("hello"));

            Assert.True(await fs.ExistsAsync("docs/cv.pdf"));
            Assert.Equal("hello", Encoding.UTF8.GetString((await fs.ReadAllBytesAsync("docs/cv.pdf"))!));

            var entries = new List<string>();
            await foreach (var e in fs.ListAsync())
            {
                entries.Add(e.Path);
            }

            Assert.Contains("docs/cv.pdf", entries);

            Assert.True(await fs.DeleteAsync("docs/cv.pdf"));
            Assert.False(await fs.ExistsAsync("docs/cv.pdf"));
            Assert.Null(await fs.ReadAllBytesAsync("docs/cv.pdf"));
            Assert.False(await fs.DeleteAsync("docs/cv.pdf"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
