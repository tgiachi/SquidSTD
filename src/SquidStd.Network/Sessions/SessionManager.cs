using System.Collections.Concurrent;
using Serilog;
using SquidStd.Network.Data.Events;
using SquidStd.Network.Interfaces.Client;
using SquidStd.Network.Interfaces.Sessions;
using SquidStd.Network.Server;

namespace SquidStd.Network.Sessions;

/// <summary>
///     Observes a <see cref="SquidTcpServer" /> and maintains a registry of <see cref="Session{TState}" />.
///     The server is not modified; the manager subscribes to its lifecycle events.
/// </summary>
/// <typeparam name="TState">Application-defined per-connection state.</typeparam>
public sealed class SessionManager<TState> : ISessionManager<TState>, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<SessionManager<TState>>();
    private readonly SquidTcpServer _server;
    private readonly ConcurrentDictionary<long, Session<TState>> _sessions = new();
    private readonly Func<INetworkConnection, TState> _stateFactory;
    private int _disposed;

    /// <inheritdoc />
    public int Count => _sessions.Count;

    /// <inheritdoc />
    public IReadOnlyCollection<Session<TState>> Sessions => _sessions.Values.ToArray();

    public SessionManager(SquidTcpServer server, Func<INetworkConnection, TState> stateFactory)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(stateFactory);

        _server = server;
        _stateFactory = stateFactory;

        _server.OnClientConnect += HandleServerClientConnect;
        _server.OnClientDisconnect += HandleServerClientDisconnect;
        _server.OnDataReceived += HandleServerDataReceived;
    }

    /// <inheritdoc />
    public async Task BroadcastAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        var snapshot = _sessions.Values.ToArray();
        var tasks = new List<Task>(snapshot.Length);

        foreach (var session in snapshot)
        {
            tasks.Add(SendSafelyAsync(session, payload, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public Task DisconnectAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return _sessions.TryGetValue(sessionId, out var session)
            ? session.CloseAsync(cancellationToken)
            : Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendAsync(long sessionId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        return _sessions.TryGetValue(sessionId, out var session)
            ? session.SendAsync(payload, cancellationToken)
            : Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool TryGetSession(long sessionId, out Session<TState>? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    internal void HandleConnected(INetworkConnection connection)
    {
        var session = new Session<TState>(
            connection.SessionId,
            connection,
            _stateFactory(connection),
            DateTimeOffset.UtcNow
        );

        if (_sessions.TryAdd(session.SessionId, session))
        {
            RaiseSessionCreated(session);
        }
    }

    internal void HandleData(INetworkConnection connection, ReadOnlyMemory<byte> data)
    {
        if (_sessions.TryGetValue(connection.SessionId, out var session))
        {
            RaiseSessionData(session, data);
        }
        else
        {
            _logger.Debug("Data received for unknown session {SessionId}", connection.SessionId);
        }
    }

    internal void HandleDisconnected(INetworkConnection connection)
    {
        if (_sessions.TryRemove(connection.SessionId, out var session))
        {
            RaiseSessionRemoved(session);
        }
    }

    private void HandleServerClientConnect(object? sender, SquidStdTcpClientEventArgs e)
    {
        HandleConnected(e.Client);
    }

    private void HandleServerClientDisconnect(object? sender, SquidStdTcpClientEventArgs e)
    {
        HandleDisconnected(e.Client);
    }

    private void HandleServerDataReceived(object? sender, SquidStdTcpDataReceivedEventArgs e)
    {
        HandleData(e.Client, e.Data);
    }

    private void RaiseSessionCreated(Session<TState> session)
    {
        try
        {
            OnSessionCreated?.Invoke(this, new SquidStdSessionEventArgs<TState>(session));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "OnSessionCreated handler failed for session {SessionId}", session.SessionId);
        }
    }

    private void RaiseSessionData(Session<TState> session, ReadOnlyMemory<byte> data)
    {
        try
        {
            OnSessionData?.Invoke(this, new SquidStdSessionDataEventArgs<TState>(session, data));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "OnSessionData handler failed for session {SessionId}", session.SessionId);
        }
    }

    private void RaiseSessionRemoved(Session<TState> session)
    {
        try
        {
            OnSessionRemoved?.Invoke(this, new SquidStdSessionEventArgs<TState>(session));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "OnSessionRemoved handler failed for session {SessionId}", session.SessionId);
        }
    }

    private async Task SendSafelyAsync(
        Session<TState> session,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await session.SendAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Broadcast send failed for session {SessionId}", session.SessionId);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _server.OnClientConnect -= HandleServerClientConnect;
        _server.OnClientDisconnect -= HandleServerClientDisconnect;
        _server.OnDataReceived -= HandleServerDataReceived;
        _sessions.Clear();
    }

    /// <inheritdoc />
    public event EventHandler<SquidStdSessionEventArgs<TState>>? OnSessionCreated;

    /// <inheritdoc />
    public event EventHandler<SquidStdSessionEventArgs<TState>>? OnSessionRemoved;

    /// <inheritdoc />
    public event EventHandler<SquidStdSessionDataEventArgs<TState>>? OnSessionData;
}
