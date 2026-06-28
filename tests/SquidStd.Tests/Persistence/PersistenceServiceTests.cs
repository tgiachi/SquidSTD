using SquidStd.Core.Json;
using SquidStd.Persistence.Abstractions.Data;
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
}
