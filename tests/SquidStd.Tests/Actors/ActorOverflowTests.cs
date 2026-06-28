using SquidStd.Actors.Data;
using SquidStd.Actors.Types;
using SquidStd.Tests.Actors.Support;

namespace SquidStd.Tests.Actors;

public class ActorOverflowTests
{
    [Fact]
    public async Task Wait_BlocksUntilCapacityFrees()
    {
        var gate = new TaskCompletionSource();
        await using var actor = new ProbeActor(
            new ActorOptions { Capacity = 2, OverflowPolicy = ActorOverflowPolicy.Wait }
        );

        await actor.TellAsync(new Hold(gate));  // occupies the consumer (slot 1)
        await actor.TellAsync(new Append("a")); // buffered (slot 2) -> full

        var blocked = actor.TellAsync(new Append("b")).AsTask();
        await Task.Delay(50);
        Assert.False(blocked.IsCompleted);

        gate.SetResult(); // release the consumer
        Assert.True(await blocked);
    }

    [Fact]
    public async Task DropNewest_ReturnsFalseWhenFull()
    {
        var gate = new TaskCompletionSource();
        await using var actor = new ProbeActor(
            new ActorOptions { Capacity = 1, OverflowPolicy = ActorOverflowPolicy.DropNewest }
        );

        await actor.TellAsync(new Hold(gate)); // occupies the only slot

        var accepted = await actor.TellAsync(new Append("dropped"));

        Assert.False(accepted);
        gate.SetResult();
    }

    [Fact]
    public async Task Unbounded_AcceptsEveryMessage()
    {
        await using var actor = new ProbeActor(
            new ActorOptions { OverflowPolicy = ActorOverflowPolicy.Unbounded }
        );

        for (var i = 0; i < 500; i++)
        {
            Assert.True(await actor.TellAsync(new Append(i.ToString())));
        }

        var log = await actor.AskAsync<GetLog, string>(new GetLog());
        Assert.Equal(500, log.Split(",").Length);
    }
}
