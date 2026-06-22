using System.Net;
using SquidStd.Network.Client;
using SquidStd.Network.Server;
using SquidStd.Network.Sessions;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Network;

public class UdpSessionManagerTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static SquidStdUdpServer NewServer()
        => new(new IPEndPoint(IPAddress.Loopback, 0), bindAllInterfaces: false);

    private static UdpSessionManager<string> NewManager(SquidStdUdpServer server, FakeTimeProvider time)
        => new(
            server,
            connection => $"state-{connection.SessionId}",
            idleTimeout: TimeSpan.FromSeconds(30),
            sweepInterval: TimeSpan.FromSeconds(10),
            timeProvider: time);

    private static IPEndPoint Peer(int port)
        => new(IPAddress.Loopback, port);

    [Fact]
    public void FirstDatagram_CreatesSessionAndRaisesCreatedThenData()
    {
        using var server = NewServer();
        var time = new FakeTimeProvider(Start);
        using var manager = NewManager(server, time);

        Session<string>? created = null;
        var dataCount = 0;
        manager.OnSessionCreated += (_, e) => created = e.Session;
        manager.OnSessionData += (_, _) => dataCount++;

        manager.HandleDatagram(Peer(5000), new byte[] { 1 });

        Assert.Equal(1, manager.Count);
        Assert.NotNull(created);
        Assert.Equal("state-1", created!.State);
        Assert.Equal(1, dataCount);
        Assert.True(manager.TryGetSession(Peer(5000), out var byEp));
        Assert.True(manager.TryGetSession(created.SessionId, out var byId));
        Assert.Same(byEp, byId);
    }

    [Fact]
    public void SecondDatagram_SameEndpoint_RaisesDataOnly()
    {
        using var server = NewServer();
        var time = new FakeTimeProvider(Start);
        using var manager = NewManager(server, time);

        var createdCount = 0;
        var dataCount = 0;
        manager.OnSessionCreated += (_, _) => createdCount++;
        manager.OnSessionData += (_, _) => dataCount++;

        manager.HandleDatagram(Peer(5000), new byte[] { 1 });
        manager.HandleDatagram(Peer(5000), new byte[] { 2 });

        Assert.Equal(1, createdCount);
        Assert.Equal(2, dataCount);
        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void SweepExpiredSessions_RemovesIdleSessions()
    {
        using var server = NewServer();
        var time = new FakeTimeProvider(Start);
        using var manager = NewManager(server, time);

        Session<string>? removed = null;
        manager.OnSessionRemoved += (_, e) => removed = e.Session;

        manager.HandleDatagram(Peer(5000), new byte[] { 1 });
        time.Advance(TimeSpan.FromSeconds(31));
        manager.SweepExpiredSessions();

        Assert.Equal(0, manager.Count);
        Assert.NotNull(removed);
        Assert.False(manager.TryGetSession(Peer(5000), out _));
    }

    [Fact]
    public void SweepExpiredSessions_KeepsRecentlyActiveSessions()
    {
        using var server = NewServer();
        var time = new FakeTimeProvider(Start);
        using var manager = NewManager(server, time);

        manager.HandleDatagram(Peer(5000), new byte[] { 1 });
        time.Advance(TimeSpan.FromSeconds(20));
        manager.HandleDatagram(Peer(5000), new byte[] { 2 }); // refresh activity
        time.Advance(TimeSpan.FromSeconds(20));               // 20s since last activity < 30s
        manager.SweepExpiredSessions();

        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public async Task DisconnectAsync_ByEndpoint_RemovesSessionOnce()
    {
        using var server = NewServer();
        var time = new FakeTimeProvider(Start);
        using var manager = NewManager(server, time);

        var removals = 0;
        manager.OnSessionRemoved += (_, _) => removals++;
        manager.HandleDatagram(Peer(5000), new byte[] { 1 });

        await manager.DisconnectAsync(Peer(5000), CancellationToken.None);
        await manager.DisconnectAsync(Peer(5000), CancellationToken.None); // idempotent

        Assert.Equal(0, manager.Count);
        Assert.Equal(1, removals);
    }

    [Fact]
    public async Task DisconnectAsync_UnknownId_IsNoOp()
    {
        using var server = NewServer();
        var time = new FakeTimeProvider(Start);
        using var manager = NewManager(server, time);

        await manager.DisconnectAsync(999, CancellationToken.None);
        // No exception = pass.
    }

    [Fact]
    public void Dispose_ClearsSessionsAndIsIdempotent()
    {
        using var server = NewServer();
        var time = new FakeTimeProvider(Start);
        var manager = NewManager(server, time);
        manager.HandleDatagram(Peer(5000), new byte[] { 1 });

        manager.Dispose();
        manager.Dispose();

        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public async Task Integration_DatagramCreatesSessionAndManagerCanReply()
    {
        var timeout = TimeSpan.FromSeconds(5);

        await using var server = new SquidStdUdpServer(new IPEndPoint(IPAddress.Loopback, 0), bindAllInterfaces: false);
        using var manager = new UdpSessionManager<string>(server, c => $"state-{c.SessionId}");

        var created = new TaskCompletionSource<Session<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var data = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.OnSessionCreated += (_, e) => created.TrySetResult(e.Session);
        manager.OnSessionData += (_, e) => data.TrySetResult(e.Data.ToArray());

        await server.StartAsync(CancellationToken.None);
        var serverPort = server.Port;

        await using var client = new SquidStdUdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var clientReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnDataReceived += (_, e) => clientReceived.TrySetResult(e.Data.ToArray());
        await client.StartAsync(CancellationToken.None);

        await client.SendToAsync(new byte[] { 1, 2, 3 }, new IPEndPoint(IPAddress.Loopback, serverPort), CancellationToken.None);

        var session = await created.Task.WaitAsync(timeout);
        Assert.Equal([1, 2, 3], await data.Task.WaitAsync(timeout));
        Assert.Equal(1, manager.Count);

        await session.SendAsync(new byte[] { 4, 5 }, CancellationToken.None);
        Assert.Equal([4, 5], await clientReceived.Task.WaitAsync(timeout));
    }
}
