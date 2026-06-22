using System.Net;
using SquidStd.Network.Client;
using SquidStd.Network.Data.Events;
using SquidStd.Network.Server;
using SquidStd.Network.Sessions;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Network;

public class SessionManagerTests
{
    [Fact]
    public async Task BroadcastAsync_SendsToAll_IsolatesFailures()
    {
        using var server = NewServer();
        using var manager = NewManager(server);

        var good = new FakeNetworkConnection(1);
        var faulty = new FakeNetworkConnection(2)
        {
            SendCallback = _ => throw new InvalidOperationException("boom")
        };
        manager.HandleConnected(good);
        manager.HandleConnected(faulty);

        await manager.BroadcastAsync(new byte[] { 0xAB }, CancellationToken.None);

        Assert.Single(good.SentPayloads);
        Assert.Equal([0xAB], good.SentPayloads[0]);
    }

    [Fact]
    public async Task DisconnectAsync_KnownSession_ClosesConnection()
    {
        using var server = NewServer();
        using var manager = NewManager(server);
        var connection = new FakeNetworkConnection(6);
        manager.HandleConnected(connection);

        await manager.DisconnectAsync(6, CancellationToken.None);

        Assert.Equal(1, connection.CloseCount);
    }

    [Fact]
    public async Task DisconnectAsync_UnknownSession_IsNoOp()
    {
        using var server = NewServer();
        using var manager = NewManager(server);

        await manager.DisconnectAsync(999, CancellationToken.None);

        // No exception = pass.
    }

    [Fact]
    public void Dispose_ClearsSessionsAndIsIdempotent()
    {
        using var server = NewServer();
        var manager = NewManager(server);
        manager.HandleConnected(new FakeNetworkConnection(1));

        manager.Dispose();
        manager.Dispose(); // idempotent, no throw

        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void HandleConnected_CreatesSessionAndRaisesEvent()
    {
        using var server = NewServer();
        using var manager = NewManager(server);
        Session<string>? created = null;
        manager.OnSessionCreated += (_, e) => created = e.Session;

        var connection = new FakeNetworkConnection(7);
        manager.HandleConnected(connection);

        Assert.Equal(1, manager.Count);
        Assert.NotNull(created);
        Assert.Equal(7, created!.SessionId);
        Assert.Equal("state-7", created.State);
        Assert.Same(connection, created.Connection);
    }

    [Fact]
    public void HandleData_KnownSession_RaisesData()
    {
        using var server = NewServer();
        using var manager = NewManager(server);
        var connection = new FakeNetworkConnection(5);
        manager.HandleConnected(connection);

        SquidStdSessionDataEventArgs<string>? received = null;
        manager.OnSessionData += (_, e) => received = e;

        manager.HandleData(connection, new byte[] { 1, 2 });

        Assert.NotNull(received);
        Assert.Equal(5, received!.Session.SessionId);
        Assert.Equal([1, 2], received.Data.ToArray());
    }

    [Fact]
    public void HandleData_UnknownSession_DoesNotRaise()
    {
        using var server = NewServer();
        using var manager = NewManager(server);
        var raised = false;
        manager.OnSessionData += (_, _) => raised = true;

        manager.HandleData(new FakeNetworkConnection(123), new byte[] { 1 });

        Assert.False(raised);
    }

    [Fact]
    public void HandleDisconnected_RemovesSessionAndRaisesOnce()
    {
        using var server = NewServer();
        using var manager = NewManager(server);
        var connection = new FakeNetworkConnection(8);
        manager.HandleConnected(connection);

        var removals = 0;
        manager.OnSessionRemoved += (_, _) => removals++;

        manager.HandleDisconnected(connection);
        manager.HandleDisconnected(connection); // idempotent

        Assert.Equal(0, manager.Count);
        Assert.False(manager.TryGetSession(8, out _));
        Assert.Equal(1, removals);
    }

    [Fact]
    public async Task Integration_LifecycleOverLoopback()
    {
        var timeout = TimeSpan.FromSeconds(5);

        await using var server = new SquidTcpServer(new(IPAddress.Loopback, 0));
        using var manager = NewManager(server);

        var created = new TaskCompletionSource<Session<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dataReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var removed = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.OnSessionCreated += (_, e) => created.TrySetResult(e.Session);
        manager.OnSessionData += (_, e) => dataReceived.TrySetResult(e.Data.ToArray());
        manager.OnSessionRemoved += (_, e) => removed.TrySetResult(e.Session.SessionId);

        await server.StartAsync(CancellationToken.None);
        var port = server.Port;

        var client = await SquidStdTcpClient.ConnectAsync(new(IPAddress.Loopback, port));

        var session = await created.Task.WaitAsync(timeout);
        Assert.Equal(1, manager.Count);

        await client.SendAsync(new byte[] { 10, 20, 30 }, CancellationToken.None);
        Assert.Equal([10, 20, 30], await dataReceived.Task.WaitAsync(timeout));

        await client.DisposeAsync();
        var removedId = await removed.Task.WaitAsync(timeout);

        Assert.Equal(session.SessionId, removedId);
    }

    [Fact]
    public async Task SendAsync_KnownSession_SendsToConnection()
    {
        using var server = NewServer();
        using var manager = NewManager(server);
        var connection = new FakeNetworkConnection(4);
        manager.HandleConnected(connection);

        await manager.SendAsync(4, new byte[] { 7, 8 }, CancellationToken.None);

        Assert.Single(connection.SentPayloads);
        Assert.Equal([7, 8], connection.SentPayloads[0]);
    }

    [Fact]
    public async Task SendAsync_UnknownSession_IsNoOp()
    {
        using var server = NewServer();
        using var manager = NewManager(server);

        await manager.SendAsync(999, new byte[] { 1 }, CancellationToken.None);

        // No exception = pass.
    }

    [Fact]
    public void Sessions_ReturnsSnapshotOfAll()
    {
        using var server = NewServer();
        using var manager = NewManager(server);
        manager.HandleConnected(new FakeNetworkConnection());
        manager.HandleConnected(new FakeNetworkConnection(2));

        Assert.Equal(2, manager.Sessions.Count);
    }

    [Fact]
    public void TryGetSession_HitAndMiss()
    {
        using var server = NewServer();
        using var manager = NewManager(server);
        manager.HandleConnected(new FakeNetworkConnection(3));

        Assert.True(manager.TryGetSession(3, out var session));
        Assert.Equal(3, session!.SessionId);
        Assert.False(manager.TryGetSession(999, out var missing));
        Assert.Null(missing);
    }

    private static SessionManager<string> NewManager(SquidTcpServer server)
        => new(server, connection => $"state-{connection.SessionId}");

    private static SquidTcpServer NewServer()
        => new(new(IPAddress.Loopback, 0));
}
