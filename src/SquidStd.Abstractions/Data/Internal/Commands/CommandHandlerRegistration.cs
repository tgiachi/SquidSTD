using DryIoc;
using SquidStd.Core.Interfaces.Commands;

namespace SquidStd.Abstractions.Data.Internal.Commands;

/// <summary>
///     A declarative command-handler registration consumed by the bootstrap activator. The
///     <see cref="Subscribe" /> closure captures the concrete command and handler types at registration
///     time, so subscription needs no reflection.
/// </summary>
/// <typeparam name="TContext">The dispatcher context type.</typeparam>
/// <param name="HandlerType">The concrete handler implementation type.</param>
/// <param name="Subscribe">Resolves the handler and subscribes it to the dispatcher.</param>
public sealed record CommandHandlerRegistration<TContext>(
    Type HandlerType,
    Action<ICommandDispatcher<TContext>, IResolverContext> Subscribe
);
