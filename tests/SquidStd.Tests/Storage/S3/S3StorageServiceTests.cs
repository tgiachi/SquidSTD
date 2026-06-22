using System.Text;
using SquidStd.Storage.S3.Data.Config;
using SquidStd.Storage.S3.Services;

namespace SquidStd.Tests.Storage.S3;

[Collection(MinioCollection.Name)]
public class S3StorageServiceTests
{
    private readonly MinioContainerFixture _fixture;

    public S3StorageServiceTests(MinioContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private S3StorageService NewService()
        => new(
            new S3StorageOptions
            {
                Endpoint = _fixture.Endpoint,
                AccessKey = _fixture.AccessKey,
                SecretKey = _fixture.SecretKey,
                Bucket = "squidstd-tests",
                UseSsl = false
            }
        );

    private static string Key() => "k-" + Guid.NewGuid().ToString("N");

    [Fact]
    public async Task SaveThenLoad_RoundTrips_AndCreatesBucket()
    {
        var storage = NewService();
        var key = Key();

        await storage.SaveAsync(key, Encoding.UTF8.GetBytes("hello"));
        var loaded = await storage.LoadAsync(key);

        Assert.NotNull(loaded);
        Assert.Equal("hello", Encoding.UTF8.GetString(loaded!));
    }

    [Fact]
    public async Task Load_MissingKey_ReturnsNull()
        => Assert.Null(await NewService().LoadAsync(Key()));

    [Fact]
    public async Task Exists_And_Delete()
    {
        var storage = NewService();
        var key = Key();
        await storage.SaveAsync(key, Encoding.UTF8.GetBytes("v"));

        Assert.True(await storage.ExistsAsync(key));
        Assert.True(await storage.DeleteAsync(key));
        Assert.False(await storage.ExistsAsync(key));
        Assert.False(await storage.DeleteAsync(key));
    }

    [Fact]
    public void Ctor_MissingEndpoint_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new S3StorageService(new S3StorageOptions { AccessKey = "a", SecretKey = "b", Bucket = "c" })
        );
    }

    [Fact]
    public async Task ListKeysAsync_ReturnsObjectsUnderPrefix()
    {
        var storage = NewService();
        var prefix = "list-" + Guid.NewGuid().ToString("N") + "/";
        await storage.SaveAsync(prefix + "a", Encoding.UTF8.GetBytes("1"));
        await storage.SaveAsync(prefix + "b", Encoding.UTF8.GetBytes("2"));

        var keys = new List<string>();

        await foreach (var key in storage.ListKeysAsync(prefix))
        {
            keys.Add(key);
        }

        Assert.Equal(2, keys.Count);
        Assert.Contains(prefix + "a", keys);
        Assert.Contains(prefix + "b", keys);
    }
}
