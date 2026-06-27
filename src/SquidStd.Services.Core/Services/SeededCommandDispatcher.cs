using SquidStd.Core.Data.Commands;
using SquidStd.Core.Interfaces.Commands;

namespace SquidStd.Services.Core.Services;

/// <summary>
///     Seeded dispatcher that builds the context from a seed via an
///     <see cref="ICommandContextFactory{TContext,TSeed}" /> and forwards to the underlying
///     <see cref="ICommandDispatcher{TContext}" />.
/// </summary>
/// <typeparam name="TContext">The ambient context type.</typeparam>
/// <typeparam name="TSeed">The seed the context is built from.</typeparam>
public sealed class SeededCommandDispatcher<TContext, TSeed> : ISeededCommandDispatcher<TContext, TSeed>
{
    private readonly ICommandDispatcher<TContext> _dispatcher;
    private readonly ICommandContextFactory<TContext, TSeed> _contextFactory;

    public SeededCommandDispatcher(
        ICommandDispatcher<TContext> dispatcher,
        ICommandContextFactory<TContext, TSeed> contextFactory
    )
    {
        _dispatcher = dispatcher;
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public Task<CommandDispatchResult> DispatchAsync<TCommand>(
        TCommand command, TSeed seed, CancellationToken cancellationToken = default
    )
        where TCommand : ICommand
    {
        return _dispatcher.DispatchAsync(command, _contextFactory.Create(seed), cancellationToken);
    }
}
