using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using SquidStd.Network.Client;
using SquidStd.Network.Interfaces.Framing;

namespace SquidStd.Tests.Network;

public sealed class TcpMaxFrameLengthTests
{
    // 4-byte big-endian length prefix framer.
    private sealed class LengthPrefixFramer : INetFramer
    {
        public bool TryReadFrame(ReadOnlySpan<byte> buffer, out int frameLength)
        {
            frameLength = 0;

            if (buffer.Length < 4)
            {
                return false;
            }

            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(buffer);
            var total = 4 + payloadLength;

            if (buffer.Length < total)
            {
                return false;
            }

            frameLength = total;

            return true;
        }
    }

    [Fact]
    public async Task OversizedDeclaredFrame_ClosesConnection()
    {
        var (server, client) = await ConnectedPairAsync(maxFrameLength: 1024);

        try
        {
            // Declare a 10 MiB payload (way over the 1 KiB cap) then dribble bytes; the receiver must
            // close instead of growing its pending buffer toward 10 MiB.
            var header = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(header, 10 * 1024 * 1024);
            await server.SendAsync(header, CancellationToken.None);
            await server.SendAsync(new byte[4096], CancellationToken.None);

            var closed = await WaitUntilAsync(() => !client.IsConnected, TimeSpan.FromSeconds(5));
            Assert.True(closed);
        }
        finally
        {
            await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task FrameAtTheLimit_IsDelivered()
    {
        var (server, client) = await ConnectedPairAsync(maxFrameLength: 1024);
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnDataReceived += (_, e) => received.TrySetResult(e.Data.ToArray());

        try
        {
            var payload = new byte[1000];
            var frame = new byte[4 + payload.Length];
            BinaryPrimitives.WriteInt32BigEndian(frame, payload.Length);
            payload.CopyTo(frame, 4);
            await server.SendAsync(frame, CancellationToken.None);

            var got = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(4 + payload.Length, got.Length);
            Assert.True(client.IsConnected);
        }
        finally
        {
            await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }

    private static async Task<(SquidStdTcpClient Server, SquidStdTcpClient Client)> ConnectedPairAsync(int maxFrameLength)
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var port = ((IPEndPoint)listener.LocalEndPoint!).Port;

        var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var connectTask = clientSocket.ConnectAsync(IPAddress.Loopback, port);
        var serverSocket = await listener.AcceptAsync();
        await connectTask;
        listener.Dispose();

        // "server" side here is just the sending end; the receiving end (client) enforces the cap.
        var server = new SquidStdTcpClient(serverSocket);
        var client = new SquidStdTcpClient(clientSocket, middlewares: null, framer: new LengthPrefixFramer(),
            codec: null, maxFrameLength: maxFrameLength);

        await server.StartAsync(CancellationToken.None);
        await client.StartAsync(CancellationToken.None);

        return (server, client);
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(25);
        }

        return condition();
    }
}
