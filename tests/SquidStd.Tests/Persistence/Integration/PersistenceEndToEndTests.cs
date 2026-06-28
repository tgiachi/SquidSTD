using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Data;
using SquidStd.Persistence.MessagePack;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence.Integration;

public sealed class PersistenceEndToEndTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "squidstd-e2e-" + Guid.NewGuid().ToString("N"));

    private PersistenceService Create()
    {
        var serializer = new MessagePackDataSerializer();
        var registry = new PersistenceEntityRegistry();
        registry.Register(new PersistenceEntityDescriptor<Item, int>(serializer, serializer, 1, "Item", 1, i => i.Id));
        var config = new PersistenceConfig { SaveDirectory = _dir, AutosaveInterval = TimeSpan.FromHours(1) };
        var journal = new BinaryJournalService(Path.Combine(_dir, config.JournalFileName));
        var snapshot = new SnapshotService(_dir, config.SnapshotFileSuffix);

        return new PersistenceService(registry, journal, snapshot, config, eventBus: null);
    }

    [Fact]
    public async Task FullCycle_SnapshotRemoveTailCrashRestart_RestoresExactState()
    {
        // First lifetime: snapshot, then mutate. No clean shutdown (no final snapshot) to simulate a crash,
        // so recovery must come from the seq-2 snapshot plus the replayed journal tail (seq 3 and 4).
        var service = Create();
        await service.InitializeAsync();
        var store = service.GetStore<Item, int>();
        await store.UpsertAsync(new Item { Id = 1, Label = "Sword", Quantity = 1 });
        await store.UpsertAsync(new Item { Id = 2, Label = "Potion", Quantity = 5 });
        await service.SaveSnapshotAsync();                                            // snapshot at seq 2
        await store.UpsertAsync(new Item { Id = 2, Label = "Potion", Quantity = 9 }); // tail update (seq 3)
        await store.RemoveAsync(1);                                                   // tail remove (seq 4)

        var reloaded = Create();
        await reloaded.InitializeAsync();
        var store2 = reloaded.GetStore<Item, int>();
        var all = await store2.GetAllAsync();
        var potion = await store2.GetByIdAsync(2);

        Assert.Single(all);
        Assert.Null(await store2.GetByIdAsync(1));
        Assert.Equal(9, potion!.Quantity);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    public sealed class Item
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
