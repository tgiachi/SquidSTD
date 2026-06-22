using System.Net;
using DryIoc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SquidStd.AspNetCore.Extensions;
using SquidStd.Core.Interfaces.Bootstrap;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Interfaces.Metrics;
using SquidStd.Core.Interfaces.Timing;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.AspNetCore;

public class SquidStdAspNetCoreBuilderExtensionsTests
{
    [Fact]
    public async Task UseSquidStd_RegistersBootstrapWithContentRootDefaults()
    {
        using var temp = new TempDirectory();
        var builder = CreateBuilder(temp.Path);

        var returned = builder.UseSquidStd(options => options.ConfigName = "app");

        await using var app = builder.Build();
        var bootstrap = app.Services.GetRequiredService<ISquidStdBootstrap>();

        Assert.Same(builder, returned);
        Assert.Equal("app", bootstrap.Options.ConfigName);
        Assert.Equal(Path.GetFullPath(temp.Path), Path.GetFullPath(bootstrap.Options.RootDirectory));
    }

    [Fact]
    public async Task UseSquidStd_ResolvesSquidStdServicesAfterHostStart()
    {
        using var temp = new TempDirectory();
        var builder = CreateBuilder(temp.Path);
        builder.UseSquidStd(options => options.ConfigName = "app");

        await using var app = builder.Build();
        await app.StartAsync();

        try
        {
            Assert.NotNull(app.Services.GetRequiredService<ITimerService>());
            Assert.NotNull(app.Services.GetRequiredService<IEventBus>());
            Assert.NotNull(app.Services.GetRequiredService<IMetricsCollectionService>());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task UseSquidStd_AllowsMinimalApiInjection()
    {
        using var temp = new TempDirectory();
        var builder = CreateBuilder(temp.Path);
        builder.UseSquidStd(options => options.ConfigName = "app");

        await using var app = builder.Build();
        app.MapGet("/timer", (ITimerService timer) => Results.Ok(timer.GetType().Name));

        await app.StartAsync();

        try
        {
            var client = app.GetTestClient();
            var response = await client.GetAsync("/timer");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("TimerWheelService", content, StringComparison.Ordinal);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task UseSquidStd_AppliesCustomDryIocRegistrations()
    {
        using var temp = new TempDirectory();
        var builder = CreateBuilder(temp.Path);

        builder.UseSquidStd(
            options => options.ConfigName = "app",
            container =>
            {
                container.RegisterInstance(new TestAspNetCoreMarker { Value = "custom" });

                return container;
            }
        );

        await using var app = builder.Build();

        Assert.Equal("custom", app.Services.GetRequiredService<TestAspNetCoreMarker>().Value);
    }

    [Fact]
    public void UseSquidStd_WhenContainerCallbackReturnsDifferentContainer_Throws()
    {
        using var temp = new TempDirectory();
        var builder = CreateBuilder(temp.Path);

        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.UseSquidStd(
                options => options.ConfigName = "app",
                _ => new Container()
            )
        );

        Assert.Equal("ConfigureSquidStdContainer must return the DryIoc container instance.", ex.Message);
    }

    [Fact]
    public void UseSquidStd_WhenOptionsAreInvalid_Throws()
    {
        using var temp = new TempDirectory();
        var builder = CreateBuilder(temp.Path);

        Assert.Throws<ArgumentException>(
            () => builder.UseSquidStd(options => options.ConfigName = string.Empty)
        );
    }

    private static WebApplicationBuilder CreateBuilder(string contentRootPath)
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                ContentRootPath = contentRootPath,
                EnvironmentName = Environments.Development
            }
        );

        builder.WebHost.UseTestServer();

        return builder;
    }
}
