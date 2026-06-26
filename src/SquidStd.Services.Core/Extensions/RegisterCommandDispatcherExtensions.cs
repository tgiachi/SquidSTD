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
            container.RegisterDelegate<ICommandDispatcher<TContext>>(
                resolver => new CommandDispatcher<TContext>(
                    resolver.Resolve<ICommandContextFactory<TContext>>(IfUnresolved.ReturnDefault)
                ),
                Reuse.Singleton
            );
            container.RegisterStdService<CommandDispatcherActivator<TContext>, CommandDispatcherActivator<TContext>>(-900);

            return container;
        }

        /// <summary>
        ///     Registers the context factory used by the context-less dispatch overload.
        /// </summary>
        /// <typeparam name="TContext">The dispatcher context type.</typeparam>
        /// <typeparam name="TFactory">The factory implementation type.</typeparam>
        /// <returns>The same container for chaining.</returns>
        public IContainer RegisterCommandContextFactory<TContext, TFactory>()
            where TFactory : class, ICommandContextFactory<TContext>
        {
            container.Register<ICommandContextFactory<TContext>, TFactory>(Reuse.Singleton);

            return container;
        }
    }
}
