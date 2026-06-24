using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Caching.Extensions;
using SquidStd.Core.Data.Bootstrap;
using SquidStd.Services.Core.Services.Bootstrap;

var bootstrap = SquidStdBootstrap.Create(new SquidStdOptions
{
    ConfigName = "squidstd",
    RootDirectory = AppContext.BaseDirectory
});

#region step-1
bootstrap.ConfigureServices(container => container.AddInMemoryCache());
#endregion

await bootstrap.StartAsync();

#region step-2
var cache = bootstrap.Resolve<ICacheService>();

await cache.SetAsync("greeting", "hello", TimeSpan.FromMinutes(5));
var value = await cache.GetAsync<string>("greeting");

Console.WriteLine(value);
#endregion

await bootstrap.StopAsync();
