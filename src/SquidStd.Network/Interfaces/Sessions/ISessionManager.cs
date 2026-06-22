using SquidStd.Network.Data.Events;
using SquidStd.Network.Sessions;

namespace SquidStd.Network.Interfaces.Sessions;

/// <summary>
/// Tracks active connections and their application state.
/// </summary>
/// <typeparam name="TState">Application-defined per-connection state.</typeparam>
public interface ISessionManager<TState>
{
    /// <summary>Number of active sessions.</summary>
    int Count { get; }

    /// <summary>Snapshot of all active sessions.</summary>
    IReadOnlyCollection<Session<TState>> Sessions { get; }

    /// <summary>Raised when a new session is created.</summary>
    event EventHandler<SquidStdSessionEventArgs<TState>>? OnSessionCreated;

    /// <summary>Raised when a session is removed.</summary>
    event EventHandler<SquidStdSessionEventArgs<TState>>? OnSessionRemoved;

    /// <summary>Raised when a session receives data.</summary>
    event EventHandler<SquidStdSessionDataEventArgs<TState>>? OnSessionData;

    /// <summary>Sends a payload to every active session, isolating per-session failures.</summary>
    Task BroadcastAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);

    /// <summary>Closes a session's connection. No-op when the id is unknown.</summary>
    Task DisconnectAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>Sends a payload to a single session. No-op when the id is unknown.</summary>
    Task SendAsync(long sessionId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);

    /// <summary>Looks up a session by its id.</summary>
    bool TryGetSession(long sessionId, out Session<TState>? session);
}
