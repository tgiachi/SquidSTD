using System.Net;
using SquidStd.Network.Interfaces.Client;

namespace SquidStd.Network.Sessions;

/// <summary>
/// A tracked network connection with associated application state.
/// </summary>
/// <typeparam name="TState">Application-defined per-connection state.</typeparam>
public sealed class Session<TState>
{
    /// <summary>Unique connection identifier assigned by the transport.</summary>
    public long SessionId { get; }

    /// <summary>Underlying transport connection.</summary>
    public INetworkConnection Connection { get; }

    /// <summary>Application-defined state for this session.</summary>
    public TState State { get; }

    /// <summary>UTC instant the session was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Remote endpoint of the connection, when available.</summary>
    public EndPoint? RemoteEndPoint => Connection.RemoteEndPoint;

    /// <summary>Whether the underlying connection is still open.</summary>
    public bool IsConnected => Connection.IsConnected;

    public Session(long sessionId, INetworkConnection connection, TState state, DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(connection);

        Connection = connection;
        SessionId = sessionId;
        State = state;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Closes the underlying connection.</summary>
    public Task CloseAsync(CancellationToken cancellationToken = default)
        => Connection.CloseAsync(cancellationToken);

    /// <summary>Sends a payload over the underlying connection.</summary>
    public Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
        => Connection.SendAsync(payload, cancellationToken);
}
