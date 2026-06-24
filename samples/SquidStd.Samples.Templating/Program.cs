using SquidStd.Core.Data.Bootstrap;
using SquidStd.Services.Core.Services.Bootstrap;
using SquidStd.Templating.Extensions;
using SquidStd.Templating.Interfaces;

var bootstrap = SquidStdBootstrap.Create(new SquidStdOptions
{
    ConfigName = "squidstd",
    RootDirectory = AppContext.BaseDirectory
});

#region step-1
bootstrap.ConfigureServices(container => container.AddTemplating());
#endregion

await bootstrap.StartAsync();

#region step-2
var renderer = bootstrap.Resolve<ITemplateRenderer>();

var greeting = await renderer.RenderAsync("Hi {{ user.name }}!", new { User = new { Name = "squid" } });

Console.WriteLine(greeting);
#endregion

#region step-3
renderer.Register("welcome", "Welcome aboard, {{ user.name }}.");

var welcome = await renderer.RenderByNameAsync("welcome", new { User = new { Name = "squid" } });

Console.WriteLine(welcome);
#endregion

await bootstrap.StopAsync();
