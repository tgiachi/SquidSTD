using Serilog;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Data.Events;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Abstractions.Types;
using SquidStd.Persistence.Internal;
using ILogger = Serilog.ILogger;

namespace SquidStd.Persistence.Services;

/// <summary>
/// Owns persistence lifecycle: loads snapshots and replays the journal tail at startup, autosaves on a
/// timer, and hands out per-type <see cref="IEntityStore{TEntity,TKey}" /> instances.
/// </summary>
public sealed class PersistenceService : IPersistenceService, IAsyncDisposable
{
    private readonly PersistenceConfig _config;
    private readonly IEventBus? _eventBus;
    private readonly IJournalService _journalService;
    private readonly ILogger _logger = Log.ForContext<PersistenceService>();
    private readonly IPersistenceEntityRegistry _registry;
    private readonly ISnapshotService _snapshotService;
    private readonly PersistenceStateStore _stateStore = new();
    private readonly SemaphoreSlim _snapshotLock = new(1, 1);
    private CancellationTokenSource? _autosaveCts;
    private Task? _autosaveLoop;

    public PersistenceService(
        IPersistenceEntityRegistry registry,
        IJournalService journalService,
        ISnapshotService snapshotService,
        PersistenceConfig config,
        IEventBus? eventBus = null
    )
    {
        _registry = registry;
        _journalService = journalService;
        _snapshotService = snapshotService;
        _config = config;
        _eventBus = eventBus;
    }

    public IEntityStore<TEntity, TKey> GetStore<TEntity, TKey>()
        where TKey : notnull
        => new EntityStore<TEntity, TKey>(_stateStore, _journalService, _registry.GetDescriptor<TEntity, TKey>());

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        _registry.Freeze();

        var maxSequenceId = 0L;
        var snapshotThresholds = new Dictionary<ushort, long>();

        foreach (var descriptor in _registry.GetRegisteredDescriptors())
        {
            var loaded = await _snapshotService.LoadBucketAsync(descriptor.TypeName, descriptor.TypeId, cancellationToken);

            if (loaded is null)
            {
                continue;
            }

            ((IInternalEntityApplier)descriptor).LoadBucket(_stateStore, loaded.Bucket);
            snapshotThresholds[descriptor.TypeId] = loaded.LastSequenceId;
            maxSequenceId = Math.Max(maxSequenceId, loaded.LastSequenceId);
        }

        foreach (var entry in await _journalService.ReadAllAsync(cancellationToken))
        {
            // Replay each entry against its own type's snapshot watermark, not a global maximum: a
            // single global threshold would skip journal entries newer than a lagging type's snapshot
            // (e.g. after a partial snapshot save where one bucket persisted and another did not),
            // silently dropping that type's recent writes.
            if (entry.SequenceId <= snapshotThresholds.GetValueOrDefault(entry.TypeId))
            {
                continue;
            }

            if (!_registry.IsRegistered(entry.TypeId))
            {
                _logger.Warning(
                    "Journal replay: unregistered type id {TypeId}; skipping entry {SequenceId}",
                    entry.TypeId,
                    entry.SequenceId
                );

                continue;
            }

            var applier = (IInternalEntityApplier)_registry.GetDescriptor(entry.TypeId);

            switch (entry.Operation)
            {
                case JournalEntityOperationType.Upsert:
                    applier.ApplyUpsert(_stateStore, entry.Payload);

                    break;
                case JournalEntityOperationType.Remove:
                    applier.ApplyRemove(_stateStore, entry.Payload);

                    break;
            }

            maxSequenceId = Math.Max(maxSequenceId, entry.SequenceId);
        }

        _stateStore.LastSequenceId = maxSequenceId;
    }

    public async ValueTask SaveSnapshotAsync(CancellationToken cancellationToken = default)
    {
        // One snapshot at a time: autosave, an explicit call, and StopAsync must not interleave their
        // capture/save/trim phases, or a lower-sequence snapshot could land after a higher-sequence trim.
        await _snapshotLock.WaitAsync(cancellationToken);

        try
        {
            long capturedSequenceId;
            List<EntitySnapshotBucket> buckets = [];
            List<(string TypeName, ushort TypeId)> emptyTypes = [];

            await _stateStore.WriteLock.WaitAsync(cancellationToken);

            try
            {
                capturedSequenceId = _stateStore.LastSequenceId;

                foreach (var descriptor in _registry.GetRegisteredDescriptors())
                {
                    lock (_stateStore.SyncRoot)
                    {
                        var bucket = ((IInternalEntityApplier)descriptor).CaptureBucket(_stateStore);

                        if (bucket is null)
                        {
                            emptyTypes.Add((descriptor.TypeName, descriptor.TypeId));
                        }
                        else
                        {
                            buckets.Add(bucket);
                        }
                    }
                }
            }
            finally
            {
                _stateStore.WriteLock.Release();
            }

            if (_eventBus is not null)
            {
                await _eventBus.PublishAsync(new SnapshotSaveStartedEvent(capturedSequenceId), cancellationToken);
            }

            foreach (var bucket in buckets)
            {
                await _snapshotService.SaveBucketAsync(bucket, capturedSequenceId, cancellationToken);
            }

            foreach (var (typeName, typeId) in emptyTypes)
            {
                await _snapshotService.DeleteBucketAsync(typeName, typeId, cancellationToken);
            }

            await _journalService.TrimThroughSequenceAsync(capturedSequenceId, cancellationToken);

            if (_eventBus is not null)
            {
                await _eventBus.PublishAsync(
                    new SnapshotSaveCompletedEvent(capturedSequenceId, buckets.Count),
                    cancellationToken
                );
            }
        }
        finally
        {
            _snapshotLock.Release();
        }
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        _autosaveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _autosaveLoop = Task.Run(() => AutosaveLoopAsync(_autosaveCts.Token), CancellationToken.None);
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_autosaveCts is not null)
        {
            await _autosaveCts.CancelAsync();
        }

        if (_autosaveLoop is not null)
        {
            try
            {
                await _autosaveLoop;
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }

        await SaveSnapshotAsync(cancellationToken);

        _autosaveCts?.Dispose();
        _autosaveCts = null;
        _autosaveLoop = null;
    }

    private async Task AutosaveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.AutosaveInterval, cancellationToken);
                await SaveSnapshotAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Autosave failed; journal retained for the next attempt");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }
}
