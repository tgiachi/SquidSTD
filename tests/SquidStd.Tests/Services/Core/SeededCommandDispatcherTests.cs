using DryIoc;
using SquidStd.Abstractions.Extensions.Commands;
using SquidStd.Core.Interfaces.Commands;
using SquidStd.Services.Core.Extensions;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Services.Core;

public class SeededCommandDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_BuildsContextFromSeed()
    {
        using var inner = new CommandDispatcher<Session>();
        var handler = new RecordingHandler();
        inner.RegisterHandler(handler);
        var seeded = new SeededCommandDispatcher<Session, Connection>(inner, new ConnectionSessionFactory());

        var result = await seeded.DispatchAsync(new PingCommand("hi"), new Connection("conn-1"));

        Assert.True(result.Matched);
        Assert.Equal(1, result.HandlerCount);
        Assert.Equal("hi", handler.LastText);
        Assert.Equal("conn-1", handler.LastSessionId);
    }

    [Fact]
    public async Task RegisterSeededCommandDispatcher_ResolvesAndDispatches()
    {
        using var container = new Container();
        container.RegisterCommandDispatcher<Session>();
        container.RegisterCommandHandler<PingCommand, Session, PingHandler>();
        container.RegisterSeededCommandDispatcher<Session, Connection, ConnectionSessionFactory>();

        await container.Resolve<CommandDispatcherActivator<Session>>().StartAsync(CancellationToken.None);

        var seeded = container.Resolve<ISeededCommandDispatcher<Session, Connection>>();
        var result = await seeded.DispatchAsync(new PingCommand("yo"), new Connection("conn-2"));

        Assert.True(result.Matched);
        var handler = container.Resolve<PingHandler>();
        Assert.Equal("yo", handler.LastText);
        Assert.Equal("conn-2", handler.LastSessionId);
    }

    private sealed class Connection
    {
        public string Id { get; }

        public Connection(string id)
        {
            Id = id;
        }
    }

    private sealed class Session
    {
        public string Id { get; }

        public Session(string id)
        {
            Id = id;
        }
    }

    private sealed class ConnectionSessionFactory : ICommandContextFactory<Session, Connection>
    {
        public Session Create(Connection seed)
        {
            return new Session(seed.Id);
        }
    }

    private sealed class RecordingHandler : ICommandHandler<PingCommand, Session>
    {
        public string? LastText { get; private set; }

        public string? LastSessionId { get; private set; }

        public Task HandleAsync(PingCommand command, Session context, CancellationToken cancellationToken = default)
        {
            LastText = command.Text;
            LastSessionId = context.Id;

            return Task.CompletedTask;
        }
    }

    private sealed class PingHandler : ICommandHandler<PingCommand, Session>
    {
        public string? LastText { get; private set; }

        public string? LastSessionId { get; private set; }

        public Task HandleAsync(PingCommand command, Session context, CancellationToken cancellationToken = default)
        {
            LastText = command.Text;
            LastSessionId = context.Id;

            return Task.CompletedTask;
        }
    }

    private sealed record PingCommand(string Text) : ICommand;
}
