using DryIoc;
using SquidStd.Core.Interfaces.Threading;
using SquidStd.Services.Core.Extensions;
using SquidStd.Services.Core.Services;
using SquidStd.Services.Core.Services.Internal;

namespace SquidStd.Tests.Services.Core;

public class MainThreadDispatcherServiceTests
{
    [Fact]
    public void DrainPending_BudgetExceededAfterFirstCallback_DefersRest()
    {
        IMainThreadDispatcher dispatcher = new MainThreadDispatcherService();
        var calls = new List<int>();
        dispatcher.Post(
            () =>
            {
                calls.Add(1);
                Thread.Sleep(5);
            }
        );
        dispatcher.Post(() => calls.Add(2));

        var firstDrain = dispatcher.DrainPending(1);
        var secondDrain = dispatcher.DrainPending();

        Assert.Equal(1, firstDrain);
        Assert.Equal(1, secondDrain);
        Assert.Equal([1, 2], calls);
    }

    [Fact]
    public void DrainPending_CallbackPostsAnother_DefersToNextDrain()
    {
        IMainThreadDispatcher dispatcher = new MainThreadDispatcherService();
        var calls = new List<int>();
        dispatcher.Post(
            () =>
            {
                calls.Add(1);
                dispatcher.Post(() => calls.Add(2));
            }
        );

        var firstDrain = dispatcher.DrainPending();
        var secondDrain = dispatcher.DrainPending();

        Assert.Equal(1, firstDrain);
        Assert.Equal(1, secondDrain);
        Assert.Equal([1, 2], calls);
    }

    [Fact]
    public void DrainPending_CallbackThrows_ContinuesWithNext()
    {
        IMainThreadDispatcher dispatcher = new MainThreadDispatcherService();
        var calls = 0;
        dispatcher.Post(() => throw new InvalidOperationException("boom"));
        dispatcher.Post(() => calls++);

        var executed = dispatcher.DrainPending();

        Assert.Equal(2, executed);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void DrainPending_EmptyQueue_ReturnsZero()
    {
        IMainThreadDispatcher dispatcher = new MainThreadDispatcherService();

        var executed = dispatcher.DrainPending();

        Assert.Equal(0, executed);
        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public void DrainPending_NoBudget_DrainsAll()
    {
        IMainThreadDispatcher dispatcher = new MainThreadDispatcherService();
        var calls = new List<int>();
        dispatcher.Post(() => calls.Add(1));
        dispatcher.Post(() => calls.Add(2));

        var executed = dispatcher.DrainPending();

        Assert.Equal(2, executed);
        Assert.Equal([1, 2], calls);
        Assert.Equal(0, dispatcher.PendingCount);
    }

    [Fact]
    public void Post_NullAction_ThrowsArgumentNullException()
    {
        IMainThreadDispatcher dispatcher = new MainThreadDispatcherService();

        Assert.Throws<ArgumentNullException>(() => dispatcher.Post(null!));
    }

    [Fact]
    public void RegisterCoreServices_RegistersMainThreadDispatcher()
    {
        using var container = new Container();

        container.RegisterCoreServices();

        Assert.IsType<MainThreadDispatcherService>(container.Resolve<IMainThreadDispatcher>());
    }

    [Fact]
    public void SynchronizationContext_Post_EnqueuesIntoDispatcher()
    {
        IMainThreadDispatcher dispatcher = new MainThreadDispatcherService();
        var context = new MainThreadSynchronizationContext(dispatcher);
        var stateValue = string.Empty;

        context.Post(state => stateValue = (string)state!, "posted");
        var executed = dispatcher.DrainPending();

        Assert.Equal(1, executed);
        Assert.Equal("posted", stateValue);
    }

    [Fact]
    public void SynchronizationContext_Send_InvokesDelegateSynchronously()
    {
        IMainThreadDispatcher dispatcher = new MainThreadDispatcherService();
        var context = new MainThreadSynchronizationContext(dispatcher);
        var stateValue = string.Empty;

        context.Send(state => stateValue = (string)state!, "sent");

        Assert.Equal("sent", stateValue);
        Assert.Equal(0, dispatcher.PendingCount);
    }
}
