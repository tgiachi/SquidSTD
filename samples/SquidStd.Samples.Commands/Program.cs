using DryIoc;
using SquidStd.Abstractions.Data.Internal.Commands;
using SquidStd.Abstractions.Extensions.Commands;
using SquidStd.Core.Interfaces.Commands;
using SquidStd.Services.Core.Extensions;

var container = new Container();
container.RegisterCommandDispatcher<Session>();
container.RegisterCommandHandler<PingCommand, Session, PingHandler>();
container.RegisterCommandHandler<EchoCommand, Session, EchoHandler>();
container.RegisterCommandHandler<EchoCommand, Session, AuditHandler>();

var dispatcher = container.Resolve<ICommandDispatcher<Session>>();

// At runtime SquidStdBootstrap starts CommandDispatcherActivator<Session>, which performs this
// auto-subscription. Done inline here to keep the sample self-contained.
foreach (var registration in container.Resolve<List<CommandHandlerRegistration<Session>>>())
{
    registration.Subscribe(dispatcher, container);
}

// The context (here a Session) is passed explicitly at dispatch time — in a server this is the
// session the message arrived on. See RegisterSeededCommandDispatcher for building it from a seed.
var session = new Session();

await Dispatch(dispatcher, new PingCommand(), session);
await Dispatch(dispatcher, new EchoCommand("hello world"), session);

// Unknown command path: nothing registered for UnknownCommand.
var unknown = await dispatcher.DispatchAsync(new UnknownCommand(), session);
Console.WriteLine($"UnknownCommand -> matched={unknown.Matched} handlers={unknown.HandlerCount}");

return;

static async Task Dispatch<TCommand>(ICommandDispatcher<Session> dispatcher, TCommand command, Session context)
    where TCommand : ICommand
{
    var result = await dispatcher.DispatchAsync(command, context);
    Console.WriteLine(
        $"{typeof(TCommand).Name} -> matched={result.Matched} handlers={result.HandlerCount} errors={result.Errors.Count}"
    );
}

internal sealed class Session
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
}

internal sealed record PingCommand : ICommand;

internal sealed record EchoCommand(string Text) : ICommand;

internal sealed record UnknownCommand : ICommand;

internal sealed class PingHandler : ICommandHandler<PingCommand, Session>
{
    public Task HandleAsync(PingCommand command, Session context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{context.Id}] pong");

        return Task.CompletedTask;
    }
}

internal sealed class EchoHandler : ICommandHandler<EchoCommand, Session>
{
    public Task HandleAsync(EchoCommand command, Session context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{context.Id}] echo: {command.Text}");

        return Task.CompletedTask;
    }
}

internal sealed class AuditHandler : ICommandHandler<EchoCommand, Session>
{
    public Task HandleAsync(EchoCommand command, Session context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{context.Id}] audit: echo len={command.Text.Length}");

        return Task.CompletedTask;
    }
}
