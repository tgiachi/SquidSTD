using SquidStd.Core.Interfaces.Events;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Services.Core;

public class EventBusServiceTests
{
    private sealed record TestEvent(string Payload) : IEvent;

    private sealed class SyncListener : ISyncEventListener<TestEvent>
    {
        private readonly List<string> _calls;
        private readonly string _name;

        public TestEvent? LastEvent { get; private set; }

        public SyncListener(string name, List<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        public void Handle(TestEvent eventData)
        {
            LastEvent = eventData;
            _calls.Add($"{_name}:{eventData.Payload}");
        }
    }

    private sealed class ThrowingSyncListener : ISyncEventListener<TestEvent>
    {
        private readonly InvalidOperationException _exception;

        public ThrowingSyncListener(InvalidOperationException exception)
        {
            _exception = exception;
        }

        public void Handle(TestEvent eventData)
            => throw _exception;
    }

    private sealed class AsyncListener : IAsyncEventListener<TestEvent>
    {
        private readonly List<string> _calls;
        private readonly string _name;

        public CancellationToken CancellationToken { get; private set; }
        public TestEvent? LastEvent { get; private set; }

        public AsyncListener(string name, List<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        public async Task HandleAsync(TestEvent eventData, CancellationToken cancellationToken)
        {
            await Task.Yield();

            CancellationToken = cancellationToken;
            LastEvent = eventData;
            _calls.Add($"{_name}:{eventData.Payload}");
        }
    }

    private sealed class ThrowingAsyncListener : IAsyncEventListener<TestEvent>
    {
        private readonly InvalidOperationException _exception;

        public ThrowingAsyncListener(InvalidOperationException exception)
        {
            _exception = exception;
        }

        public Task HandleAsync(TestEvent eventData, CancellationToken cancellationToken)
            => throw _exception;
    }

    [Fact]
    public void Publish_NoSyncListeners_DoesNotThrow()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;

        var exception = Record.Exception(() => bus.Publish(new TestEvent("ignored")));

        Assert.Null(exception);
    }

    [Fact]
    public void Publish_RegisteredSyncListeners_InvokesEachInRegistrationOrder()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var calls = new List<string>();
        var first = new SyncListener("first", calls);
        var second = new SyncListener("second", calls);
        bus.RegisterListener(first);
        bus.RegisterListener(second);
        var eventData = new TestEvent("payload");

        bus.Publish(eventData);

        Assert.Equal(["first:payload", "second:payload"], calls);
        Assert.Same(eventData, first.LastEvent);
        Assert.Same(eventData, second.LastEvent);
    }

    [Fact]
    public void Publish_WhenSyncListenerThrows_PropagatesException()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var expected = new InvalidOperationException("sync failure");
        bus.RegisterListener(new ThrowingSyncListener(expected));

        var actual = Assert.Throws<InvalidOperationException>(() => bus.Publish(new TestEvent("payload")));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task PublishAsync_NoAsyncListeners_Completes()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;

        var exception =
            await Record.ExceptionAsync(() => bus.PublishAsync(new TestEvent("ignored"), CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_PassesCancellationToken()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        using var cancellationTokenSource = new CancellationTokenSource();
        var listener = new AsyncListener("listener", []);
        bus.RegisterAsyncListener(listener);

        await bus.PublishAsync(new TestEvent("payload"), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, listener.CancellationToken);
    }

    [Fact]
    public async Task PublishAsync_RegisteredAsyncListeners_InvokesEachInRegistrationOrder()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var calls = new List<string>();
        var first = new AsyncListener("first", calls);
        var second = new AsyncListener("second", calls);
        bus.RegisterAsyncListener(first);
        bus.RegisterAsyncListener(second);
        var eventData = new TestEvent("payload");

        await bus.PublishAsync(eventData, CancellationToken.None);

        Assert.Equal(["first:payload", "second:payload"], calls);
        Assert.Same(eventData, first.LastEvent);
        Assert.Same(eventData, second.LastEvent);
    }

    [Fact]
    public async Task PublishAsync_WhenAsyncListenerThrows_PropagatesException()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var expected = new InvalidOperationException("async failure");
        bus.RegisterAsyncListener(new ThrowingAsyncListener(expected));

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                         () => bus.PublishAsync(new TestEvent("payload"), CancellationToken.None)
                     );

        Assert.Same(expected, actual);
    }
}
