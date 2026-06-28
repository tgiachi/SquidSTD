using System.Net;
using SquidStd.Network.Interfaces.Client;
using SquidStd.Network.Server;

namespace SquidStd.Network.Sessions;

/// <summary>
///     Virtual per-endpoint connection backing a UDP session. Sends route to the server's
///     <see cref="SquidStdUdpServer.SendToAsync" />; closing removes the session via a callback.
/// </summary>
internal sealed class UdpSessionConnection : INetworkConnection
{
    private readonly Action _onClose;
    private readonly IPEndPoint _remoteEndPoint;
    private readonly SquidStdUdpServer _server;
    private int _closed;

    public long SessionId { get; }
    public EndPoint? RemoteEndPoint => _remoteEndPoint;
    public bool IsConnected => Volatile.Read(ref _closed) == 0;

    public UdpSessionConnection(SquidStdUdpServer server, IPEndPoint remoteEndPoint, long sessionId, Action onClose)
    {
        _server = server;
        _remoteEndPoint = remoteEndPoint;
        SessionId = sessionId;
        _onClose = onClose;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _closed, 1) == 0)
        {
            _onClose();
        }

        return Task.CompletedTask;
    }

    public Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        return _server.SendToAsync(_remoteEndPoint, payload, cancellationToken);
    }
}
