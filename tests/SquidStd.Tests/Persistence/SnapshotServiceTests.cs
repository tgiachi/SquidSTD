using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public sealed class SnapshotServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "squidstd-snapshot-" + Guid.NewGuid().ToString("N"));

    private SnapshotService Create()
        => new(_dir, ".snapshot.bin");

    private static EntitySnapshotBucket Bucket(string typeName = "Player")
        => new() { TypeId = 1, TypeName = typeName, SchemaVersion = 1, Payload = [1, 2, 3] };

    [Fact]
    public async Task SaveThenLoad_RoundTrips()
    {
        var service = Create();
        await service.SaveBucketAsync(Bucket(), 42);

        var loaded = await service.LoadBucketAsync("Player", 1);

        Assert.NotNull(loaded);
        Assert.Equal(42, loaded.LastSequenceId);
        Assert.Equal([1, 2, 3], loaded.Bucket.Payload);
        Assert.Equal("Player", loaded.Bucket.TypeName);
    }

    [Fact]
    public async Task LoadBucket_MissingFile_ReturnsNull()
        => Assert.Null(await Create().LoadBucketAsync("Absent", 1));

    [Fact]
    public async Task LoadBucket_CorruptPayload_ReturnsNull()
    {
        var service = Create();
        await service.SaveBucketAsync(Bucket(), 1);
        var path = Directory.GetFiles(_dir, "*.snapshot.bin").Single();
        var bytes = await File.ReadAllBytesAsync(path);
        bytes[^1] ^= 0xFF; // corrupt the payload so checksum mismatches
        await File.WriteAllBytesAsync(path, bytes);

        Assert.Null(await service.LoadBucketAsync("Player", 1));
    }

    [Fact]
    public async Task DeleteBucket_RemovesFile()
    {
        var service = Create();
        await service.SaveBucketAsync(Bucket(), 1);

        await service.DeleteBucketAsync("Player", 1);

        Assert.Null(await service.LoadBucketAsync("Player", 1));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
