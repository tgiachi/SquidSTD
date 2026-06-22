using System.Text;
using SquidStd.Storage.Services;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Storage;

public class FileStorageServiceTests
{
    [Fact]
    public async Task SaveAsync_LoadAsync_RoundTripsBytes()
    {
        using var temp = new TempDirectory();
        var service = new FileStorageService(new() { RootDirectory = temp.Path });
        var data = Encoding.UTF8.GetBytes("hello storage");

        await service.SaveAsync("profiles/main.bin", data);

        var loaded = await service.LoadAsync("profiles/main.bin");

        Assert.Equal(data, loaded);
        Assert.True(await service.ExistsAsync("profiles/main.bin"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesStoredValue()
    {
        using var temp = new TempDirectory();
        var service = new FileStorageService(new() { RootDirectory = temp.Path });
        await service.SaveAsync("cache/value.bin", new byte[] { 1, 2, 3 });

        var deleted = await service.DeleteAsync("cache/value.bin");

        Assert.True(deleted);
        Assert.False(await service.ExistsAsync("cache/value.bin"));
        Assert.Null(await service.LoadAsync("cache/value.bin"));
    }

    [Theory, InlineData("../escape.bin"), InlineData("/absolute.bin"), InlineData("nested/../../escape.bin")]
    public async Task SaveAsync_RejectsUnsafeKeys(string key)
    {
        using var temp = new TempDirectory();
        var service = new FileStorageService(new() { RootDirectory = temp.Path });

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(key, new byte[] { 1 }).AsTask());
    }

    [Fact]
    public async Task ObjectStorage_SaveAsync_LoadAsync_RoundTripsYamlObject()
    {
        using var temp = new TempDirectory();
        var storage = new FileStorageService(new() { RootDirectory = temp.Path });
        var objects = new YamlObjectStorageService(storage);
        var expected = new SampleObject
        {
            Name = "main",
            Value = 42
        };

        await objects.SaveAsync("objects/sample.yaml", expected);

        var actual = await objects.LoadAsync<SampleObject>("objects/sample.yaml");

        Assert.NotNull(actual);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Value, actual.Value);
    }

    private sealed class SampleObject
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    private static async Task<List<string>> ToListAsync(IAsyncEnumerable<string> source)
    {
        var list = new List<string>();

        await foreach (var item in source)
        {
            list.Add(item);
        }

        return list;
    }

    [Fact]
    public async Task ListKeysAsync_ReturnsSavedKeys_AndFiltersByPrefix()
    {
        using var temp = new TempDirectory();
        var service = new FileStorageService(new() { RootDirectory = temp.Path });
        await service.SaveAsync("a/one.bin", new byte[] { 1 });
        await service.SaveAsync("a/two.bin", new byte[] { 2 });
        await service.SaveAsync("b/three.bin", new byte[] { 3 });

        var all = (await ToListAsync(service.ListKeysAsync())).OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var aOnly = (await ToListAsync(service.ListKeysAsync("a/"))).OrderBy(k => k, StringComparer.Ordinal).ToArray();

        Assert.Equal(new[] { "a/one.bin", "a/two.bin", "b/three.bin" }, all);
        Assert.Equal(new[] { "a/one.bin", "a/two.bin" }, aOnly);
    }

    [Fact]
    public async Task ListKeysAsync_EmptyStore_ReturnsEmpty()
    {
        using var temp = new TempDirectory();
        var service = new FileStorageService(new() { RootDirectory = temp.Path });

        Assert.Empty(await ToListAsync(service.ListKeysAsync()));
    }

    [Fact]
    public async Task ObjectStorage_ListKeysAsync_ReturnsSavedKeys()
    {
        using var temp = new TempDirectory();
        var storage = new FileStorageService(new() { RootDirectory = temp.Path });
        var objects = new YamlObjectStorageService(storage);
        await objects.SaveAsync("objects/x.yaml", new SampleObject { Name = "x", Value = 1 });

        var keys = await ToListAsync(objects.ListKeysAsync("objects/"));

        Assert.Contains("objects/x.yaml", keys);
    }
}
