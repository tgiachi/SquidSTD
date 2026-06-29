using SquidStd.Persistence.Abstractions.Data;

namespace SquidStd.Persistence.Abstractions.Interfaces.Persistence;

/// <summary>Reads and writes per-type entity snapshot files.</summary>
public interface ISnapshotService
{
    ValueTask DeleteBucketAsync(string typeName, ushort typeId, CancellationToken cancellationToken = default);

    ValueTask<PersistedBucket?> LoadBucketAsync(
        string typeName, ushort typeId, CancellationToken cancellationToken = default
    );

    ValueTask SaveBucketAsync(
        EntitySnapshotBucket bucket, long lastSequenceId, CancellationToken cancellationToken = default
    );
}
