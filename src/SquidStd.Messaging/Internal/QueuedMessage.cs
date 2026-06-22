namespace SquidStd.Messaging.Internal;

/// <summary>
/// A buffered queue message with its delivery attempt count.
/// </summary>
/// <param name="Payload">The raw message payload.</param>
/// <param name="Attempt">Number of delivery attempts already made (0 on first enqueue).</param>
internal sealed record QueuedMessage(ReadOnlyMemory<byte> Payload, int Attempt);
