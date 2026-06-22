using System.Net;
using SquidStd.Network.Client;
using SquidStd.Network.Server;
using SquidStd.Network.Types.Server;

namespace SquidStd.Tests.Network;

public class SquidStdUdpServerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task BindSingleInterface_HasOneListener()
    {
        await using var server = new SquidStdUdpServer(new(IPAddress.Loopback, 0), false);
        await server.StartAsync(CancellationToken.None);

        Assert.Equal(1, server.ListenerCount);
    }

    [Fact]
    public async Task DefaultBehaviour_EchoesDatagramBackToSender()
    {
        await using var server = new SquidStdUdpServer(new(IPAddress.Loopback, 0), false);
        await server.StartAsync(CancellationToken.None);
        var serverPort = server.Port;

        await using var client = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnDataReceived += (_, e) => received.TrySetResult(e.Data.ToArray());
        await client.StartAsync(CancellationToken.None);

        var payload = new byte[] { 1, 2, 3, 4, 5 };
        await client.SendToAsync(payload, new(IPAddress.Loopback, serverPort), CancellationToken.None);

        Assert.Equal(payload, await received.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task OnDatagram_CustomResponse_IsReturnedToSender()
    {
        await using var server = new SquidStdUdpServer(new(IPAddress.Loopback, 0), false)
        {
            OnDatagram = (_, _) => new byte[] { 0xFF, 0xFE }
        };
        await server.StartAsync(CancellationToken.None);
        var serverPort = server.Port;

        await using var client = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnDataReceived += (_, e) => received.TrySetResult(e.Data.ToArray());
        await client.StartAsync(CancellationToken.None);

        await client.SendToAsync(new byte[] { 1 }, new(IPAddress.Loopback, serverPort), CancellationToken.None);

        Assert.Equal([0xFF, 0xFE], await received.Task.WaitAsync(Timeout));
    }

    [Fact]
    public void ServerType_IsUdp()
    {
        using var server = new SquidStdUdpServer(new(IPAddress.Loopback, 0), false);

        Assert.Equal(ServerType.UDP, server.ServerType);
    }

    [Fact]
    public async Task StartStop_TogglesIsRunning()
    {
        await using var server = new SquidStdUdpServer(new(IPAddress.Loopback, 0), false);

        Assert.False(server.IsRunning);
        await server.StartAsync(CancellationToken.None);
        Assert.True(server.IsRunning);

        await server.StopAsync(CancellationToken.None);
        Assert.False(server.IsRunning);

        // Start/Stop/Start cycle is supported.
        await server.StartAsync(CancellationToken.None);
        Assert.True(server.IsRunning);
    }

    [Fact]
    public async Task OnDatagramReceived_RaisedForIncomingDatagram()
    {
        await using var server = new SquidStdUdpServer(new(IPAddress.Loopback, 0), false);
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnDatagramReceived += (_, e) => received.TrySetResult(e.Data.ToArray());
        await server.StartAsync(CancellationToken.None);
        var serverPort = server.Port;

        await using var client = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        await client.StartAsync(CancellationToken.None);
        await client.SendToAsync(new byte[] { 1, 2, 3 }, new(IPAddress.Loopback, serverPort), CancellationToken.None);

        Assert.Equal([1, 2, 3], await received.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task SendToAsync_DeliversToEndpointThatWasSeen()
    {
        await using var server = new SquidStdUdpServer(new(IPAddress.Loopback, 0), false)
        {
            OnDatagram = static (_, _) => ReadOnlyMemory<byte>.Empty // suppress default echo for this test
        };
        var senderEndpoint = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnDatagramReceived += (_, e) => senderEndpoint.TrySetResult(e.RemoteEndPoint);
        await server.StartAsync(CancellationToken.None);
        var serverPort = server.Port;

        await using var client = new SquidStdUdpClient(new(IPAddress.Loopback, 0));
        var clientReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnDataReceived += (_, e) => clientReceived.TrySetResult(e.Data.ToArray());
        await client.StartAsync(CancellationToken.None);

        // Make the server "see" the client endpoint first.
        await client.SendToAsync(new byte[] { 0 }, new(IPAddress.Loopback, serverPort), CancellationToken.None);
        var clientEndpoint = await senderEndpoint.Task.WaitAsync(Timeout);

        await server.SendToAsync(clientEndpoint, new byte[] { 9, 9 }, CancellationToken.None);

        Assert.Equal([9, 9], await clientReceived.Task.WaitAsync(Timeout));
    }
}
