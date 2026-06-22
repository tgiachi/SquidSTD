using System.Net;

namespace SquidStd.Network.Data.Events;

/// <summary>
/// Event payload for a datagram received by the UDP server, carrying the sender endpoint.
/// </summary>
public sealed class SquidStdUdpDatagramReceivedEventArgs : EventArgs
{
    /// <summary>Endpoint that sent the datagram.</summary>
    public IPEndPoint RemoteEndPoint { get; }

    /// <summary>Received datagram payload.</summary>
    public ReadOnlyMemory<byte> Data { get; }

    public SquidStdUdpDatagramReceivedEventArgs(IPEndPoint remoteEndPoint, ReadOnlyMemory<byte> data)
    {
        RemoteEndPoint = remoteEndPoint;
        Data = data;
    }
}
