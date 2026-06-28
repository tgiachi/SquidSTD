using SquidStd.Actors.Data;
using SquidStd.Actors.Types;
using SquidStd.Tests.Actors.Support;

namespace SquidStd.Tests.Actors;

public class ActorErrorTests
{
    [Fact]
    public async Task Isolate_KeepsProcessingAfterHandlerThrows()
    {
        await using var actor = new ProbeActor(); // default Isolate

        await actor.TellAsync(new Append("a"));
        await actor.TellAsync(new Boom());
        await actor.TellAsync(new Append("b"));

        var log = await actor.AskAsync<GetLog, string>(new GetLog());

        Assert.Equal("a,b", log);
        Assert.Contains("boom", actor.Errors);
    }

    [Fact]
    public async Task AskAsync_PropagatesHandlerException()
    {
        await using var actor = new ProbeActor();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            actor.AskAsync<FailingRequest, string>(new FailingRequest())
        );

        Assert.Equal("ask-boom", ex.Message);
    }

    [Fact]
    public async Task StopOnError_StopsProcessingAfterThrow()
    {
        await using var actor = new ProbeActor(
            new ActorOptions { ErrorPolicy = ActorErrorPolicy.StopOnError }
        );

        await actor.TellAsync(new Boom()); // faults the mailbox
        await Task.Delay(50);

        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.AskAsync<GetLog, string>(new GetLog())
        );
    }
}
