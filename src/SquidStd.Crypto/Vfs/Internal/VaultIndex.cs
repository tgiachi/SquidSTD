using System.Text.Json;

namespace SquidStd.Crypto.Vfs.Internal;

/// <summary>A single index record: the opaque backing blob id, plaintext size, and modification time.</summary>
internal sealed record VaultIndexEntry(string BlobId, long Size, DateTimeOffset ModifiedUtc);

/// <summary>The decrypted vault index: a map from logical path to its backing blob entry.</summary>
internal sealed class VaultIndex
{
    private readonly Dictionary<string, VaultIndexEntry> _entries;

    public IReadOnlyDictionary<string, VaultIndexEntry> Entries => _entries;

    public VaultIndex()
    {
        _entries = new Dictionary<string, VaultIndexEntry>(StringComparer.Ordinal);
    }

    private VaultIndex(Dictionary<string, VaultIndexEntry> entries)
    {
        _entries = entries;
    }

    public void Set(string path, VaultIndexEntry entry)
    {
        _entries[path] = entry;
    }

    public bool TryGet(string path, out VaultIndexEntry? entry)
    {
        return _entries.TryGetValue(path, out entry);
    }

    public bool Remove(string path, out VaultIndexEntry? entry)
    {
        return _entries.Remove(path, out entry);
    }

    public byte[] Serialize()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_entries);
    }

    public static VaultIndex Parse(byte[] data)
    {
        var map = JsonSerializer.Deserialize<Dictionary<string, VaultIndexEntry>>(data)
                  ?? new Dictionary<string, VaultIndexEntry>(StringComparer.Ordinal);

        return new VaultIndex(new Dictionary<string, VaultIndexEntry>(map, StringComparer.Ordinal));
    }
}
