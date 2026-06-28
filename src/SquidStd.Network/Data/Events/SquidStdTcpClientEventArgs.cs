using SquidStd.Network.Client;

namespace SquidStd.Network.Data.Events;

/// <summary>
///     Event payload containing a network client instance.
/// </summary>
public sealed class SquidStdTcpClientEventArgs : EventArgs
{
    /// <summary>
    ///     Connected or disconnected client.
    /// </summary>
    public SquidStdTcpClient Client { get; }

    public SquidStdTcpClientEventArgs(SquidStdTcpClient client)
    {
        Client = client;
    }
}
