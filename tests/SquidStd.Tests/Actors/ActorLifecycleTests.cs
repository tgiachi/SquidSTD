using SquidStd.Tests.Actors.Support;

namespace SquidStd.Tests.Actors;

public class ActorLifecycleTests
{
    [Fact]
    public async Task DisposeAsync_FaultsPendingAskRequests()
    {
        var actor = new ProbeActor();
        await actor.TellAsync(new HoldUntilCancelled()); // in-flight, honors cancellation

        var ask = actor.AskAsync<GetLog, string>(new GetLog()); // queued behind the hold
        await Task.Delay(50);

        await actor.DisposeAsync(); // cancels the hold; the queued request never runs

        await Assert.ThrowsAsync<ObjectDisposedException>(() => ask);
    }

    [Fact]
    public async Task TellAsync_AfterDispose_Throws()
    {
        var actor = new ProbeActor();
        await actor.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await actor.TellAsync(new Append("x"))
        );
    }

    [Fact]
    public async Task DisposeAsync_CalledConcurrentlyTwice_IsIdempotent()
    {
        var actor = new ProbeActor();

        // The dispose guard is claimed atomically, so concurrent disposers must not both run teardown.
        var first = actor.DisposeAsync();
        var second = actor.DisposeAsync();

        await first;
        await second;
    }
}
