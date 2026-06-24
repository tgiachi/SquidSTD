using SquidStd.Core.Data.Bootstrap;
using SquidStd.Services.Core.Services.Bootstrap;

#region step-1
var bootstrap = SquidStdBootstrap.Create(new SquidStdOptions
{
    ConfigName = "squidstd",
    RootDirectory = AppContext.BaseDirectory
});
#endregion

#region step-2
await bootstrap.StartAsync();
#endregion

#region step-3
await bootstrap.RunAsync();
#endregion
