namespace SquidStd.Abstractions.Interfaces.Services;

/// <summary>
/// Defines lifecycle operations for a SquidStd service.
/// </summary>
public interface ISquidStdService
{
    /// <summary>Starts the service.</summary>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the service.</summary>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
