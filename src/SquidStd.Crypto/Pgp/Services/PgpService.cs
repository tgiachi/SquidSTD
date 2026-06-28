using System.Globalization;
using System.Text;
using PgpCore;
using SquidStd.Crypto.Pgp.Data;
using SquidStd.Crypto.Pgp.Interfaces;
using SquidStd.Crypto.Pgp.Internal;
using BcPgpException = Org.BouncyCastle.Bcpg.OpenPgp.PgpException;

namespace SquidStd.Crypto.Pgp.Services;

/// <summary>
///     OpenPGP operations over a keyring, implemented with PgpCore. Every byte/armored-string operation
///     round-trips through <see cref="MemoryStream" /> so binary payloads survive intact.
/// </summary>
public sealed class PgpService : IPgpService
{
    private readonly IPgpKeyring _keyring;

    public PgpService(IPgpKeyring keyring)
    {
        _keyring = keyring;
    }

    /// <inheritdoc />
    public PgpKey GenerateKey(string identity, string passphrase, PgpKeyOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);

        var settings = options ?? new PgpKeyOptions();
        var expirationSeconds = settings.ExpiresAfter is { } span ? (long)span.TotalSeconds : 0L;

        var pgp = new PGP();
        using var publicStream = new MemoryStream();
        using var privateStream = new MemoryStream();
        pgp.GenerateKey(
            publicStream,
            privateStream,
            identity,
            passphrase,
            settings.KeySizeBits,
            armor: true,
            keyExpirationInSeconds: expirationSeconds
        );

        var publicArmored = ReadAll(publicStream);
        var privateArmored = ReadAll(privateStream);

        return PgpKeyFactory.FromArmored(publicArmored, privateArmored);
    }

    /// <inheritdoc />
    public async Task<string> EncryptForAsync(string recipientIdentity, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var recipient = RequireKey(recipientIdentity);
        var pgp = new PGP(new EncryptionKeys(recipient.PublicArmored));

        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        await pgp.EncryptAsync(input, output).ConfigureAwait(false);

        return Encoding.UTF8.GetString(output.ToArray());
    }

    /// <inheritdoc />
    public async Task EncryptForAsync(string recipientIdentity, Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        var recipient = RequireKey(recipientIdentity);
        var pgp = new PGP(new EncryptionKeys(recipient.PublicArmored));
        await pgp.EncryptAsync(input, output).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<byte[]> DecryptAsync(string armored, string passphrase, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(armored);

        var secret = RequireSecretFor(armored);
        var pgp = new PGP(new EncryptionKeys(secret.PrivateArmored!, passphrase));

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(armored));
        using var output = new MemoryStream();
        await pgp.DecryptAsync(input, output).ConfigureAwait(false);

        return output.ToArray();
    }

    /// <inheritdoc />
    public async Task DecryptAsync(Stream input, Stream output, string passphrase, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        // Buffer to a seekable copy so the recipient key id can be read before decrypting.
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        var armored = Encoding.UTF8.GetString(buffer.ToArray());

        var secret = RequireSecretFor(armored);
        var pgp = new PGP(new EncryptionKeys(secret.PrivateArmored!, passphrase));

        buffer.Position = 0;
        await pgp.DecryptAsync(buffer, output).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> EncryptAndSignForAsync(string recipientIdentity, byte[] data, string signerIdentity, string signerPassphrase, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var pgp = BuildEncryptAndSign(recipientIdentity, signerIdentity, signerPassphrase);

        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        await pgp.EncryptAndSignAsync(input, output).ConfigureAwait(false);

        return Encoding.UTF8.GetString(output.ToArray());
    }

    /// <inheritdoc />
    public async Task EncryptAndSignForAsync(string recipientIdentity, Stream input, Stream output, string signerIdentity, string signerPassphrase, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        var pgp = BuildEncryptAndSign(recipientIdentity, signerIdentity, signerPassphrase);
        await pgp.EncryptAndSignAsync(input, output).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PgpDecryptionResult> DecryptAndVerifyAsync(string armored, string passphrase, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(armored);

        var secret = RequireSecretFor(armored);
        var publicKeys = _keyring.Keys.Select(key => key.PublicArmored).ToArray();
        var pgp = new PGP(new EncryptionKeys(publicKeys, secret.PrivateArmored!, passphrase));

        var isSigned = pgp.Inspect(armored).IsSigned;

        if (!isSigned)
        {
            var plain = await DecryptWith(pgp, armored).ConfigureAwait(false);

            return new PgpDecryptionResult(plain, false, false);
        }

        try
        {
            using var input = new MemoryStream(Encoding.UTF8.GetBytes(armored));
            using var output = new MemoryStream();
            await pgp.DecryptAndVerifyAsync(input, output).ConfigureAwait(false);

            return new PgpDecryptionResult(output.ToArray(), true, true);
        }
        catch (Exception ex) when (ex is BcPgpException or InvalidOperationException or ArgumentException or IOException)
        {
            var plain = await DecryptWith(pgp, armored).ConfigureAwait(false);

            return new PgpDecryptionResult(plain, true, false);
        }
    }

    /// <inheritdoc />
    public async Task<string> SignAsync(byte[] data, string signerIdentity, string passphrase, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var signer = RequireKey(signerIdentity);
        var pgp = new PGP(new EncryptionKeys(signer.PrivateArmored!, passphrase));

        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        await pgp.SignAsync(input, output).ConfigureAwait(false);

        return Encoding.UTF8.GetString(output.ToArray());
    }

    /// <inheritdoc />
    public async Task<PgpVerificationResult> VerifyAsync(string signedMessage, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signedMessage);

        var recovered = Array.Empty<byte>();

        foreach (var key in _keyring.Keys)
        {
            using var input = new MemoryStream(Encoding.UTF8.GetBytes(signedMessage));
            using var output = new MemoryStream();
            var pgp = new PGP(new EncryptionKeys(key.PublicArmored));

            bool ok;
            try
            {
                ok = await pgp.VerifyAsync(input, output).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is BcPgpException or InvalidOperationException or ArgumentException or IOException)
            {
                ok = false;
            }

            if (output.Length > 0)
            {
                recovered = output.ToArray();
            }

            if (ok)
            {
                return new PgpVerificationResult(true, recovered);
            }
        }

        return new PgpVerificationResult(false, recovered);
    }

    private PGP BuildEncryptAndSign(string recipientIdentity, string signerIdentity, string signerPassphrase)
    {
        var recipient = RequireKey(recipientIdentity);
        var signer = RequireKey(signerIdentity);

        return new PGP(new EncryptionKeys(recipient.PublicArmored, signer.PrivateArmored!, signerPassphrase));
    }

    private static async Task<byte[]> DecryptWith(PGP pgp, string armored)
    {
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(armored));
        using var output = new MemoryStream();
        await pgp.DecryptAsync(input, output).ConfigureAwait(false);

        return output.ToArray();
    }

    private PgpKey RequireKey(string identity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);

        return _keyring.Find(identity)
               ?? throw new KeyNotFoundException($"No key for identity '{identity}' in the keyring.");
    }

    private PgpKey RequireSecretFor(string armored)
    {
        var recipientIds = new PGP().GetRecipients(armored).ToHashSet();

        foreach (var key in _keyring.Keys)
        {
            if (key.HasSecret && recipientIds.Contains(ParseKeyId(key.KeyId)))
            {
                return key;
            }
        }

        throw new KeyNotFoundException("No secret key in the keyring matches the message recipients.");
    }

    private static long ParseKeyId(string keyId)
    {
        return long.Parse(keyId, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static string ReadAll(MemoryStream stream)
    {
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
