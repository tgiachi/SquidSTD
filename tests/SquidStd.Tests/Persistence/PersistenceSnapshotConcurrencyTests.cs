using SquidStd.Core.Json;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Data;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public sealed class PersistenceSnapshotConcurrencyTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "squidstd-snapshot-conc-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveSnapshotAsync_ConcurrentCalls_DoNotInterleave()
    {
        var serializer = new JsonDataSerializer();
        var registry = new PersistenceEntityRegistry();
        registry.Register(new PersistenceEntityDescriptor<Player, int>(serializer, serializer, 1, "Player", 1, p => p.Id));
        var config = new PersistenceConfig { SaveDirectory = _dir, AutosaveInterval = TimeSpan.FromHours(1) };
        var journal = new BinaryJournalService(Path.Combine(_dir, config.JournalFileName));
        var snapshot = new ConcurrencyProbeSnapshotService();
        var service = new PersistenceService(registry, journal, snapshot, config, eventBus: null);

        await service.InitializeAsync();
        await service.GetStore<Player, int>().UpsertAsync(new Player { Id = 1, Name = "Bob" });

        await Task.WhenAll(
            Enumerable.Range(0, 8).Select(async _ => await service.SaveSnapshotAsync())
        );

        await journal.DisposeAsync();
        Assert.Equal(1, snapshot.MaxObservedConcurrency); // never two snapshot operations at once
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private sealed class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ConcurrencyProbeSnapshotService : ISnapshotService
    {
        private int _active;

        public int MaxObservedConcurrency { get; private set; }

        public async ValueTask SaveBucketAsync(
            EntitySnapshotBucket bucket, long lastSequenceId, CancellationToken cancellationToken = default
        )
        {
            var now = Interlocked.Increment(ref _active);
            MaxObservedConcurrency = Math.Max(MaxObservedConcurrency, now);
            await Task.Delay(20, cancellationToken);
            Interlocked.Decrement(ref _active);
        }

        public ValueTask<PersistedBucket?> LoadBucketAsync(string typeName, ushort typeId, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<PersistedBucket?>(null);

        public ValueTask DeleteBucketAsync(string typeName, ushort typeId, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
