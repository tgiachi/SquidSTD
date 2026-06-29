using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Types;
using SquidStd.Persistence.Abstractions.Types.Persistence;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public sealed class DurableWriteTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "squidstd-durable-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task DurableJournal_AppendThenReadAll_RoundTrips()
    {
        var path = Path.Combine(_dir, "world.journal.bin");
        await using var journal = new BinaryJournalService(path, DurabilityMode.Durable);

        await journal.AppendAsync(new JournalEntry
        {
            SequenceId = 1,
            TimestampUnixMilliseconds = 1000,
            TypeId = 1,
            Operation = JournalEntityOperationType.Upsert,
            Payload = [1, 2, 3]
        });

        var entries = (await journal.ReadAllAsync()).ToArray();
        Assert.Single(entries);
        Assert.Equal(1, entries[0].SequenceId);
    }

    [Fact]
    public async Task DurableSnapshot_SaveThenLoad_RoundTrips()
    {
        var service = new SnapshotService(_dir, ".snapshot.bin", DurabilityMode.Durable);
        var bucket = new EntitySnapshotBucket { TypeId = 1, TypeName = "Player", SchemaVersion = 1, Payload = [9, 9] };

        await service.SaveBucketAsync(bucket, lastSequenceId: 5);
        var loaded = await service.LoadBucketAsync("Player", 1);

        Assert.NotNull(loaded);
        Assert.Equal(5, loaded.LastSequenceId);
        Assert.Equal(new byte[] { 9, 9 }, loaded.Bucket.Payload);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
