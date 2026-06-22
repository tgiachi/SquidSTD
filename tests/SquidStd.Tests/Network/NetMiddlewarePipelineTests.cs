using SquidStd.Network.Pipeline;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Network;

public class NetMiddlewarePipelineTests
{
    [Fact]
    public void AddMiddleware_RegistersMiddleware()
    {
        var pipeline = new NetMiddlewarePipeline();
        pipeline.AddMiddleware(new DroppingMiddleware());

        Assert.True(pipeline.ContainsMiddleware<DroppingMiddleware>());
    }

    [Fact]
    public void ContainsMiddleware_ReflectsRegistration()
    {
        var pipeline = new NetMiddlewarePipeline([new AppendingMiddleware(1)]);

        Assert.True(pipeline.ContainsMiddleware<AppendingMiddleware>());
        Assert.False(pipeline.ContainsMiddleware<DroppingMiddleware>());
    }

    [Fact]
    public async Task ExecuteAsync_CancelledToken_Throws()
    {
        var pipeline = new NetMiddlewarePipeline([new AppendingMiddleware(1)]);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await pipeline.ExecuteAsync(null, new byte[] { 1 }, cts.Token)
        );
    }

    [Fact]
    public async Task ExecuteAsync_DroppingMiddleware_ShortCircuits()
    {
        var pipeline = new NetMiddlewarePipeline([new DroppingMiddleware(), new AppendingMiddleware(0x02)]);

        var result = await pipeline.ExecuteAsync(null, new byte[] { 9 }, CancellationToken.None);

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public async Task ExecuteAsync_NoMiddleware_ReturnsInputUnchanged()
    {
        var pipeline = new NetMiddlewarePipeline();

        var result = await pipeline.ExecuteAsync(null, new byte[] { 1, 2 }, CancellationToken.None);

        Assert.Equal([1, 2], result.ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_RunsMiddlewareInRegistrationOrder()
    {
        var pipeline = new NetMiddlewarePipeline([new AppendingMiddleware(0x01), new AppendingMiddleware(0x02)]);

        var result = await pipeline.ExecuteAsync(null, new byte[] { 0x00 }, CancellationToken.None);

        Assert.Equal([0x00, 0x01, 0x02], result.ToArray());
    }

    [Fact]
    public async Task ExecuteSendAsync_RunsMiddlewareInRegistrationOrder()
    {
        var pipeline = new NetMiddlewarePipeline([new AppendingMiddleware(0xAA), new AppendingMiddleware(0xBB)]);

        var result = await pipeline.ExecuteSendAsync(null, new byte[] { 0 }, CancellationToken.None);

        Assert.Equal([0x00, 0xAA, 0xBB], result.ToArray());
    }

    [Fact]
    public void RemoveMiddleware_RemovesRegisteredAndReportsResult()
    {
        var pipeline = new NetMiddlewarePipeline([new DroppingMiddleware()]);

        Assert.True(pipeline.RemoveMiddleware<DroppingMiddleware>());
        Assert.False(pipeline.ContainsMiddleware<DroppingMiddleware>());
        Assert.False(pipeline.RemoveMiddleware<DroppingMiddleware>());
    }
}
