using SquidStd.Core.Data.Commands;

namespace SquidStd.Core.Interfaces.Commands;

/// <summary>
///     Dispatches commands by first building the <typeparamref name="TContext" /> from a
///     <typeparamref name="TSeed" /> (for example the originating connection) through an
///     <see cref="ICommandContextFactory{TContext,TSeed}" />, then delegating to the underlying
///     <see cref="ICommandDispatcher{TContext}" />. Handlers are still registered on the
///     <see cref="ICommandDispatcher{TContext}" />.
/// </summary>
/// <typeparam name="TContext">The ambient context type.</typeparam>
/// <typeparam name="TSeed">The seed the context is built from.</typeparam>
public interface ISeededCommandDispatcher<TContext, in TSeed>
{
    /// <summary>Builds the context from the seed and dispatches the command.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command payload.</param>
    /// <param name="seed">The seed used to build the context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The dispatch result.</returns>
    Task<CommandDispatchResult> DispatchAsync<TCommand>(
        TCommand command, TSeed seed, CancellationToken cancellationToken = default
    )
        where TCommand : ICommand;
}
