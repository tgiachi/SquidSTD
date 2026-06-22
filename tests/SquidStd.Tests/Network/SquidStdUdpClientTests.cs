using System.Net;
using SquidStd.Network.Client;

namespace SquidStd.Tests.Network;

public class SquidStdUdpClientTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task CloseAsync_RaisesDisconnectedOnceAndMarksDisconnected()
    {
        var client = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        var disconnects = 0;
        client.OnDisconnected += (_, _) => Interlocked.Increment(ref disconnects);
        await client.StartAsync(CancellationToken.None);

        await client.CloseAsync(CancellationToken.None);
        await client.CloseAsync(CancellationToken.None);

        Assert.False(client.IsConnected);
        Assert.Equal(1, disconnects);
    }

    [Fact]
    public void Constructor_AssignsUniqueSessionIds()
    {
        using var first = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        using var second = new SquidStdUdpClient(new(IPAddress.Loopback, 0));

        Assert.NotEqual(first.SessionId, second.SessionId);
    }

    [Fact]
    public void Constructor_BindsLocalEndPoint()
    {
        using var client = new SquidStdUdpClient(new(IPAddress.Loopback, 0));

        var local = Assert.IsType<IPEndPoint>(client.LocalEndPoint);
        Assert.NotEqual(0, local.Port);
        Assert.True(client.IsConnected);
    }

    [Fact]
    public void RemoteEndPoint_ReflectsConfiguredDefault()
    {
        var remote = new IPEndPoint(IPAddress.Loopback, 9999);

        using var client = new SquidStdUdpClient(new(IPAddress.Loopback, 0), remote);

        Assert.Equal(remote, client.RemoteEndPoint);
    }

    [Fact]
    public async Task SendAsync_UsesConfiguredDefaultRemote()
    {
        await using var receiver = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.OnDataReceived += (_, e) => received.TrySetResult(e.Data.ToArray());
        await receiver.StartAsync(CancellationToken.None);

        var receiverPort = ((IPEndPoint)receiver.LocalEndPoint!).Port;

        await using var sender = new SquidStdUdpClient(
            new(IPAddress.Loopback, 0),
            new(IPAddress.Loopback, receiverPort)
        );
        await sender.StartAsync(CancellationToken.None);
        await sender.SendAsync(new byte[] { 9, 8, 7 }, CancellationToken.None);

        var payload = await received.Task.WaitAsync(Timeout);
        Assert.Equal([9, 8, 7], payload);
    }

    [Fact]
    public async Task SendAsync_WithoutDefaultRemote_Throws()
    {
        await using var client = new SquidStdUdpClient(new(IPAddress.Loopback, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await client.SendAsync(new byte[] { 1 }, CancellationToken.None)
        );
    }

    [Fact]
    public async Task SendToAsync_DeliversDatagramToPeer()
    {
        await using var receiver = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        receiver.OnDataReceived += (_, e) => received.TrySetResult(e.Data.ToArray());
        await receiver.StartAsync(CancellationToken.None);

        var receiverPort = ((IPEndPoint)receiver.LocalEndPoint!).Port;

        await using var sender = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        await sender.StartAsync(CancellationToken.None);
        await sender.SendToAsync(
            new byte[] { 1, 2, 3, 4 },
            new(IPAddress.Loopback, receiverPort),
            CancellationToken.None
        );

        var payload = await received.Task.WaitAsync(Timeout);
        Assert.Equal([1, 2, 3, 4], payload);
    }

    [Fact]
    public async Task StartAsync_RaisesConnected()
    {
        await using var client = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        var connected = false;
        client.OnConnected += (_, _) => connected = true;

        await client.StartAsync(CancellationToken.None);

        Assert.True(connected);
    }
}
