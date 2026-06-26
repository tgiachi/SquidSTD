using System.Net;
using SquidStd.Network.Client;
using SquidStd.Network.Data;
using SquidStd.Network.Server;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Network;

public class TransportCodecTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Codec_RoundTripsClientToServer()
    {
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var server = new SquidTcpServer(
            new(IPAddress.Loopback, 0),
            connectionPipelineFactory: () => new ConnectionPipeline(new CountingXorCodec(7))
        );
        server.OnDataReceived += (_, e) => received.TrySetResult(e.Data.ToArray());
        await server.StartAsync(CancellationToken.None);

        await using var client = await SquidStdTcpClient.ConnectAsync(
            new(IPAddress.Loopback, server.Port),
            codec: new CountingXorCodec(7)
        );

        var payload = new byte[] { 1, 2, 3, 4, 5 };
        await client.SendAsync(payload, CancellationToken.None);

        Assert.Equal(payload, await received.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task NoCodecNoFactory_PassesBytesUnchanged()
    {
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var server = new SquidTcpServer(new(IPAddress.Loopback, 0));
        server.OnDataReceived += (_, e) => received.TrySetResult(e.Data.ToArray());
        await server.StartAsync(CancellationToken.None);

        await using var client = await SquidStdTcpClient.ConnectAsync(new(IPAddress.Loopback, server.Port));

        var payload = new byte[] { 42, 43, 44 };
        await client.SendAsync(payload, CancellationToken.None);

        Assert.Equal(payload, await received.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task Codec_ConcurrentSends_PreserveKeystreamIntegrity()
    {
        const int messageCount = 40;
        const int payloadSize = 16;
        var concurrentTimeout = TimeSpan.FromSeconds(10);

        var frames = new System.Collections.Concurrent.ConcurrentBag<byte[]>();
        using var done = new CountdownEvent(messageCount);

        await using var server = new SquidTcpServer(
            new(IPAddress.Loopback, 0),
            connectionPipelineFactory: () => new ConnectionPipeline(new CountingXorCodec(5), null, new LengthPrefixFramer())
        );
        server.OnDataReceived += (_, e) =>
        {
            frames.Add(e.Data.ToArray());
            done.Signal();
        };
        await server.StartAsync(CancellationToken.None);

        await using var client = await SquidStdTcpClient.ConnectAsync(
            new(IPAddress.Loopback, server.Port),
            codec: new CountingXorCodec(5)
        );

        await Parallel.ForEachAsync(
            Enumerable.Range(0, messageCount),
            async (id, ct) =>
            {
                var frame = new byte[1 + payloadSize];
                frame[0] = payloadSize;

                for (var i = 1; i < frame.Length; i++)
                {
                    frame[i] = (byte)id;
                }

                await client.SendAsync(frame, ct);
            }
        );

        Assert.True(done.Wait(concurrentTimeout));
        Assert.Equal(messageCount, frames.Count);

        var ids = new HashSet<int>();

        foreach (var frame in frames)
        {
            Assert.Equal(1 + payloadSize, frame.Length);
            Assert.Equal(payloadSize, frame[0]);
            var id = frame[1];

            for (var i = 1; i < frame.Length; i++)
            {
                Assert.Equal(id, frame[i]);
            }

            ids.Add(id);
        }

        Assert.Equal(messageCount, ids.Count);
    }

    [Fact]
    public async Task Codec_WithFramer_EmitsDecodedFrames()
    {
        var frames = new System.Collections.Concurrent.BlockingCollection<byte[]>();

        await using var server = new SquidTcpServer(
            new(IPAddress.Loopback, 0),
            connectionPipelineFactory: () => new ConnectionPipeline(new CountingXorCodec(2), null, new LengthPrefixFramer())
        );
        server.OnDataReceived += (_, e) => frames.Add(e.Data.ToArray());
        await server.StartAsync(CancellationToken.None);

        await using var client = await SquidStdTcpClient.ConnectAsync(
            new(IPAddress.Loopback, server.Port),
            codec: new CountingXorCodec(2)
        );

        // Three length-prefixed messages in a single send: [len=2][AA BB] [len=1][CC] [len=3][01 02 03].
        var buffer = new byte[] { 2, 0xAA, 0xBB, 1, 0xCC, 3, 0x01, 0x02, 0x03 };
        await client.SendAsync(buffer, CancellationToken.None);

        Assert.True(frames.TryTake(out var f1, Timeout));
        Assert.Equal(new byte[] { 2, 0xAA, 0xBB }, f1);
        Assert.True(frames.TryTake(out var f2, Timeout));
        Assert.Equal(new byte[] { 1, 0xCC }, f2);
        Assert.True(frames.TryTake(out var f3, Timeout));
        Assert.Equal(new byte[] { 3, 0x01, 0x02, 0x03 }, f3);
    }

    [Fact]
    public async Task Codec_IsolatesStatePerConnection()
    {
        var payload = new byte[] { 9, 8, 7, 6 };
        var inbox = new System.Collections.Concurrent.BlockingCollection<byte[]>();

        await using var server = new SquidTcpServer(
            new(IPAddress.Loopback, 0),
            connectionPipelineFactory: () => new ConnectionPipeline(new CountingXorCodec(3))
        );
        server.OnDataReceived += (_, e) => inbox.Add(e.Data.ToArray());
        await server.StartAsync(CancellationToken.None);

        // Two sequential connections. Each accepted connection must get a fresh codec (position 0);
        // a shared codec would leave the second connection's decode position offset and corrupt the bytes.
        for (var i = 0; i < 2; i++)
        {
            await using var client = await SquidStdTcpClient.ConnectAsync(
                new(IPAddress.Loopback, server.Port),
                codec: new CountingXorCodec(3)
            );
            await client.SendAsync(payload, CancellationToken.None);

            Assert.True(inbox.TryTake(out var got, Timeout));
            Assert.Equal(payload, got);
        }
    }

    [Fact]
    public async Task Codec_SwapsAtomicallyMidConnection()
    {
        var msg1 = new byte[] { 1, 1, 1 };
        var msg2 = new byte[] { 2, 2, 2 };
        var first = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var server = new SquidTcpServer(
            new(IPAddress.Loopback, 0),
            connectionPipelineFactory: () => new ConnectionPipeline(new CountingXorCodec(10))
        );
        server.OnDataReceived += (_, e) =>
        {
            if (first.Task.IsCompleted)
            {
                second.TrySetResult(e.Data.ToArray());
            }
            else
            {
                e.Client.SwapCodec(new CountingXorCodec(20));
                first.TrySetResult(e.Data.ToArray());
            }
        };
        await server.StartAsync(CancellationToken.None);

        await using var client = await SquidStdTcpClient.ConnectAsync(
            new(IPAddress.Loopback, server.Port),
            codec: new CountingXorCodec(10)
        );

        await client.SendAsync(msg1, CancellationToken.None);
        Assert.Equal(msg1, await first.Task.WaitAsync(Timeout));

        client.SwapCodec(new CountingXorCodec(20));
        await client.SendAsync(msg2, CancellationToken.None);
        Assert.Equal(msg2, await second.Task.WaitAsync(Timeout));
    }
}
