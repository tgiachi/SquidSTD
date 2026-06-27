using DryIoc;
using SquidStd.Abstractions.Extensions.Services;
using SquidStd.Core.Interfaces.Commands;
using SquidStd.Services.Core.Services;

namespace SquidStd.Services.Core.Extensions;

/// <summary>
///     Extension methods for registering a command dispatcher and its context factory.
/// </summary>
public static class RegisterCommandDispatcherExtensions
{
    /// <param name="container">Container that receives the command dispatcher registrations.</param>
    extension(IContainer container)
    {
        /// <summary>
        ///     Registers an <see cref="ICommandDispatcher{TContext}" /> singleton and its bootstrap activator.
        /// </summary>
        /// <typeparam name="TContext">The dispatcher context type.</typeparam>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterCommandDispatcher<TContext>()
        {
            container.Register<ICommandDispatcher<TContext>, CommandDispatcher<TContext>>(Reuse.Singleton);
            container.RegisterStdService<CommandDispatcherActivator<TContext>, CommandDispatcherActivator<TContext>>(-900);

            return container;
        }

        /// <summary>
        ///     Registers a seeded context factory and an <see cref="ISeededCommandDispatcher{TContext,TSeed}" />
        ///     singleton over the existing <see cref="ICommandDispatcher{TContext}" /> (which must already be
        ///     registered via <see cref="RegisterCommandDispatcher{TContext}" />).
        /// </summary>
        /// <typeparam name="TContext">The dispatcher context type.</typeparam>
        /// <typeparam name="TSeed">The seed the context is built from.</typeparam>
        /// <typeparam name="TFactory">The seeded factory implementation type.</typeparam>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterSeededCommandDispatcher<TContext, TSeed, TFactory>()
            where TFactory : class, ICommandContextFactory<TContext, TSeed>
        {
            container.Register<ICommandContextFactory<TContext, TSeed>, TFactory>(Reuse.Singleton);
            container.RegisterDelegate<ISeededCommandDispatcher<TContext, TSeed>>(
                resolver => new SeededCommandDispatcher<TContext, TSeed>(
                    resolver.Resolve<ICommandDispatcher<TContext>>(),
                    resolver.Resolve<ICommandContextFactory<TContext, TSeed>>()
                ),
                Reuse.Singleton
            );

            return container;
        }
    }
}
