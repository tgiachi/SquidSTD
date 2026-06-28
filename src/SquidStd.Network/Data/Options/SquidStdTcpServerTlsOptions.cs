using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace SquidStd.Network.Data.Options;

public sealed class SquidStdTcpServerTlsOptions
{
    public X509Certificate2 ServerCertificate { get; }

    public bool ClientCertificateRequired { get; init; }

    public bool CheckCertificateRevocation { get; init; }

    public SslProtocols EnabledSslProtocols { get; init; } = SslProtocols.None;

    public SquidStdTcpServerTlsOptions(X509Certificate2 serverCertificate)
    {
        ArgumentNullException.ThrowIfNull(serverCertificate);

        ServerCertificate = serverCertificate;
    }

    internal SslServerAuthenticationOptions ToAuthenticationOptions()
    {
        return new SslServerAuthenticationOptions
        {
            ServerCertificate = ServerCertificate,
            ClientCertificateRequired = ClientCertificateRequired,
            CertificateRevocationCheckMode = CheckCertificateRevocation
                ? X509RevocationMode.Online
                : X509RevocationMode.NoCheck,
            EnabledSslProtocols = EnabledSslProtocols
        };
    }
}
