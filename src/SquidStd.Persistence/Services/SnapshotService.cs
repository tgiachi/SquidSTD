using System.Buffers.Binary;
using Serilog;
using SquidStd.Core.Utils;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Abstractions.Types.Persistence;
using SquidStd.Persistence.Internal;
using ILogger = Serilog.ILogger;

namespace SquidStd.Persistence.Services;

/// <summary>
/// Stores each registered entity type as its own fixed-binary snapshot file
/// (<c>&lt;snake_case type name&gt;&lt;suffix&gt;</c> under the save directory), written atomically
/// via temp + rename and verified by a payload checksum on load.
/// </summary>
public sealed class SnapshotService : ISnapshotService, IDisposable
{
    private static readonly char[] _invalidTypeNameChars =
        [.. Path.GetInvalidFileNameChars(), Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    private readonly string _directory;
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private readonly ILogger _logger = Log.ForContext<SnapshotService>();
    private readonly string _suffix;
    private readonly DurabilityMode _durability;

    public SnapshotService(string saveDirectory, string fileSuffix, DurabilityMode durability = DurabilityMode.Buffered)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileSuffix);

        _directory = Path.GetFullPath(saveDirectory);
        _suffix = fileSuffix;
        _durability = durability;

        Directory.CreateDirectory(_directory);
    }

    public async ValueTask DeleteBucketAsync(string typeName, ushort typeId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        var path = PathFor(typeName, typeId);
        var legacyPath = LegacyPathFor(typeName);

        await _ioLock.WaitAsync(cancellationToken);

        try
        {
            File.Delete(path);
            File.Delete(path + ".tmp");
            File.Delete(legacyPath);
            File.Delete(legacyPath + ".tmp");
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async ValueTask<PersistedBucket?> LoadBucketAsync(
        string typeName, ushort typeId, CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        var path = PathFor(typeName, typeId);

        await _ioLock.WaitAsync(cancellationToken);

        try
        {
            // Migrate a legacy (pre-TypeId) snapshot file name to the new scheme on first access.
            if (!File.Exists(path))
            {
                var legacyPath = LegacyPathFor(typeName);

                if (File.Exists(legacyPath))
                {
                    File.Move(legacyPath, path);
                }
            }

            if (!File.Exists(path))
            {
                return null;
            }

            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            var envelope = SnapshotEnvelopeCodec.Decode(bytes);

            var checksumValid = envelope.Version >= 2
                ? SnapshotEnvelopeCodec.ComputeFullChecksum(bytes) == envelope.Checksum
                : ChecksumUtils.Compute(envelope.Bucket.Payload) == envelope.Checksum;

            if (!checksumValid || !string.Equals(envelope.Bucket.TypeName, typeName, StringComparison.Ordinal))
            {
                _logger.Error("Snapshot {Path}: checksum or type-name mismatch; treating as absent", path);

                return null;
            }

            return new PersistedBucket(envelope.Bucket, envelope.LastSequenceId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Snapshot {Path}: unreadable; treating as absent", path);

            return null;
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async ValueTask SaveBucketAsync(
        EntitySnapshotBucket bucket, long lastSequenceId, CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(bucket);

        var envelope = new SnapshotFileEnvelope
        {
            Version = 2,
            LastSequenceId = lastSequenceId,
            Checksum = 0,
            Bucket = bucket
        };

        var path = PathFor(bucket.TypeName, bucket.TypeId);
        var tempPath = path + ".tmp";
        var bytes = SnapshotEnvelopeCodec.Encode(envelope);

        // Checksum covers everything except its own 4 bytes; patch it into the encoded buffer in place.
        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(SnapshotEnvelopeCodec.ChecksumOffset),
            SnapshotEnvelopeCodec.ComputeFullChecksum(bytes)
        );

        await _ioLock.WaitAsync(cancellationToken);

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.WriteAsync(bytes, cancellationToken);

                if (_durability == DurabilityMode.Durable)
                {
                    stream.Flush(flushToDisk: true); // fsync the temp file before the atomic rename
                }
                else
                {
                    await stream.FlushAsync(cancellationToken);
                }
            }

            File.Move(tempPath, path, true);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private string PathFor(string typeName, ushort typeId)
    {
        return Path.Combine(_directory, SnakeName(typeName) + "_" + typeId + _suffix);
    }

    private string LegacyPathFor(string typeName)
    {
        return Path.Combine(_directory, SnakeName(typeName) + _suffix);
    }

    private string SnakeName(string typeName)
    {
        if (typeName.AsSpan().IndexOfAny(_invalidTypeNameChars) >= 0)
        {
            throw new InvalidOperationException($"Persisted type name '{typeName}' cannot be used as a snapshot file name.");
        }

        return StringUtils.ToSnakeCase(typeName);
    }

    public void Dispose()
        => _ioLock.Dispose();
}
