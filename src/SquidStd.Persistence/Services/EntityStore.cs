using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Abstractions.Types;
using SquidStd.Persistence.Internal;

namespace SquidStd.Persistence.Services;

/// <summary>
/// In-memory <see cref="IEntityStore{TEntity,TKey}" /> backed by the shared state store. Reads return
/// detached clones; writes serialize end-to-end on the state store's write lock (apply + journal append),
/// so journal order matches sequence order.
/// </summary>
public sealed class EntityStore<TEntity, TKey> : IEntityStore<TEntity, TKey>
    where TKey : notnull
{
    private readonly IPersistenceEntityDescriptor<TEntity, TKey> _descriptor;
    private readonly IJournalService _journalService;
    private readonly PersistenceStateStore _stateStore;

    internal EntityStore(
        PersistenceStateStore stateStore,
        IJournalService journalService,
        IPersistenceEntityDescriptor<TEntity, TKey> descriptor
    )
    {
        _stateStore = stateStore;
        _journalService = journalService;
        _descriptor = descriptor;
    }

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateStore.SyncRoot)
        {
            return ValueTask.FromResult(Bucket().Count);
        }
    }

    public ValueTask<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateStore.SyncRoot)
        {
            IReadOnlyCollection<TEntity> clones = [.. Bucket().Values.Select(_descriptor.Clone)];

            return ValueTask.FromResult(clones);
        }
    }

    public ValueTask<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        lock (_stateStore.SyncRoot)
        {
            return ValueTask.FromResult(Bucket().TryGetValue(id, out var entity) ? _descriptor.Clone(entity) : default);
        }
    }

    public IQueryable<TEntity> Query()
    {
        lock (_stateStore.SyncRoot)
        {
            return Bucket().Values.Select(_descriptor.Clone).ToArray().AsQueryable();
        }
    }

    public async ValueTask UpsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _stateStore.WriteLock.WaitAsync(cancellationToken);

        try
        {
            JournalEntry entry;
            TEntity clone;
            TKey key;
            long next;

            lock (_stateStore.SyncRoot)
            {
                clone = _descriptor.Clone(entity);
                key = _descriptor.GetKey(clone);
                next = _stateStore.LastSequenceId + 1; // computed, not yet committed
                entry = new JournalEntry
                {
                    SequenceId = next,
                    TimestampUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    TypeId = _descriptor.TypeId,
                    Operation = JournalEntityOperationType.Upsert,
                    Payload = _descriptor.SerializeEntity(clone)
                };
            }

            // Write-ahead: the journal must be durable before the in-memory state reflects the change.
            await _journalService.AppendAsync(entry, cancellationToken);

            lock (_stateStore.SyncRoot)
            {
                Bucket()[key] = clone;
                _stateStore.LastSequenceId = next;
            }
        }
        finally
        {
            _stateStore.WriteLock.Release();
        }
    }

    public async ValueTask<bool> RemoveAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await _stateStore.WriteLock.WaitAsync(cancellationToken);

        try
        {
            JournalEntry entry;
            long next;

            lock (_stateStore.SyncRoot)
            {
                if (!Bucket().ContainsKey(id))
                {
                    return false;
                }

                next = _stateStore.LastSequenceId + 1;
                entry = new JournalEntry
                {
                    SequenceId = next,
                    TimestampUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    TypeId = _descriptor.TypeId,
                    Operation = JournalEntityOperationType.Remove,
                    Payload = _descriptor.SerializeKey(id)
                };
            }

            // Write-ahead: append before applying so a failed append leaves the entity in place.
            await _journalService.AppendAsync(entry, cancellationToken);

            lock (_stateStore.SyncRoot)
            {
                Bucket().Remove(id);
                _stateStore.LastSequenceId = next;
            }

            return true;
        }
        finally
        {
            _stateStore.WriteLock.Release();
        }
    }

    private Dictionary<TKey, TEntity> Bucket()
        => _stateStore.GetBucket<TEntity, TKey>(_descriptor.TypeId);
}
