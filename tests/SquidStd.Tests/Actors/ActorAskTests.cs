using SquidStd.Actors;
using SquidStd.Tests.Actors.Support;

namespace SquidStd.Tests.Actors;

public class ActorAskTests
{
    [Fact]
    public async Task Reply_CompletesTheCompletionTask()
    {
        var request = new SampleRequest();

        request.Reply(42);

        Assert.True(request.Completion.IsCompletedSuccessfully);
        Assert.Equal(42, await request.Completion);
    }

    [Fact]
    public async Task Fail_FaultsTheCompletionTask()
    {
        var request = new SampleRequest();

        request.Fail(new InvalidOperationException("nope"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => request.Completion);
        Assert.Equal("nope", ex.Message);
    }

    [Fact]
    public async Task AskAsync_ReturnsReplyFromHandler()
    {
        await using var actor = new ProbeActor();
        await actor.TellAsync(new Append("x"));
        await actor.TellAsync(new Append("y"));

        var log = await actor.AskAsync<GetLog, string>(new GetLog());

        Assert.Equal("x,y", log);
    }

    [Fact]
    public async Task AskAsync_WhenTokenCancelled_Faults()
    {
        var gate = new TaskCompletionSource();
        await using var actor = new ProbeActor();
        await actor.TellAsync(new Hold(gate)); // occupy the consumer so the request waits

        using var cts = new CancellationTokenSource();
        var ask = actor.AskAsync<GetLog, string>(new GetLog(), cts.Token);
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => ask);
        gate.SetResult();
    }

    private sealed record SampleRequest : ActorRequest<int>;
}
