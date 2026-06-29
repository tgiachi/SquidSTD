using System.Buffers.Binary;
using SquidStd.Persistence.Abstractions.Data;
using SquidStd.Persistence.Abstractions.Types;

namespace SquidStd.Persistence.Internal;

/// <summary>
/// Fixed-layout binary encoder/decoder for a <see cref="JournalEntry" />: no MessagePack, no reflection.
/// Layout: long Seq | long Ts | ushort TypeId | byte Op | int PayloadLen | payload bytes.
/// </summary>
internal static class JournalRecordCodec
{
    internal const int FixedHeader = 8 + 8 + 2 + 1 + 4;

    public static byte[] Encode(JournalEntry entry)
    {
        var buffer = new byte[FixedHeader + entry.Payload.Length];
        var span = buffer.AsSpan();

        BinaryPrimitives.WriteInt64LittleEndian(span, entry.SequenceId);
        BinaryPrimitives.WriteInt64LittleEndian(span[8..], entry.TimestampUnixMilliseconds);
        BinaryPrimitives.WriteUInt16LittleEndian(span[16..], entry.TypeId);
        span[18] = (byte)entry.Operation;
        BinaryPrimitives.WriteInt32LittleEndian(span[19..], entry.Payload.Length);
        entry.Payload.CopyTo(span[FixedHeader..]);

        return buffer;
    }

    public static JournalEntry Decode(ReadOnlySpan<byte> bytes)
    {
        var sequenceId = BinaryPrimitives.ReadInt64LittleEndian(bytes);
        var timestamp = BinaryPrimitives.ReadInt64LittleEndian(bytes[8..]);
        var typeId = BinaryPrimitives.ReadUInt16LittleEndian(bytes[16..]);
        var operation = (JournalEntityOperationType)bytes[18];
        var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(bytes[19..]);
        var payload = bytes.Slice(FixedHeader, payloadLength).ToArray();

        return new JournalEntry
        {
            SequenceId = sequenceId,
            TimestampUnixMilliseconds = timestamp,
            TypeId = typeId,
            Operation = operation,
            Payload = payload
        };
    }
}
