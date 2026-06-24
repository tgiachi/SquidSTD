using SquidStd.Core.Data.Bootstrap;
using SquidStd.Database.Abstractions.Data.Entities;
using SquidStd.Database.Abstractions.Interfaces.Data;
using SquidStd.Database.Extensions;
using SquidStd.Services.Core.Services.Bootstrap;

var bootstrap = SquidStdBootstrap.Create(new SquidStdOptions
{
    ConfigName = "squidstd",
    RootDirectory = AppContext.BaseDirectory
});

#region step-1
bootstrap.ConfigureServices(container => container.RegisterDatabase());

await bootstrap.StartAsync();
#endregion

#region step-2
var products = bootstrap.Resolve<IDataAccess<Product>>();

await products.InsertAsync(new Product { Name = "Squid Plushie", Price = 19.99m });
await products.InsertAsync(new Product { Name = "Kraken Mug", Price = 12.50m });

var page = await products.GetPagedAsync(
    page: 1,
    pageSize: 10,
    orderBy: product => product.Price);

Console.WriteLine($"Found {page.TotalCount} product(s) on page {page.Page}/{page.TotalPages}:");

foreach (var product in page.Items)
{
    Console.WriteLine($"  {product.Name} - {product.Price:C}");
}
#endregion

await bootstrap.StopAsync();

internal sealed class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
}
