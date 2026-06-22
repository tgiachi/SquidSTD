using DryIoc;
using SquidStd.Storage.Abstractions.Data.Config;
using SquidStd.Storage.Abstractions.Interfaces;
using SquidStd.Storage.Extensions;

namespace SquidStd.Tests.Storage;

public class StorageRegistrationTests
{
    [Fact]
    public async Task AddFileStorage_ResolvesAndRoundTrips()
    {
        var root = Path.Combine(Path.GetTempPath(), "squidstd-storage-" + Guid.NewGuid().ToString("N"));
        var container = new Container();

        container.AddFileStorage(new StorageConfig { RootDirectory = root });

        var storage = container.Resolve<IStorageService>();
        Assert.NotNull(container.Resolve<IObjectStorageService>());

        await storage.SaveAsync("k", new byte[] { 1, 2, 3 });
        var loaded = await storage.LoadAsync("k");

        Assert.Equal(new byte[] { 1, 2, 3 }, loaded);

        Directory.Delete(root, recursive: true);
    }
}
