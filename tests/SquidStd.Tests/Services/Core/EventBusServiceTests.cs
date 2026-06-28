using Serilog;
using Serilog.Events;
using SquidStd.Core.Data.Events;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Services.Core.Services;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Services.Core;

public class EventBusServiceTests
{
    [Fact]
    public async Task PublishAsync_NoListeners_Completes()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;

        var exception =
            await Record.ExceptionAsync(() => bus.PublishAsync(new TestEvent("ignored"), CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_TypedListener_ReceivesEvent()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var calls = new List<string>();
        var listener = new RecordingListener("only", calls);
        bus.RegisterListener(listener);
        var eventData = new TestEvent("payload");

        await bus.PublishAsync(eventData, CancellationToken.None);

        Assert.Equal(["only:payload"], calls);
        Assert.Same(eventData, listener.LastEvent);
    }

    [Fact]
    public async Task PublishAsync_OnlyMatchingEventType_Receives()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var testListener = new RecordingListener("test", []);
        var otherListener = new OtherRecordingListener();
        bus.RegisterListener(testListener);
        bus.RegisterListener(otherListener);

        await bus.PublishAsync(new TestEvent("payload"), CancellationToken.None);

        Assert.NotNull(testListener.LastEvent);
        Assert.Null(otherListener.LastEvent);
    }

    [Fact]
    public async Task PublishAsync_CatchAllListener_ReceivesEveryEventType()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var catchAll = new CatchAllListener();
        bus.RegisterListener(catchAll);

        await bus.PublishAsync(new TestEvent("a"), CancellationToken.None);
        await bus.PublishAsync(new OtherEvent(7), CancellationToken.None);

        Assert.Equal(2, catchAll.Received.Count);
        Assert.IsType<TestEvent>(catchAll.Received[0]);
        Assert.IsType<OtherEvent>(catchAll.Received[1]);
    }

    [Fact]
    public async Task PublishAsync_MultipleListeners_AllReceive()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var calls = new List<string>();
        var first = new RecordingListener("first", calls);
        var second = new RecordingListener("second", calls);
        bus.RegisterListener(first);
        bus.RegisterListener(second);

        await bus.PublishAsync(new TestEvent("payload"), CancellationToken.None);

        Assert.Contains("first:payload", calls);
        Assert.Contains("second:payload", calls);
    }

    [Fact]
    public async Task PublishAsync_WhenListenerThrows_IsolatesAndOthersStillRun()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var calls = new List<string>();
        var survivor = new RecordingListener("survivor", calls);
        bus.RegisterListener(new ThrowingListener());
        bus.RegisterListener(survivor);

        var exception =
            await Record.ExceptionAsync(() => bus.PublishAsync(new TestEvent("payload"), CancellationToken.None));

        Assert.Null(exception);
        Assert.Equal(["survivor:payload"], calls);
    }

    [Fact]
    public async Task PublishAsync_PreCancelledToken_Throws()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        bus.RegisterListener(new RecordingListener("x", []));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bus.PublishAsync(new TestEvent("payload"), cts.Token)
        );
    }

    [Fact]
    public async Task PublishAsync_ListenerCancellation_IsNotSwallowed()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        using var cts = new CancellationTokenSource();
        bus.RegisterListener(new SelfCancellingListener(cts));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bus.PublishAsync(new TestEvent("payload"), cts.Token)
        );
    }

    [Fact]
    public async Task RegisterListener_DisposeToken_StopsDelivery()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var calls = new List<string>();
        var token = bus.RegisterListener(new RecordingListener("once", calls));

        await bus.PublishAsync(new TestEvent("first"), CancellationToken.None);
        token.Dispose();
        await bus.PublishAsync(new TestEvent("second"), CancellationToken.None);

        Assert.Equal(["once:first"], calls);
    }

    [Fact]
    public async Task Subscribe_DelegateHandler_ReceivesAndUnsubscribes()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var received = new List<string>();
        var token = bus.Subscribe<TestEvent>((e, _) =>
            {
                received.Add(e.Payload);

                return Task.CompletedTask;
            }
        );

        await bus.PublishAsync(new TestEvent("first"), CancellationToken.None);
        token.Dispose();
        await bus.PublishAsync(new TestEvent("second"), CancellationToken.None);

        Assert.Equal(["first"], received);
    }

    [Fact]
    public void Publish_Sync_DeliversToAllListeners()
    {
        using var eventBus = new EventBusService();
        IEventBus bus = eventBus;
        var calls = new List<string>();
        bus.RegisterListener(new RecordingListener("a", calls));
        bus.RegisterListener(new RecordingListener("b", calls));

        bus.Publish(new TestEvent("payload"));

        Assert.Contains("a:payload", calls);
        Assert.Contains("b:payload", calls);
    }

    private sealed class RecordingListener : IEventListener<TestEvent>
    {
        private readonly List<string> _calls;
        private readonly string _name;

        public TestEvent? LastEvent { get; private set; }

        public RecordingListener(string name, List<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        public Task HandleAsync(TestEvent eventData, CancellationToken cancellationToken = default)
        {
            LastEvent = eventData;
            _calls.Add($"{_name}:{eventData.Payload}");

            return Task.CompletedTask;
        }
    }

    private sealed class OtherRecordingListener : IEventListener<OtherEvent>
    {
        public OtherEvent? LastEvent { get; private set; }

        public Task HandleAsync(OtherEvent eventData, CancellationToken cancellationToken = default)
        {
            LastEvent = eventData;

            return Task.CompletedTask;
        }
    }

    private sealed class CatchAllListener : IEventListener<IEvent>
    {
        public List<IEvent> Received { get; } = [];

        public Task HandleAsync(IEvent eventData, CancellationToken cancellationToken = default)
        {
            Received.Add(eventData);

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingListener : IEventListener<TestEvent>
    {
        public Task HandleAsync(TestEvent eventData, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("listener failure");
        }
    }

    private sealed class SelfCancellingListener : IEventListener<TestEvent>
    {
        private readonly CancellationTokenSource _cts;

        public SelfCancellingListener(CancellationTokenSource cts)
        {
            _cts = cts;
        }

        public Task HandleAsync(TestEvent eventData, CancellationToken cancellationToken = default)
        {
            _cts.Cancel();
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }

    private sealed class SlowListener : IEventListener<TestEvent>
    {
        public async Task HandleAsync(TestEvent eventData, CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(40), cancellationToken);
        }
    }

    [Collection(SerilogEventSinkCollection.Name)]
    public class Telemetry
    {
        [Fact]
        public async Task PublishAsync_SlowListener_LogsWarning()
        {
            var sink = new CapturingLogSink();
            var original = Log.Logger;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(sink)
                .CreateLogger();

            try
            {
                using var eventBus = new EventBusService(
                    new EventBusOptions { SlowListenerThreshold = TimeSpan.FromMilliseconds(5) }
                );
                IEventBus bus = eventBus;
                bus.RegisterListener(new SlowListener());

                await bus.PublishAsync(new TestEvent("payload"), CancellationToken.None);

                Assert.Contains(
                    sink.Events,
                    e => e.Level == LogEventLevel.Warning && e.MessageTemplate.Text.Contains("Slow event listener")
                );
            }
            finally
            {
                (Log.Logger as IDisposable)?.Dispose();
                Log.Logger = original;
            }
        }
    }

    private sealed record TestEvent(string Payload) : IEvent;

    private sealed record OtherEvent(int Value) : IEvent;
}
