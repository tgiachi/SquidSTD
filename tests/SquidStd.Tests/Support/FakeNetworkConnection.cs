using System.Net;
using SquidStd.Network.Interfaces.Client;

namespace SquidStd.Tests.Support;

/// <summary>
/// In-memory <see cref="INetworkConnection" /> for testing sessions without sockets.
/// Records sent payloads and close calls; SendCallback can inject failures.
/// </summary>
public sealed class FakeNetworkConnection : INetworkConnection
{
    private readonly List<byte[]> _sent = new();

    public long SessionId { get; }
    public EndPoint? RemoteEndPoint { get; }
    public bool IsConnected { get; private set; } = true;
    public IReadOnlyList<byte[]> SentPayloads => _sent;
    public int CloseCount { get; private set; }
    public Func<ReadOnlyMemory<byte>, Task>? SendCallback { get; set; }

    public FakeNetworkConnection(long sessionId = 1, EndPoint? remoteEndPoint = null)
    {
        SessionId = sessionId;
        RemoteEndPoint = remoteEndPoint ?? new IPEndPoint(IPAddress.Loopback, 1234);
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        CloseCount++;
        IsConnected = false;

        return Task.CompletedTask;
    }

    public Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        _sent.Add(payload.ToArray());

        return SendCallback?.Invoke(payload) ?? Task.CompletedTask;
    }
}
