using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using SquidStd.Core.Utils;

namespace SquidStd.Tests.Core.Utils;

public class SslUtilsTests
{
    [Fact]
    public void LoadFromPem_LoadsCertificateWithPrivateKey()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=squidstd-pem",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        using var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1)
        );

        var certPath = Path.GetTempFileName();
        var keyPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(certPath, cert.ExportCertificatePem());
            File.WriteAllText(keyPath, rsa.ExportPkcs8PrivateKeyPem());

            using var loaded = SslUtils.LoadFromPem(certPath, keyPath);

            Assert.Equal("CN=squidstd-pem", loaded.Subject);
            Assert.True(loaded.HasPrivateKey);
        }
        finally
        {
            File.Delete(certPath);
            File.Delete(keyPath);
        }
    }

    [Fact]
    public void LoadFromPfx_LoadsCertificateWithPrivateKey()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=squidstd-pfx",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        using var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1)
        );

        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllBytes(path, cert.Export(X509ContentType.Pkcs12, "pw"));

            using var loaded = SslUtils.LoadFromPfx(path, "pw");

            Assert.Equal("CN=squidstd-pfx", loaded.Subject);
            Assert.True(loaded.HasPrivateKey);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromPem_WhenPathBlank_Throws()
    {
        Assert.Throws<ArgumentException>(() => SslUtils.LoadFromPem(" "));
    }
}
