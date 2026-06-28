using SquidStd.Core.Data.Health;
using SquidStd.Core.Interfaces.Health;

namespace SquidStd.Tests.Support;

/// <summary>
///     Configurable <see cref="IHealthCheck" /> for tests: returns a fixed result, optionally after a
///     delay, or throws a configured exception.
/// </summary>
public sealed class FakeHealthCheck : IHealthCheck
{
    private readonly TimeSpan _delay;
    private readonly HealthCheckResult? _result;
    private readonly Exception? _throw;

    public string Name { get; }

    public FakeHealthCheck(
        string name,
        HealthCheckResult? result = null,
        TimeSpan? delay = null,
        Exception? throwException = null
    )
    {
        Name = name;
        _result = result;
        _delay = delay ?? TimeSpan.Zero;
        _throw = throwException;
    }

    public async ValueTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (_delay > TimeSpan.Zero)
        {
            await Task.Delay(_delay, cancellationToken);
        }

        if (_throw is not null)
        {
            throw _throw;
        }

        return _result ?? HealthCheckResult.Healthy();
    }
}
