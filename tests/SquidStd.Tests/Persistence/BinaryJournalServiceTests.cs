using System.Buffers.Binary;
using SquidStd.Core.Utils;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Types;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public sealed class BinaryJournalServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "squidstd-journal-" + Guid.NewGuid().ToString("N"));

    private string JournalPath => Path.Combine(_dir, "world.journal.bin");

    private static JournalEntry Entry(long seq, JournalEntityOperationType op = JournalEntityOperationType.Upsert)
        => new()
        {
            SequenceId = seq,
            TimestampUnixMilliseconds = seq * 1000,
            TypeId = 1,
            Operation = op,
            Payload = [(byte)seq]
        };

    [Fact]
    public async Task AppendThenReadAll_RoundTrips()
    {
        await using var journal = new BinaryJournalService(JournalPath);
        await journal.AppendAsync(Entry(1));
        await journal.AppendAsync(Entry(2));

        var entries = (await journal.ReadAllAsync()).ToArray();

        Assert.Equal([1, 2], entries.Select(e => e.SequenceId));
    }

    [Fact]
    public async Task AppendBatch_WritesAll()
    {
        await using var journal = new BinaryJournalService(JournalPath);
        await journal.AppendBatchAsync([Entry(1), Entry(2), Entry(3)]);

        Assert.Equal(3, (await journal.ReadAllAsync()).Count);
    }

    [Fact]
    public async Task ReadAll_DiscardsTruncatedTail()
    {
        await using (var journal = new BinaryJournalService(JournalPath))
        {
            await journal.AppendAsync(Entry(1));
            await journal.AppendAsync(Entry(2));
        }

        var bytes = await File.ReadAllBytesAsync(JournalPath);
        await File.WriteAllBytesAsync(JournalPath, bytes[..^1]); // chop one byte off the last record

        await using var reopened = new BinaryJournalService(JournalPath);
        var entries = (await reopened.ReadAllAsync()).ToArray();

        Assert.Equal([1], entries.Select(e => e.SequenceId));
    }

    [Fact]
    public async Task ReadAll_DiscardsTailOnChecksumMismatch()
    {
        await using (var journal = new BinaryJournalService(JournalPath))
        {
            await journal.AppendAsync(Entry(1));
        }

        var bytes = await File.ReadAllBytesAsync(JournalPath);
        bytes[^1] ^= 0xFF; // corrupt last payload byte
        await File.WriteAllBytesAsync(JournalPath, bytes);

        await using var reopened = new BinaryJournalService(JournalPath);

        Assert.Empty(await reopened.ReadAllAsync());
    }

    [Fact]
    public async Task ReadAll_DiscardsRecordShorterThanFixedHeader_WithoutThrowing()
    {
        await using (var journal = new BinaryJournalService(JournalPath))
        {
            await journal.AppendAsync(Entry(1));
        }

        // Append a well-framed but undersized record (valid length + checksum, but fewer bytes than
        // the fixed entry header). The reader must discard it as a truncated tail, not crash decoding.
        var bytes = (await File.ReadAllBytesAsync(JournalPath)).ToList();
        var shortRecord = new byte[] { 1, 2, 3, 4, 5 };
        var frameHeader = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(frameHeader, shortRecord.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(frameHeader.AsSpan(4), ChecksumUtils.Compute(shortRecord));
        bytes.AddRange(frameHeader);
        bytes.AddRange(shortRecord);
        await File.WriteAllBytesAsync(JournalPath, bytes.ToArray());

        await using var reopened = new BinaryJournalService(JournalPath);
        var entries = (await reopened.ReadAllAsync()).ToArray();

        Assert.Equal([1], entries.Select(e => e.SequenceId));
    }

    [Fact]
    public async Task TrimThroughSequence_KeepsNewerOnly()
    {
        await using var journal = new BinaryJournalService(JournalPath);
        await journal.AppendBatchAsync([Entry(1), Entry(2), Entry(3)]);

        await journal.TrimThroughSequenceAsync(2);

        Assert.Equal([3], (await journal.ReadAllAsync()).Select(e => e.SequenceId));
    }

    [Fact]
    public async Task Reset_ClearsJournal()
    {
        await using var journal = new BinaryJournalService(JournalPath);
        await journal.AppendAsync(Entry(1));

        await journal.ResetAsync();

        Assert.Empty(await journal.ReadAllAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }
}
