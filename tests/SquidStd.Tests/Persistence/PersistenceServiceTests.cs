using SquidStd.Core.Json;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Data;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public sealed class PersistenceServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "squidstd-persistence-" + Guid.NewGuid().ToString("N"));

    private (PersistenceService Service, PersistenceEntityRegistry Registry) Create()
    {
        var serializer = new JsonDataSerializer();
        var registry = new PersistenceEntityRegistry();
        registry.Register(new PersistenceEntityDescriptor<Player, int>(serializer, serializer, 1, "Player", 1, p => p.Id));
        var config = new PersistenceConfig { SaveDirectory = _dir, AutosaveInterval = TimeSpan.FromHours(1) };
        var journal = new BinaryJournalService(Path.Combine(_dir, config.JournalFileName));
        var snapshot = new SnapshotService(_dir, config.SnapshotFileSuffix);
        var service = new PersistenceService(registry, journal, snapshot, config, eventBus: null);

        return (service, registry);
    }

    [Fact]
    public async Task SnapshotThenReload_RestoresState()
    {
        var (service, _) = Create();
        await service.InitializeAsync();
        var store = service.GetStore<Player, int>();
        await store.UpsertAsync(new Player { Id = 1, Name = "Bob" });
        await service.SaveSnapshotAsync();

        var (reloaded, _) = Create();
        await reloaded.InitializeAsync();
        var fetched = await reloaded.GetStore<Player, int>().GetByIdAsync(1);

        Assert.NotNull(fetched);
        Assert.Equal("Bob", fetched.Name);
    }

    [Fact]
    public async Task NoSnapshot_ReplaysJournal()
    {
        var (service, _) = Create();
        await service.InitializeAsync();
        await service.GetStore<Player, int>().UpsertAsync(new Player { Id = 7, Name = "Eve" });

        var (reloaded, _) = Create();
        await reloaded.InitializeAsync(); // only journal exists, no snapshot
        var fetched = await reloaded.GetStore<Player, int>().GetByIdAsync(7);

        Assert.Equal("Eve", fetched!.Name);
    }

    [Fact]
    public async Task SnapshotPlusJournalTail_ReplaysTail()
    {
        var (service, _) = Create();
        await service.InitializeAsync();
        var store = service.GetStore<Player, int>();
        await store.UpsertAsync(new Player { Id = 1, Name = "First" });
        await service.SaveSnapshotAsync();
        await store.UpsertAsync(new Player { Id = 2, Name = "Second" });

        var (reloaded, _) = Create();
        await reloaded.InitializeAsync();
        var all = await reloaded.GetStore<Player, int>().GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task SaveSnapshot_TrimsJournal()
    {
        var (service, _) = Create();
        await service.InitializeAsync();
        await service.GetStore<Player, int>().UpsertAsync(new Player { Id = 1 });
        await service.SaveSnapshotAsync();

        var journal = new BinaryJournalService(Path.Combine(_dir, "world.journal.bin"));
        Assert.Empty(await journal.ReadAllAsync());
        await journal.DisposeAsync();
    }

    [Fact]
    public async Task PartialSnapshotSave_ReplaysLaggingTypeFromJournal()
    {
        var serializer = new JsonDataSerializer();
        var config = new PersistenceConfig { SaveDirectory = _dir, AutosaveInterval = TimeSpan.FromHours(1) };

        // First run: write one entity of each type, then attempt a snapshot whose second bucket save
        // fails. One type's snapshot persists at the global watermark; the other's stays only in the
        // (untrimmed) journal — the exact state a partial snapshot save leaves behind.
        var journal = new BinaryJournalService(Path.Combine(_dir, config.JournalFileName));
        var faulty = new FailOnSecondSaveSnapshotService(new SnapshotService(_dir, config.SnapshotFileSuffix));
        var service = new PersistenceService(BuildRegistry(serializer), journal, faulty, config, eventBus: null);

        await service.InitializeAsync();
        await service.GetStore<Player, int>().UpsertAsync(new Player { Id = 1, Name = "First" });
        await service.GetStore<Item, int>().UpsertAsync(new Item { Id = 1, Name = "Sword" });

        await Assert.ThrowsAnyAsync<Exception>(async () => await service.SaveSnapshotAsync());
        await journal.DisposeAsync();

        // Reload with a healthy snapshot service: both entities must survive. A single global replay
        // watermark would skip the journal entry for whichever type's snapshot did persist, losing it.
        var reloadJournal = new BinaryJournalService(Path.Combine(_dir, config.JournalFileName));
        var reloaded = new PersistenceService(
            BuildRegistry(serializer), reloadJournal, new SnapshotService(_dir, config.SnapshotFileSuffix), config, eventBus: null
        );

        await reloaded.InitializeAsync();
        var player = await reloaded.GetStore<Player, int>().GetByIdAsync(1);
        var item = await reloaded.GetStore<Item, int>().GetByIdAsync(1);
        await reloadJournal.DisposeAsync();

        Assert.NotNull(player);
        Assert.NotNull(item);
        Assert.Equal("Sword", item.Name);
    }

    private static PersistenceEntityRegistry BuildRegistry(JsonDataSerializer serializer)
    {
        var registry = new PersistenceEntityRegistry();
        registry.Register(new PersistenceEntityDescriptor<Player, int>(serializer, serializer, 1, "Player", 1, p => p.Id));
        registry.Register(new PersistenceEntityDescriptor<Item, int>(serializer, serializer, 2, "Item", 1, i => i.Id));

        return registry;
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

    private sealed class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Delegates to a real snapshot service but throws on the second bucket save, simulating a
    /// snapshot run that persists one type's bucket and then fails before the next.</summary>
    private sealed class FailOnSecondSaveSnapshotService : ISnapshotService
    {
        private readonly ISnapshotService _inner;
        private int _saveCount;

        public FailOnSecondSaveSnapshotService(ISnapshotService inner)
        {
            _inner = inner;
        }

        public ValueTask SaveBucketAsync(
            EntitySnapshotBucket bucket, long lastSequenceId, CancellationToken cancellationToken = default
        )
        {
            if (++_saveCount >= 2)
            {
                throw new IOException("Simulated snapshot write failure.");
            }

            return _inner.SaveBucketAsync(bucket, lastSequenceId, cancellationToken);
        }

        public ValueTask<PersistedBucket?> LoadBucketAsync(string typeName, CancellationToken cancellationToken = default)
        {
            return _inner.LoadBucketAsync(typeName, cancellationToken);
        }

        public ValueTask DeleteBucketAsync(string typeName, CancellationToken cancellationToken = default)
        {
            return _inner.DeleteBucketAsync(typeName, cancellationToken);
        }
    }
}
