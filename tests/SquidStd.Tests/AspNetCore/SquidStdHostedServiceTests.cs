using SquidStd.AspNetCore.Services;

namespace SquidStd.Tests.AspNetCore;

public class SquidStdHostedServiceTests
{
    [Fact]
    public async Task StartAsync_StopAsync_DelegatesToBootstrap()
    {
        await using var bootstrap = new FakeSquidStdBootstrap();
        var service = new SquidStdHostedService(bootstrap);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, bootstrap.StartCount);
        Assert.Equal(1, bootstrap.StopCount);
    }
}
