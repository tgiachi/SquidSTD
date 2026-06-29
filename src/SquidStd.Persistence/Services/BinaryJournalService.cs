using System.Buffers.Binary;
using Serilog;
using SquidStd.Core.Utils;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Internal;
using ILogger = Serilog.ILogger;

namespace SquidStd.Persistence.Services;

/// <summary>
/// Append-only journal stored as length+checksum-framed fixed-binary records. A corrupt trailing
/// record (truncated write or checksum mismatch) is detected on read and the tail is discarded.
/// </summary>
public sealed class BinaryJournalService : IJournalService, IAsyncDisposable
{
    private const int FrameHeaderSize = 8; // int length + uint checksum

    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private readonly ILogger _logger = Log.ForContext<BinaryJournalService>();
    private readonly string _path;

    public BinaryJournalService(string journalFilePath, bool enableFileLock = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFilePath);

        _path = Path.GetFullPath(journalFilePath);
        _ = enableFileLock;

        var directory = Path.GetDirectoryName(_path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public async ValueTask AppendAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await _ioLock.WaitAsync(cancellationToken);

        try
        {
            await using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read);
            await WriteRecordAsync(stream, entry, cancellationToken);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async ValueTask AppendBatchAsync(
        IReadOnlyList<JournalEntry> entries, CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entries);

        await _ioLock.WaitAsync(cancellationToken);

        try
        {
            await using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read);

            for (var i = 0; i < entries.Count; i++)
            {
                await WriteRecordAsync(stream, entries[i], cancellationToken);
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async ValueTask<IReadOnlyCollection<JournalEntry>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        await _ioLock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_path))
            {
                return [];
            }

            return ParseAll(await File.ReadAllBytesAsync(_path, cancellationToken));
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async ValueTask ResetAsync(CancellationToken cancellationToken = default)
    {
        await _ioLock.WaitAsync(cancellationToken);

        try
        {
            await RewriteAsync([], cancellationToken);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async ValueTask TrimThroughSequenceAsync(long inclusiveSequenceId, CancellationToken cancellationToken = default)
    {
        await _ioLock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_path))
            {
                return;
            }

            var kept = ParseAll(await File.ReadAllBytesAsync(_path, cancellationToken))
                .Where(entry => entry.SequenceId > inclusiveSequenceId)
                .ToArray();

            await RewriteAsync(kept, cancellationToken);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private List<JournalEntry> ParseAll(byte[] bytes)
    {
        var entries = new List<JournalEntry>();
        var offset = 0;

        while (offset + FrameHeaderSize <= bytes.Length)
        {
            var length = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));
            var checksum = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4));

            // A record shorter than the fixed entry header cannot be decoded (Decode reads the header
            // unconditionally); treat it as a truncated tail rather than letting it crash startup.
            if (length < JournalRecordCodec.FixedHeader || offset + FrameHeaderSize + length > bytes.Length)
            {
                _logger.Warning("Journal {Path}: truncated record at offset {Offset}; discarding tail", _path, offset);

                break;
            }

            var record = bytes.AsSpan(offset + FrameHeaderSize, length);

            if (ChecksumUtils.Compute(record) != checksum)
            {
                _logger.Warning("Journal {Path}: checksum mismatch at offset {Offset}; discarding tail", _path, offset);

                break;
            }

            entries.Add(JournalRecordCodec.Decode(record));
            offset += FrameHeaderSize + length;
        }

        return entries;
    }

    private async ValueTask RewriteAsync(IReadOnlyList<JournalEntry> entries, CancellationToken cancellationToken)
    {
        var tempPath = _path + ".tmp";

        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            for (var i = 0; i < entries.Count; i++)
            {
                await WriteRecordAsync(stream, entries[i], cancellationToken);
            }

            await stream.FlushAsync(cancellationToken);
        }

        File.Move(tempPath, _path, true);
    }

    private static async ValueTask WriteRecordAsync(
        FileStream stream, JournalEntry entry, CancellationToken cancellationToken
    )
    {
        var record = JournalRecordCodec.Encode(entry);
        var header = new byte[FrameHeaderSize];
        BinaryPrimitives.WriteInt32LittleEndian(header, record.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), ChecksumUtils.Compute(record));

        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(record, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _ioLock.Dispose();

        return ValueTask.CompletedTask;
    }
}
