using DryIoc;
using SquidStd.Core.Data.Bootstrap;

namespace SquidStd.Core.Interfaces.Bootstrap;

/// <summary>
/// Coordinates SquidStd dependency registration, configuration loading, and service lifecycle.
/// </summary>
public interface ISquidStdBootstrap : IAsyncDisposable
{
    /// <summary>
    /// Gets the bootstrap options used to configure runtime resources.
    /// </summary>
    SquidStdOptions Options { get; }

    /// <summary>
    /// Gets the owned dependency injection container.
    /// </summary>
    IContainer Container { get; }

    /// <summary>
    /// Applies custom service registrations before the bootstrap lifecycle starts.
    /// </summary>
    /// <param name="configure">Callback that receives and returns the container.</param>
    /// <returns>The same bootstrap instance for chaining.</returns>
    ISquidStdBootstrap ConfigureService(Func<IContainer, IContainer> configure);

    /// <summary>
    /// Applies custom service registrations before the bootstrap lifecycle starts.
    /// </summary>
    /// <param name="configure">Callback that receives and returns the container.</param>
    /// <returns>The same bootstrap instance for chaining.</returns>
    ISquidStdBootstrap ConfigureServices(Func<IContainer, IContainer> configure);

    /// <summary>
    /// Resolves a service from the owned dependency injection container.
    /// </summary>
    /// <typeparam name="TService">The service type to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    TService Resolve<TService>();

    /// <summary>
    /// Starts registered lifecycle services in priority order.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the start operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops started lifecycle services in reverse priority order.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the stop operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    ValueTask StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts services, waits until cancellation, and then stops services.
    /// </summary>
    /// <param name="cancellationToken">Token that controls the run lifetime.</param>
    /// <returns>A task that completes after services have stopped.</returns>
    Task RunAsync(CancellationToken cancellationToken = default);
}
