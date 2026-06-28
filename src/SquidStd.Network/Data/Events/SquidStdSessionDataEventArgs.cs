using SquidStd.Network.Sessions;

namespace SquidStd.Network.Data.Events;

/// <summary>
///     Event payload carrying a session and the data it received.
/// </summary>
public sealed class SquidStdSessionDataEventArgs<TState> : EventArgs
{
    /// <summary>The session that received the data.</summary>
    public Session<TState> Session { get; }

    /// <summary>The received payload (already processed by the server pipeline).</summary>
    public ReadOnlyMemory<byte> Data { get; }

    public SquidStdSessionDataEventArgs(Session<TState> session, ReadOnlyMemory<byte> data)
    {
        Session = session;
        Data = data;
    }
}
