using System.Collections.Concurrent;
using System.Net;
using Serilog;
using SquidStd.Network.Data.Events;
using SquidStd.Network.Interfaces.Client;
using SquidStd.Network.Interfaces.Sessions;
using SquidStd.Network.Server;

namespace SquidStd.Network.Sessions;

/// <summary>
///     Tracks per-endpoint UDP sessions over a <see cref="SquidStdUdpServer" />. Sessions are created on
///     the first datagram from an endpoint and removed by idle-timeout sweep or explicit disconnect.
/// </summary>
/// <typeparam name="TState">Application-defined per-connection state.</typeparam>
public sealed class UdpSessionManager<TState> : ISessionManager<TState>, IDisposable
{
    private readonly ConcurrentDictionary<IPEndPoint, UdpSessionEntry<TState>> _byEndpoint = new();
    private readonly ConcurrentDictionary<long, IPEndPoint> _byId = new();
    private readonly Lock _createLock = new();
    private readonly ILogger _logger = Log.ForContext<UdpSessionManager<TState>>();
    private readonly SquidStdUdpServer _server;
    private readonly Func<INetworkConnection, TState> _stateFactory;
    private readonly ITimer _sweepTimer;
    private readonly TimeProvider _timeProvider;
    private int _disposed;
    private long _sessionIdSequence;

    /// <summary>Idle period after which an inactive session is removed.</summary>
    public TimeSpan IdleTimeout { get; }

    /// <inheritdoc />
    public int Count => _byEndpoint.Count;

    /// <inheritdoc />
    public IReadOnlyCollection<Session<TState>> Sessions => _byEndpoint.Values.Select(entry => entry.Session).ToArray();

    public UdpSessionManager(
        SquidStdUdpServer server,
        Func<INetworkConnection, TState> stateFactory,
        TimeSpan? idleTimeout = null,
        TimeSpan? sweepInterval = null,
        TimeProvider? timeProvider = null
    )
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(stateFactory);

        _server = server;
        _stateFactory = stateFactory;
        IdleTimeout = idleTimeout ?? TimeSpan.FromSeconds(30);
        _timeProvider = timeProvider ?? TimeProvider.System;

        // Suppress the server's default echo while sessions are managed here.
        _server.OnDatagram = static (_, _) => ReadOnlyMemory<byte>.Empty;
        _server.OnDatagramReceived += HandleServerDatagram;

        var interval = sweepInterval ?? TimeSpan.FromSeconds(10);
        _sweepTimer = _timeProvider.CreateTimer(_ => SafeSweep(), null, interval, interval);
    }

    /// <inheritdoc />
    public async Task BroadcastAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        var snapshot = _byEndpoint.Values.ToArray();
        var tasks = new List<Task>(snapshot.Length);

        foreach (var entry in snapshot)
        {
            tasks.Add(SendSafelyAsync(entry.Session, payload, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public Task DisconnectAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return TryGetSession(sessionId, out var session)
            ? session!.CloseAsync(cancellationToken)
            : Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendAsync(long sessionId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        return TryGetSession(sessionId, out var session)
            ? session!.SendAsync(payload, cancellationToken)
            : Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool TryGetSession(long sessionId, out Session<TState>? session)
    {
        if (_byId.TryGetValue(sessionId, out var endPoint) && _byEndpoint.TryGetValue(endPoint, out var entry))
        {
            session = entry.Session;

            return true;
        }

        session = null;

        return false;
    }

    /// <summary>Closes the session for the given endpoint. No-op when unknown.</summary>
    public Task DisconnectAsync(IPEndPoint endPoint, CancellationToken cancellationToken = default)
    {
        return TryGetSession(endPoint, out var session)
            ? session!.CloseAsync(cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Sends a payload to the session for the given endpoint. No-op when unknown.</summary>
    public Task SendToAsync(IPEndPoint endPoint, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        return TryGetSession(endPoint, out var session)
            ? session!.SendAsync(payload, cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Looks up a session by remote endpoint.</summary>
    public bool TryGetSession(IPEndPoint endPoint, out Session<TState>? session)
    {
        if (_byEndpoint.TryGetValue(endPoint, out var entry))
        {
            session = entry.Session;

            return true;
        }

        session = null;

        return false;
    }

    internal void HandleDatagram(IPEndPoint remoteEndPoint, ReadOnlyMemory<byte> data)
    {
        var entry = GetOrCreate(remoteEndPoint, out var created);
        entry.LastActivityUtc = _timeProvider.GetUtcNow();

        if (created)
        {
            RaiseSessionCreated(entry.Session);
        }

        RaiseSessionData(entry.Session, data);
    }

    internal void SweepExpiredSessions()
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var kvp in _byEndpoint.ToArray())
        {
            if (now - kvp.Value.LastActivityUtc > IdleTimeout)
            {
                RemoveSession(kvp.Key);
            }
        }
    }

    private UdpSessionEntry<TState> GetOrCreate(IPEndPoint remoteEndPoint, out bool created)
    {
        if (_byEndpoint.TryGetValue(remoteEndPoint, out var existing))
        {
            created = false;

            return existing;
        }

        lock (_createLock)
        {
            if (_byEndpoint.TryGetValue(remoteEndPoint, out existing))
            {
                created = false;

                return existing;
            }

            var sessionId = Interlocked.Increment(ref _sessionIdSequence);
            var connection = new UdpSessionConnection(
                _server,
                remoteEndPoint,
                sessionId,
                () => RemoveSession(remoteEndPoint)
            );
            var session = new Session<TState>(sessionId, connection, _stateFactory(connection), _timeProvider.GetUtcNow());
            var entry = new UdpSessionEntry<TState>(session, _timeProvider.GetUtcNow());

            _byEndpoint[remoteEndPoint] = entry;
            _byId[sessionId] = remoteEndPoint;
            created = true;

            return entry;
        }
    }

    private void HandleServerDatagram(object? sender, SquidStdUdpDatagramReceivedEventArgs e)
    {
        HandleDatagram(e.RemoteEndPoint, e.Data);
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

    private void RemoveSession(IPEndPoint endPoint)
    {
        if (_byEndpoint.TryRemove(endPoint, out var entry))
        {
            _byId.TryRemove(entry.Session.SessionId, out _);

            // Mark the connection closed so holders observe IsConnected == false after a sweep
            // removal. CloseAsync is idempotent, so the explicit-close path (which routes here via the
            // close callback) is unaffected.
            _ = entry.Session.Connection.CloseAsync();
            RaiseSessionRemoved(entry.Session);
        }
    }

    private void SafeSweep()
    {
        try
        {
            SweepExpiredSessions();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "UDP session sweep failed");
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

        _server.OnDatagramReceived -= HandleServerDatagram;
        _sweepTimer.Dispose();
        _byEndpoint.Clear();
        _byId.Clear();
    }

    /// <inheritdoc />
    public event EventHandler<SquidStdSessionEventArgs<TState>>? OnSessionCreated;

    /// <inheritdoc />
    public event EventHandler<SquidStdSessionEventArgs<TState>>? OnSessionRemoved;

    /// <inheritdoc />
    public event EventHandler<SquidStdSessionDataEventArgs<TState>>? OnSessionData;
}
