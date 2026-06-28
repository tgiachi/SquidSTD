using DryIoc;
using SquidStd.Abstractions.Extensions.Commands;
using SquidStd.Core.Interfaces.Commands;
using SquidStd.Services.Core.Extensions;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Services.Core;

public class RegisterCommandDispatcherExtensionsTests
{
    [Fact]
    public void RegisterCommandDispatcher_RegistersSingleton()
    {
        using var container = new Container();
        container.RegisterCommandDispatcher<Session>();

        var first = container.Resolve<ICommandDispatcher<Session>>();
        var second = container.Resolve<ICommandDispatcher<Session>>();

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task Activator_SubscribesRegisteredHandlers()
    {
        using var container = new Container();
        container.RegisterCommandDispatcher<Session>();
        container.RegisterCommandHandler<PingCommand, Session, PingHandler>();

        var activator = container.Resolve<CommandDispatcherActivator<Session>>();
        await activator.StartAsync(CancellationToken.None);

        var dispatcher = container.Resolve<ICommandDispatcher<Session>>();
        var session = new Session();
        var result = await dispatcher.DispatchAsync(new PingCommand("hi"), session);

        Assert.True(result.Matched);
        Assert.Equal(1, result.HandlerCount);
        Assert.Equal("hi", container.Resolve<PingHandler>().LastText);
    }

    private sealed class Session
    {
    }

    private sealed class PingHandler : ICommandHandler<PingCommand, Session>
    {
        public string? LastText { get; private set; }

        public Task HandleAsync(PingCommand command, Session context, CancellationToken cancellationToken = default)
        {
            LastText = command.Text;

            return Task.CompletedTask;
        }
    }

    private sealed record PingCommand(string Text) : ICommand;
}
