using SquidStd.Persistence.Abstractions.Types.Persistence;

namespace SquidStd.Persistence.Abstractions.Data;

/// <summary>Configuration for the persistence service: autosave cadence and snapshot/journal file names.</summary>
public sealed class PersistenceConfig
{
    public TimeSpan AutosaveInterval { get; set; } = TimeSpan.FromSeconds(300);
    public string SnapshotFileSuffix { get; set; } = ".snapshot.bin";
    public string JournalFileName { get; set; } = "world.journal.bin";
    public bool EnableFileLock { get; set; } = true;
    public string? SaveDirectory { get; set; }
    public DurabilityMode DurabilityMode { get; set; } = DurabilityMode.Buffered;
}
