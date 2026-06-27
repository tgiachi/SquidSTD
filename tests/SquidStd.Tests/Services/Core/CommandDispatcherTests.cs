using SquidStd.Core.Interfaces.Commands;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Services.Core;

public class CommandDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_FansOutToEveryHandler()
    {
        using var dispatcher = new CommandDispatcher<Session>();
        var first = new RecordingHandler();
        var second = new RecordingHandler();
        dispatcher.RegisterHandler(first);
        dispatcher.RegisterHandler(second);
        var session = new Session();

        var result = await dispatcher.DispatchAsync(new PingCommand("hi"), session);

        Assert.True(result.Matched);
        Assert.Equal(2, result.HandlerCount);
        Assert.Empty(result.Errors);
        Assert.Equal("hi", first.LastText);
        Assert.Equal("hi", second.LastText);
        Assert.Same(session, first.LastContext);
    }

    [Fact]
    public async Task DispatchAsync_WhenNoHandler_ReturnsUnmatched()
    {
        using var dispatcher = new CommandDispatcher<Session>();

        var result = await dispatcher.DispatchAsync(new PingCommand("x"), new Session());

        Assert.False(result.Matched);
        Assert.Equal(0, result.HandlerCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task DispatchAsync_IsolatesFaults()
    {
        using var dispatcher = new CommandDispatcher<Session>();
        var healthy = new RecordingHandler();
        dispatcher.RegisterHandler(new ThrowingHandler());
        dispatcher.RegisterHandler(healthy);

        var result = await dispatcher.DispatchAsync(new PingCommand("go"), new Session());

        Assert.True(result.Matched);
        Assert.Equal(2, result.HandlerCount);
        Assert.Equal("go", healthy.LastText);
        var error = Assert.Single(result.Errors);
        Assert.Equal(typeof(ThrowingHandler), error.HandlerType);
        Assert.IsType<InvalidOperationException>(error.Exception);
    }

    [Fact]
    public async Task DispatchAsync_WhenCancelled_Propagates()
    {
        using var dispatcher = new CommandDispatcher<Session>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        dispatcher.Subscribe<PingCommand>((_, _, token) =>
            {
                token.ThrowIfCancellationRequested();

                return Task.CompletedTask;
            }
        );

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => dispatcher.DispatchAsync(new PingCommand("x"), new Session(), cts.Token)
        );
    }

    [Fact]
    public async Task RegisterHandler_DisposeToken_StopsDelivery()
    {
        using var dispatcher = new CommandDispatcher<Session>();
        var handler = new RecordingHandler();
        var token = dispatcher.RegisterHandler(handler);

        token.Dispose();
        var result = await dispatcher.DispatchAsync(new PingCommand("x"), new Session());

        Assert.False(result.Matched);
        Assert.Null(handler.LastText);
    }

    [Fact]
    public async Task Subscribe_DeliversToDelegate()
    {
        using var dispatcher = new CommandDispatcher<Session>();
        string? seen = null;
        Session? seenContext = null;
        dispatcher.Subscribe<PingCommand>((command, context, _) =>
            {
                seen = command.Text;
                seenContext = context;

                return Task.CompletedTask;
            }
        );
        var session = new Session();

        var result = await dispatcher.DispatchAsync(new PingCommand("yo"), session);

        Assert.True(result.Matched);
        Assert.Equal("yo", seen);
        Assert.Same(session, seenContext);
    }

    private sealed class Session
    {
    }

    private sealed record PingCommand(string Text) : ICommand;

    private sealed class RecordingHandler : ICommandHandler<PingCommand, Session>
    {
        public string? LastText { get; private set; }

        public Session? LastContext { get; private set; }

        public Task HandleAsync(PingCommand command, Session context, CancellationToken cancellationToken = default)
        {
            LastText = command.Text;
            LastContext = context;

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : ICommandHandler<PingCommand, Session>
    {
        public Task HandleAsync(PingCommand command, Session context, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Synthetic failure.");
        }
    }
}
