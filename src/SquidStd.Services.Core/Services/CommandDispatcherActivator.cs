using DryIoc;
using SquidStd.Abstractions.Data.Internal.Commands;
using SquidStd.Abstractions.Interfaces.Services;
using SquidStd.Core.Interfaces.Commands;

namespace SquidStd.Services.Core.Services;

/// <summary>
///     Subscribes every DI-registered command handler to the dispatcher at startup, before publishing
///     services run.
/// </summary>
/// <typeparam name="TContext">The dispatcher context type.</typeparam>
internal sealed class CommandDispatcherActivator<TContext> : ISquidStdService
{
    private readonly IContainer _container;
    private readonly ICommandDispatcher<TContext> _dispatcher;

    public CommandDispatcherActivator(IContainer container, ICommandDispatcher<TContext> dispatcher)
    {
        _container = container;
        _dispatcher = dispatcher;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_container.IsRegistered<List<CommandHandlerRegistration<TContext>>>())
        {
            var registrations = _container.Resolve<List<CommandHandlerRegistration<TContext>>>();

            for (var i = 0; i < registrations.Count; i++)
            {
                registrations[i].Subscribe(_dispatcher, _container);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
