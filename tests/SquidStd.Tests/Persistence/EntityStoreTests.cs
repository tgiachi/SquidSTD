using SquidStd.Core.Json;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Data;
using SquidStd.Persistence.Internal;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public sealed class EntityStoreTests : IAsyncDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "squidstd-store-" + Guid.NewGuid().ToString("N"));
    private readonly BinaryJournalService _journal;
    private readonly PersistenceStateStore _stateStore = new();
    private readonly EntityStore<Player, int> _store;

    public EntityStoreTests()
    {
        var serializer = new JsonDataSerializer();
        IPersistenceEntityDescriptor<Player, int> descriptor =
            new PersistenceEntityDescriptor<Player, int>(serializer, serializer, 1, "Player", 1, p => p.Id);
        _journal = new BinaryJournalService(Path.Combine(_dir, "world.journal.bin"));
        _store = new EntityStore<Player, int>(_stateStore, _journal, descriptor);
    }

    [Fact]
    public async Task Upsert_ThenGetById_ReturnsClone()
    {
        await _store.UpsertAsync(new Player { Id = 1, Name = "Bob" });

        var fetched = await _store.GetByIdAsync(1);

        Assert.NotNull(fetched);
        Assert.Equal("Bob", fetched.Name);
    }

    [Fact]
    public async Task GetById_ReturnsDetachedClone()
    {
        await _store.UpsertAsync(new Player { Id = 1, Tags = ["a"] });

        var first = await _store.GetByIdAsync(1);
        first!.Tags.Add("mutated");
        var second = await _store.GetByIdAsync(1);

        Assert.Equal(["a"], second!.Tags);
    }

    [Fact]
    public async Task Upsert_AppendsToJournal()
    {
        await _store.UpsertAsync(new Player { Id = 1 });

        Assert.Single(await _journal.ReadAllAsync());
    }

    [Fact]
    public async Task Remove_ExistingKey_ReturnsTrueAndJournals()
    {
        await _store.UpsertAsync(new Player { Id = 1 });

        Assert.True(await _store.RemoveAsync(1));
        Assert.Null(await _store.GetByIdAsync(1));
        Assert.Equal(2, (await _journal.ReadAllAsync()).Count);
    }

    [Fact]
    public async Task Remove_MissingKey_ReturnsFalseAndDoesNotJournal()
    {
        Assert.False(await _store.RemoveAsync(99));
        Assert.Empty(await _journal.ReadAllAsync());
    }

    [Fact]
    public async Task CountAndGetAll_ReflectState()
    {
        await _store.UpsertAsync(new Player { Id = 1 });
        await _store.UpsertAsync(new Player { Id = 2 });

        Assert.Equal(2, await _store.CountAsync());
        Assert.Equal(2, (await _store.GetAllAsync()).Count);
    }

    [Fact]
    public async Task Query_ReturnsQueryableClones()
    {
        await _store.UpsertAsync(new Player { Id = 1, Name = "Alice" });
        await _store.UpsertAsync(new Player { Id = 2, Name = "Bob" });

        var names = _store.Query().Where(p => p.Name.StartsWith('A')).Select(p => p.Name).ToArray();

        Assert.Equal(["Alice"], names);
    }

    public async ValueTask DisposeAsync()
    {
        await _journal.DisposeAsync();

        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private sealed class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = [];
    }
}
