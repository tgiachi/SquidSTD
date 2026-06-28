using SquidStd.Crypto.Vfs.Internal;

namespace SquidStd.Tests.Crypto.Vfs;

public class VaultHeaderTests
{
    [Fact]
    public void Serialize_Parse_RoundTrips()
    {
        var header = new VaultHeader(
            "SQVFS1",
            1,
            [1, 2, 3, 4],
            MemoryKib: 8192,
            Iterations: 2,
            Parallelism: 1,
            ChunkSize: 65536
        );

        var parsed = VaultHeader.Parse(header.Serialize());

        Assert.Equal(header.Magic, parsed.Magic);
        Assert.Equal(header.Version, parsed.Version);
        Assert.Equal(header.Salt, parsed.Salt);
        Assert.Equal(header.MemoryKib, parsed.MemoryKib);
        Assert.Equal(header.Iterations, parsed.Iterations);
        Assert.Equal(header.Parallelism, parsed.Parallelism);
        Assert.Equal(header.ChunkSize, parsed.ChunkSize);
    }
}
