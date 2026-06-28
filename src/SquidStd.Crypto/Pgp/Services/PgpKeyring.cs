using System.Collections.Concurrent;
using System.Text;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using SquidStd.Crypto.Pgp.Data;
using SquidStd.Crypto.Pgp.Interfaces;
using SquidStd.Crypto.Pgp.Internal;

namespace SquidStd.Crypto.Pgp.Services;

/// <summary>
///     Thread-safe in-memory keyring indexed by identity, key id, and fingerprint.
/// </summary>
public sealed class PgpKeyring : IPgpKeyring
{
    private const string SecretHeader = "BEGIN PGP PRIVATE KEY BLOCK";
    private readonly ConcurrentDictionary<string, PgpKey> _byKeyId = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlyCollection<PgpKey> Keys => _byKeyId.Values.ToArray();

    /// <inheritdoc />
    public PgpKey Import(string armored)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(armored);

        var key = armored.Contains(SecretHeader, StringComparison.Ordinal)
            ? PgpKeyFactory.FromArmored(ExportPublic(armored), armored)
            : PgpKeyFactory.FromArmored(armored, null);

        _byKeyId[key.KeyId] = key;

        return key;
    }

    /// <inheritdoc />
    public bool Remove(string identityOrKeyIdOrFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityOrKeyIdOrFingerprint);

        var match = Find(identityOrKeyIdOrFingerprint);

        return match is not null && _byKeyId.TryRemove(match.KeyId, out _);
    }

    /// <inheritdoc />
    public PgpKey? Find(string identityOrKeyIdOrFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityOrKeyIdOrFingerprint);

        if (_byKeyId.TryGetValue(identityOrKeyIdOrFingerprint, out var byId))
        {
            return byId;
        }

        foreach (var key in _byKeyId.Values)
        {
            if (string.Equals(key.Identity, identityOrKeyIdOrFingerprint, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key.Fingerprint, identityOrKeyIdOrFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public bool Contains(string identityOrKeyIdOrFingerprint)
    {
        return Find(identityOrKeyIdOrFingerprint) is not null;
    }

    /// <inheritdoc />
    public async Task LoadAsync(IPgpKeyStore store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);

        var loaded = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
        _byKeyId.Clear();

        foreach (var key in loaded)
        {
            _byKeyId[key.KeyId] = key;
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(IPgpKeyStore store, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);

        return store.SaveAsync(Keys, cancellationToken);
    }

    private static string ExportPublic(string secretArmored)
    {
        using var input = PgpUtilities.GetDecoderStream(
            new MemoryStream(Encoding.UTF8.GetBytes(secretArmored))
        );
        var ring = new PgpSecretKeyRingBundle(input).GetKeyRings().Cast<PgpSecretKeyRing>().First();

        using var output = new MemoryStream();
        using (var armor = new ArmoredOutputStream(output))
        {
            foreach (PgpSecretKey secretKey in ring.GetSecretKeys())
            {
                secretKey.PublicKey.Encode(armor);
            }
        }

        return Encoding.UTF8.GetString(output.ToArray());
    }
}
