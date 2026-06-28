using System.Net;
using SquidStd.Network.Client;

namespace SquidStd.Network.Data.Events;

/// <summary>
///     Event payload containing a datagram received from a UDP peer.
/// </summary>
public sealed class SquidStdUdpDataReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     UDP client that received the datagram.
    /// </summary>
    public SquidStdUdpClient Client { get; }

    /// <summary>
    ///     Endpoint that sent the datagram.
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; }

    /// <summary>
    ///     Received datagram payload.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    public SquidStdUdpDataReceivedEventArgs(SquidStdUdpClient client, IPEndPoint remoteEndPoint, ReadOnlyMemory<byte> data)
    {
        Client = client;
        RemoteEndPoint = remoteEndPoint;
        Data = data;
    }
}
