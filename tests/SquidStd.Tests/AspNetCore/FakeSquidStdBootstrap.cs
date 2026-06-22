using DryIoc;
using SquidStd.Core.Data.Bootstrap;
using SquidStd.Core.Interfaces.Bootstrap;

namespace SquidStd.Tests.AspNetCore;

internal sealed class FakeSquidStdBootstrap : ISquidStdBootstrap
{
    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public SquidStdOptions Options { get; } = new();

    public IContainer Container { get; } = new Container();

    public ISquidStdBootstrap ConfigureService(Func<IContainer, IContainer> configure)
        => ConfigureServices(configure);

    public ISquidStdBootstrap ConfigureServices(Func<IContainer, IContainer> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var configuredContainer = configure(Container);

        return ReferenceEquals(configuredContainer, Container)
                   ? this
                   : throw new InvalidOperationException("ConfigureServices must return the bootstrap container instance.");
    }

    public TService Resolve<TService>()
        => Container.Resolve<TService>();

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartCount++;

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StopCount++;

        return ValueTask.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await StartAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        Container.Dispose();

        return ValueTask.CompletedTask;
    }
}
