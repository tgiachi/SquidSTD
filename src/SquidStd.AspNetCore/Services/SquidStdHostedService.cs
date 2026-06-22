using Microsoft.Extensions.Hosting;
using SquidStd.Core.Interfaces.Bootstrap;

namespace SquidStd.AspNetCore.Services;

/// <summary>
/// Bridges the ASP.NET Core host lifecycle to the SquidStd bootstrap lifecycle.
/// </summary>
internal sealed class SquidStdHostedService : IHostedService
{
    private readonly ISquidStdBootstrap _bootstrap;

    /// <summary>
    /// Initializes the hosted service.
    /// </summary>
    /// <param name="bootstrap">SquidStd bootstrap instance started with the ASP.NET host.</param>
    public SquidStdHostedService(ISquidStdBootstrap bootstrap)
    {
        _bootstrap = bootstrap;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _bootstrap.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _bootstrap.StopAsync(cancellationToken);
    }
}
