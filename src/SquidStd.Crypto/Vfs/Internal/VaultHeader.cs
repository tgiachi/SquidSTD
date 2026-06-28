using System.Text.Json;

namespace SquidStd.Crypto.Vfs.Internal;

/// <summary>Cleartext vault header: format magic/version, Argon2id salt + cost params, and chunk size.</summary>
internal sealed record VaultHeader(
    string Magic,
    int Version,
    byte[] Salt,
    int MemoryKib,
    int Iterations,
    int Parallelism,
    int ChunkSize
)
{
    public byte[] Serialize()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }

    public static VaultHeader Parse(byte[] data)
    {
        return JsonSerializer.Deserialize<VaultHeader>(data)
               ?? throw new InvalidDataException("Vault header is empty or invalid.");
    }
}
