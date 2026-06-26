using DryIoc;
using SquidStd.Abstractions.Data.Internal.Commands;
using SquidStd.Abstractions.Extensions.Container;
using SquidStd.Core.Interfaces.Commands;

namespace SquidStd.Abstractions.Extensions.Commands;

/// <summary>
///     Registers command handlers for DI-native auto-subscription at bootstrap.
/// </summary>
public static class RegisterCommandHandlerExtension
{
    /// <param name="container">The DryIoc container.</param>
    extension(IContainer container)
    {
        /// <summary>
        ///     Registers a command handler as a singleton and records it for auto-subscription.
        /// </summary>
        /// <typeparam name="TCommand">The command type the handler handles.</typeparam>
        /// <typeparam name="TContext">The dispatcher context type.</typeparam>
        /// <typeparam name="THandler">The handler implementation type.</typeparam>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterCommandHandler<TCommand, TContext, THandler>()
            where TCommand : ICommand
            where THandler : class, ICommandHandler<TCommand, TContext>
        {
            container.Register<THandler>(Reuse.Singleton);
            container.AddToRegisterTypedList(
                new CommandHandlerRegistration<TContext>(
                    typeof(THandler),
                    (dispatcher, resolver) => dispatcher.RegisterHandler(resolver.Resolve<THandler>())
                )
            );

            return container;
        }
    }
}
