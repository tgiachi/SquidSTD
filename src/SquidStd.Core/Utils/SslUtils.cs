using System.Security.Cryptography.X509Certificates;

namespace SquidStd.Core.Utils;

/// <summary>
///     Helpers for loading X.509 certificates used to secure TLS endpoints.
/// </summary>
public static class SslUtils
{
    /// <summary>
    ///     Loads a certificate from a PEM file, optionally combining it with a separate private-key PEM.
    /// </summary>
    /// <param name="certificatePath">Path to the certificate PEM file.</param>
    /// <param name="privateKeyPath">Optional path to the private-key PEM file.</param>
    /// <returns>The loaded certificate.</returns>
    public static X509Certificate2 LoadFromPem(string certificatePath, string? privateKeyPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePath);

        return X509Certificate2.CreateFromPemFile(certificatePath, privateKeyPath);
    }

    /// <summary>
    ///     Loads a certificate (with its private key) from a PKCS#12 / PFX file.
    /// </summary>
    /// <param name="path">Path to the PFX file.</param>
    /// <param name="password">Optional password protecting the PFX file.</param>
    /// <returns>The loaded certificate.</returns>
    public static X509Certificate2 LoadFromPfx(string path, string? password = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return X509CertificateLoader.LoadPkcs12FromFile(path, password);
    }
}
