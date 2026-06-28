using SquidStd.Crypto.Vfs.Internal;

namespace SquidStd.Tests.Crypto.Vfs;

public class VaultIndexTests
{
    [Fact]
    public void Serialize_Parse_RoundTripsEntries()
    {
        var index = new VaultIndex();
        index.Set("docs/cv.pdf", new VaultIndexEntry("a1b2", 100, DateTimeOffset.UnixEpoch));

        var parsed = VaultIndex.Parse(index.Serialize());

        Assert.True(parsed.TryGet("docs/cv.pdf", out var entry));
        Assert.Equal("a1b2", entry!.BlobId);
    }
}
