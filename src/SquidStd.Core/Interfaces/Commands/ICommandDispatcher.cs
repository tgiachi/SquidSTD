using SquidStd.Core.Data.Commands;

namespace SquidStd.Core.Interfaces.Commands;

/// <summary>
///     Dispatches typed commands to registered handlers within an ambient <typeparamref name="TContext" />,
///     fanning a command out to every handler registered for its type.
/// </summary>
/// <typeparam name="TContext">The ambient context type.</typeparam>
public interface ICommandDispatcher<TContext>
{
    /// <summary>Registers a handler. Dispose the returned token to unregister.</summary>
    /// <typeparam name="TCommand">The command type the handler handles.</typeparam>
    /// <param name="handler">The handler to register.</param>
    /// <returns>A token that unregisters the handler when disposed.</returns>
    IDisposable RegisterHandler<TCommand>(ICommandHandler<TCommand, TContext> handler) where TCommand : ICommand;

    /// <summary>Subscribes a delegate handler. Dispose the returned token to unsubscribe.</summary>
    /// <typeparam name="TCommand">The command type the handler handles.</typeparam>
    /// <param name="handler">The delegate invoked for each dispatched command.</param>
    /// <returns>A token that unsubscribes the handler when disposed.</returns>
    IDisposable Subscribe<TCommand>(Func<TCommand, TContext, CancellationToken, Task> handler) where TCommand : ICommand;

    /// <summary>Dispatches a command to every registered handler with an explicit context.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command payload.</param>
    /// <param name="context">The ambient context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The dispatch result.</returns>
    Task<CommandDispatchResult> DispatchAsync<TCommand>(
        TCommand command, TContext context, CancellationToken cancellationToken = default
    )
        where TCommand : ICommand;
}
