using SquidStd.Core.Utils;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Internal;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public sealed class SnapshotChecksumScopeTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "squidstd-snap-csum-" + Guid.NewGuid().ToString("N"));

    private static EntitySnapshotBucket Bucket()
        => new() { TypeId = 1, TypeName = "Player", SchemaVersion = 1, Payload = [1, 2, 3, 4] };

    [Fact]
    public async Task LoadBucketAsync_WhenLastSequenceIdCorrupted_RejectsSnapshot()
    {
        var service = new SnapshotService(_dir, ".snapshot.bin");
        await service.SaveBucketAsync(Bucket(), lastSequenceId: 42);

        var file = Directory.GetFiles(_dir, "*.snapshot.bin").Single();
        var bytes = await File.ReadAllBytesAsync(file);
        bytes[4] ^= 0xFF; // flip a byte inside the LastSequenceId field (offset 4..11)
        await File.WriteAllBytesAsync(file, bytes);

        Assert.Null(await service.LoadBucketAsync("Player", 1));
    }

    [Fact]
    public async Task LoadBucketAsync_LegacyVersion1File_StillLoads()
    {
        var service = new SnapshotService(_dir, ".snapshot.bin");
        await service.SaveBucketAsync(Bucket(), lastSequenceId: 7); // creates the file at the correct path

        // Overwrite it with a legacy Version-1 envelope (payload-only checksum), as written before this change.
        var file = Directory.GetFiles(_dir, "*.snapshot.bin").Single();
        var legacy = SnapshotEnvelopeCodec.Encode(new SnapshotFileEnvelope
        {
            Version = 1,
            LastSequenceId = 7,
            Checksum = ChecksumUtils.Compute(Bucket().Payload),
            Bucket = Bucket()
        });
        await File.WriteAllBytesAsync(file, legacy);

        var loaded = await service.LoadBucketAsync("Player", 1);
        Assert.NotNull(loaded);
        Assert.Equal(7, loaded.LastSequenceId);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
