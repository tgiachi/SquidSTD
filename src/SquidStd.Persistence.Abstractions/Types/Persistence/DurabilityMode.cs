namespace SquidStd.Persistence.Abstractions.Types.Persistence;

/// <summary>How aggressively persistence writes are flushed to physical storage.</summary>
public enum DurabilityMode
{
    /// <summary>Flush to the OS cache only (fast; survives process crash, not power loss). Default.</summary>
    Buffered,

    /// <summary>fsync the journal append and the snapshot temp file before rename (survives power loss).</summary>
    Durable
}
