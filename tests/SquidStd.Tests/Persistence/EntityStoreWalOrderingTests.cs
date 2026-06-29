using SquidStd.Core.Json;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Data;
using SquidStd.Persistence.Internal;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public sealed class EntityStoreWalOrderingTests
{
    [Fact]
    public async Task Upsert_WhenJournalAppendThrows_LeavesInMemoryStateUnchanged()
    {
        var serializer = new JsonDataSerializer();
        IPersistenceEntityDescriptor<Player, int> descriptor =
            new PersistenceEntityDescriptor<Player, int>(serializer, serializer, 1, "Player", 1, p => p.Id);
        var stateStore = new PersistenceStateStore();
        var store = new EntityStore<Player, int>(stateStore, new ThrowingJournalService(), descriptor);

        await Assert.ThrowsAsync<IOException>(async () => await store.UpsertAsync(new Player { Id = 1, Name = "Bob" }));

        // The failed durable append must NOT have applied the mutation in memory.
        Assert.Null(await store.GetByIdAsync(1));
        Assert.Equal(0, await store.CountAsync());
    }

    [Fact]
    public async Task Remove_WhenJournalAppendThrows_KeepsEntity()
    {
        var serializer = new JsonDataSerializer();
        IPersistenceEntityDescriptor<Player, int> descriptor =
            new PersistenceEntityDescriptor<Player, int>(serializer, serializer, 1, "Player", 1, p => p.Id);
        var stateStore = new PersistenceStateStore();
        var ok = new CountingJournalService();
        var store = new EntityStore<Player, int>(stateStore, ok, descriptor);
        await store.UpsertAsync(new Player { Id = 1, Name = "Bob" });

        ok.FailNextAppend = true;
        await Assert.ThrowsAsync<IOException>(async () => await store.RemoveAsync(1));

        Assert.NotNull(await store.GetByIdAsync(1)); // remove not applied because the append failed
    }

    private sealed class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ThrowingJournalService : IJournalService
    {
        public ValueTask AppendAsync(JournalEntry entry, CancellationToken cancellationToken = default)
            => throw new IOException("simulated append failure");

        public ValueTask AppendBatchAsync(IReadOnlyList<JournalEntry> entries, CancellationToken cancellationToken = default)
            => throw new IOException("simulated append failure");

        public ValueTask<IReadOnlyCollection<JournalEntry>> ReadAllAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyCollection<JournalEntry>>([]);

        public ValueTask ResetAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask TrimThroughSequenceAsync(long inclusiveSequenceId, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class CountingJournalService : IJournalService
    {
        public bool FailNextAppend { get; set; }

        public ValueTask AppendAsync(JournalEntry entry, CancellationToken cancellationToken = default)
        {
            if (FailNextAppend)
            {
                throw new IOException("simulated append failure");
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask AppendBatchAsync(IReadOnlyList<JournalEntry> entries, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask<IReadOnlyCollection<JournalEntry>> ReadAllAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyCollection<JournalEntry>>([]);

        public ValueTask ResetAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask TrimThroughSequenceAsync(long inclusiveSequenceId, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
