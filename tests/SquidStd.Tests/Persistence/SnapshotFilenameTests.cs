using SquidStd.Core.Utils;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public sealed class SnapshotFilenameTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "squidstd-snap-name-" + Guid.NewGuid().ToString("N"));

    private static EntitySnapshotBucket Bucket(ushort typeId, byte payload)
        => new() { TypeId = typeId, TypeName = "Player", SchemaVersion = 1, Payload = [payload] };

    [Fact]
    public async Task SameTypeName_DifferentTypeId_ProduceDistinctFiles()
    {
        var service = new SnapshotService(_dir, ".snapshot.bin");

        await service.SaveBucketAsync(Bucket(1, 0xAA), lastSequenceId: 1);
        await service.SaveBucketAsync(Bucket(2, 0xBB), lastSequenceId: 2);

        Assert.Equal(2, Directory.GetFiles(_dir, "*.snapshot.bin").Length);

        var first = await service.LoadBucketAsync("Player", 1);
        var second = await service.LoadBucketAsync("Player", 2);

        Assert.Equal(new byte[] { 0xAA }, first!.Bucket.Payload);
        Assert.Equal(new byte[] { 0xBB }, second!.Bucket.Payload);
    }

    [Fact]
    public async Task LoadBucketAsync_MigratesLegacyNamedFile()
    {
        var service = new SnapshotService(_dir, ".snapshot.bin");

        // Write a file at the OLD path (no TypeId) by saving then renaming to the legacy name.
        await service.SaveBucketAsync(Bucket(1, 0x7), lastSequenceId: 9);
        var newPath = Directory.GetFiles(_dir, "*.snapshot.bin").Single();
        var legacyPath = Path.Combine(_dir, StringUtils.ToSnakeCase("Player") + ".snapshot.bin");
        File.Move(newPath, legacyPath);

        var loaded = await service.LoadBucketAsync("Player", 1);

        Assert.NotNull(loaded);
        Assert.Equal(9, loaded.LastSequenceId);
        Assert.False(File.Exists(legacyPath));                 // legacy file was migrated away
        Assert.True(File.Exists(Path.Combine(_dir, "player_1.snapshot.bin"))); // to the new TypeId path
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
