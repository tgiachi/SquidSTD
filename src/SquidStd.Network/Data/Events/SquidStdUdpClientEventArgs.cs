using SquidStd.Network.Client;

namespace SquidStd.Network.Data.Events;

/// <summary>
///     Event payload containing a UDP client instance.
/// </summary>
public sealed class SquidStdUdpClientEventArgs : EventArgs
{
    /// <summary>
    ///     Started or closed UDP client.
    /// </summary>
    public SquidStdUdpClient Client { get; }

    public SquidStdUdpClientEventArgs(SquidStdUdpClient client)
    {
        Client = client;
    }
}
