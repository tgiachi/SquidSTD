using System.Net;
using System.Net.Sockets;
using Serilog;
using SquidStd.Network.Data.Events;
using SquidStd.Network.Interfaces.Client;

namespace SquidStd.Network.Client;

/// <summary>
///     Connectionless UDP client that binds a local socket and surfaces inbound datagrams through an
///     async receive loop. Datagrams can be sent to any endpoint with <see cref="SendToAsync" />, or to
///     an optional default remote endpoint via <see cref="SendAsync" />. Mirrors the lifecycle surface of
///     <see cref="SquidStdTcpClient" /> (session id, connect/disconnect/data/exception events, and
///     <see cref="INetworkConnection" />). Supports Start once; recreate the instance to listen again.
/// </summary>
public sealed class SquidStdUdpClient : INetworkConnection, IAsyncDisposable, IDisposable
{
    private static long _sessionIdSequence;
    private readonly IPEndPoint? _defaultRemoteEndPoint;
    private readonly CancellationTokenSource _internalCancellationTokenSource = new();
    private readonly ILogger _logger = Log.ForContext<SquidStdUdpClient>();
    private readonly UdpClient _udpClient;
    private int _closed;

    private CancellationTokenRegistration _externalCancellationTokenRegistration;
    private Task? _receiveLoopTask;
    private int _started;

    /// <summary>
    ///     Local endpoint the client is bound to, when available.
    /// </summary>
    public EndPoint? LocalEndPoint
    {
        get
        {
            try
            {
                return _udpClient.Client?.LocalEndPoint;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     Unique session identifier for this client.
    /// </summary>
    public long SessionId { get; }

    /// <summary>
    ///     Default remote endpoint used by <see cref="SendAsync" />, when configured.
    /// </summary>
    public EndPoint? RemoteEndPoint => _defaultRemoteEndPoint;

    /// <summary>
    ///     True while the client is open (not closed).
    /// </summary>
    public bool IsConnected => Volatile.Read(ref _closed) == 0;

    /// <summary>
    ///     Creates a UDP client bound to a local endpoint.
    /// </summary>
    /// <param name="localEndPoint">
    ///     Local endpoint to bind. When <c>null</c>, binds an ephemeral port on <see cref="IPAddress.Any" />.
    /// </param>
    /// <param name="defaultRemoteEndPoint">
    ///     Optional default destination used by <see cref="SendAsync" />. When <c>null</c>, callers must use
    ///     <see cref="SendToAsync" /> with an explicit endpoint.
    /// </param>
    public SquidStdUdpClient(IPEndPoint? localEndPoint = null, IPEndPoint? defaultRemoteEndPoint = null)
    {
        _udpClient = new UdpClient(localEndPoint ?? new IPEndPoint(IPAddress.Any, 0));
        _defaultRemoteEndPoint = defaultRemoteEndPoint;
        SessionId = Interlocked.Increment(ref _sessionIdSequence);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAsync(CancellationToken.None);

        // Drain the receive loop before disposing the resources it relies on.
        if (_receiveLoopTask is not null)
        {
            try
            {
                await _receiveLoopTask;
            }
            catch
            {
                // Loop failures are already surfaced via OnException.
            }
        }

        _internalCancellationTokenSource.Dispose();
        _udpClient.Dispose();
    }

    /// <summary>
    ///     Closes the client and raises <see cref="OnDisconnected" /> once.
    /// </summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

        try
        {
            await _internalCancellationTokenSource.CancelAsync().WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Once close has started, still tear down the socket below.
        }

        _udpClient.Close();
        _externalCancellationTokenRegistration.Dispose();
        RaiseDisconnected();
    }

    /// <summary>
    ///     Sends a datagram to the configured default remote endpoint.
    /// </summary>
    /// <exception cref="InvalidOperationException">No default remote endpoint was configured.</exception>
    public Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (_defaultRemoteEndPoint is null)
        {
            throw new InvalidOperationException(
                "No default remote endpoint configured; use SendToAsync with an explicit endpoint."
            );
        }

        return SendToAsync(payload, _defaultRemoteEndPoint, cancellationToken);
    }

    /// <summary>
    ///     Sends a datagram to an explicit endpoint.
    /// </summary>
    public async Task SendToAsync(ReadOnlyMemory<byte> payload, IPEndPoint endPoint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        if (payload.IsEmpty || !IsConnected)
        {
            return;
        }

        try
        {
            await _udpClient.SendAsync(payload, endPoint, cancellationToken);
        }
        catch (Exception ex)
        {
            RaiseException(ex);
        }
    }

    /// <summary>
    ///     Starts the receive loop and raises <see cref="OnConnected" />.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return Task.CompletedTask;
        }

        if (cancellationToken.CanBeCanceled)
        {
            _externalCancellationTokenRegistration =
                cancellationToken.Register(() => _ = CloseAsync(CancellationToken.None));
        }

        RaiseConnected();
        _receiveLoopTask = Task.Run(ReceiveLoopAsync, CancellationToken.None);

        return Task.CompletedTask;
    }

    private void RaiseConnected()
    {
        _logger.Information(
            "UDP client started. SessionId={SessionId}, LocalEndPoint={LocalEndPoint}",
            SessionId,
            LocalEndPoint
        );
        OnConnected?.Invoke(this, new SquidStdUdpClientEventArgs(this));
    }

    private void RaiseDisconnected()
    {
        _logger.Information(
            "UDP client closed. SessionId={SessionId}, LocalEndPoint={LocalEndPoint}",
            SessionId,
            LocalEndPoint
        );
        OnDisconnected?.Invoke(this, new SquidStdUdpClientEventArgs(this));
    }

    private void RaiseException(Exception exception)
    {
        _logger.Error(exception, "UDP client exception. SessionId={SessionId}", SessionId);
        OnException?.Invoke(this, new SquidStdUdpExceptionEventArgs(exception, this));
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (!_internalCancellationTokenSource.IsCancellationRequested && IsConnected)
            {
                var result = await _udpClient.ReceiveAsync(_internalCancellationTokenSource.Token);

                // UdpReceiveResult.Buffer is a fresh array per receive, so it is safe to hand off.
                OnDataReceived?.Invoke(
                    this,
                    new SquidStdUdpDataReceivedEventArgs(this, result.RemoteEndPoint, result.Buffer)
                );
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during controlled shutdown.
        }
        catch (ObjectDisposedException)
        {
            // Socket closed during shutdown.
        }
        catch (SocketException) when (_internalCancellationTokenSource.IsCancellationRequested)
        {
            // Socket faulted during shutdown.
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "UDP receive loop failed for session {SessionId}", SessionId);
            RaiseException(ex);
        }
        finally
        {
            await CloseAsync(CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public void Dispose() // Sync-over-async: best effort. Prefer DisposeAsync.
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Raised when the client starts and the receive loop begins.
    /// </summary>
    public event EventHandler<SquidStdUdpClientEventArgs>? OnConnected;

    /// <summary>
    ///     Raised when the client is closed.
    /// </summary>
    public event EventHandler<SquidStdUdpClientEventArgs>? OnDisconnected;

    /// <summary>
    ///     Raised once per received datagram.
    /// </summary>
    public event EventHandler<SquidStdUdpDataReceivedEventArgs>? OnDataReceived;

    /// <summary>
    ///     Raised when the receive/send paths throw an unexpected exception.
    /// </summary>
    public event EventHandler<SquidStdUdpExceptionEventArgs>? OnException;
}
