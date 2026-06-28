using SquidStd.Network.Client;

namespace SquidStd.Network.Data.Events;

/// <summary>
///     Event payload containing data received from a network client.
/// </summary>
public sealed class SquidStdTcpDataReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     Source client for the data payload.
    /// </summary>
    public SquidStdTcpClient Client { get; }

    /// <summary>
    ///     Received data payload.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    public SquidStdTcpDataReceivedEventArgs(SquidStdTcpClient client, ReadOnlyMemory<byte> data)
    {
        Client = client;
        Data = data;
    }
}
