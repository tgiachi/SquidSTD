namespace SquidStd.Network.Sessions;

/// <summary>
/// Internal registry entry pairing a UDP session with its last-activity timestamp.
/// </summary>
/// <typeparam name="TState">Application-defined per-connection state.</typeparam>
internal sealed class UdpSessionEntry<TState>
{
    public Session<TState> Session { get; }
    public DateTimeOffset LastActivityUtc { get; set; }

    public UdpSessionEntry(Session<TState> session, DateTimeOffset lastActivityUtc)
    {
        Session = session;
        LastActivityUtc = lastActivityUtc;
    }
}
