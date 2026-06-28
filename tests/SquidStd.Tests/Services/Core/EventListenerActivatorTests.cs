using DryIoc;
using SquidStd.Abstractions.Extensions.Events;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Services.Core;

public class EventListenerActivatorTests
{
    [Fact]
    public async Task StartAsync_SubscribesRegisteredListeners()
    {
        using var container = new Container();
        var bus = new EventBusService();
        container.RegisterInstance<IEventBus>(bus);
        container.RegisterEventListener<PingEvent, PingListener>();

        var activator = new EventListenerActivator(container, bus);
        await activator.StartAsync(CancellationToken.None);

        await bus.PublishAsync(new PingEvent("hello"), CancellationToken.None);

        var listener = container.Resolve<PingListener>();
        Assert.NotNull(listener.LastEvent);
        Assert.Equal("hello", listener.LastEvent.Message);
    }

    [Fact]
    public async Task StartAsync_NoRegistrations_DoesNotThrow()
    {
        using var container = new Container();
        var bus = new EventBusService();
        container.RegisterInstance<IEventBus>(bus);

        var activator = new EventListenerActivator(container, bus);

        var exception = await Record.ExceptionAsync(() => activator.StartAsync(CancellationToken.None).AsTask());

        Assert.Null(exception);
    }

    private sealed class PingListener : IEventListener<PingEvent>
    {
        public PingEvent? LastEvent { get; private set; }

        public Task HandleAsync(PingEvent eventData, CancellationToken cancellationToken = default)
        {
            LastEvent = eventData;

            return Task.CompletedTask;
        }
    }

    private sealed record PingEvent(string Message) : IEvent;
}
