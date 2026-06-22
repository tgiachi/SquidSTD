using System.Security.Cryptography;
using System.Text;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SquidStd.Core.Data.Storage;
using SquidStd.Services.Core.Services.Storage;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Services.Core.Storage;

[Collection(SerilogEventSinkCollection.Name)]
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

    [Fact]
    public void AesGcmSecretProtector_Protect_Unprotect_RoundTripsWithoutPlaintext()
    {
        var variableName = "SQUIDSTD_TEST_SECRET_KEY";
        var previous = Environment.GetEnvironmentVariable(variableName);
        var key = RandomNumberGenerator.GetBytes(32);

        try
        {
            Environment.SetEnvironmentVariable(variableName, Convert.ToBase64String(key));
            var protector = new AesGcmSecretProtector(new() { KeyEnvironmentVariable = variableName });
            var plaintext = Encoding.UTF8.GetBytes("super-secret-value");

            var protectedData = protector.Protect(plaintext);
            var unprotected = protector.Unprotect(protectedData);

            Assert.Equal(plaintext, unprotected);
            Assert.DoesNotContain("super-secret-value", Encoding.UTF8.GetString(protectedData));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previous);
        }
    }

    [Fact]
    public void AesGcmSecretProtector_WhenKeyEnvironmentVariableIsMissing_UsesDefaultKeyAndLogsWarning()
    {
        var variableName = "SQUIDSTD_TEST_SECRET_KEY_MISSING";
        var previous = Environment.GetEnvironmentVariable(variableName);
        var previousLogger = Log.Logger;
        var sink = new CapturingSink();

        try
        {
            Environment.SetEnvironmentVariable(variableName, null);
            Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(sink).CreateLogger();
            var protector = new AesGcmSecretProtector(new() { KeyEnvironmentVariable = variableName });
            var plaintext = Encoding.UTF8.GetBytes("default-key-secret");

            var protectedData = protector.Protect(plaintext);
            var unprotected = protector.Unprotect(protectedData);

            Assert.Equal(plaintext, unprotected);
            Assert.Contains(
                sink.Events,
                logEvent => logEvent.Level == LogEventLevel.Warning &&
                            logEvent.RenderMessage().Contains(variableName, StringComparison.Ordinal)
            );
        }
        finally
        {
            Log.Logger = previousLogger;
            Environment.SetEnvironmentVariable(variableName, previous);
        }
    }

    [Fact]
    public async Task FileSecretStore_SetAsync_GetAsync_StoresEncryptedPayload()
    {
        using var temp = new TempDirectory();
        var variableName = "SQUIDSTD_TEST_SECRET_STORE_KEY";
        var previous = Environment.GetEnvironmentVariable(variableName);
        var key = RandomNumberGenerator.GetBytes(32);

        try
        {
            Environment.SetEnvironmentVariable(variableName, Convert.ToBase64String(key));
            var config = new SecretsConfig
            {
                RootDirectory = temp.Path,
                KeyEnvironmentVariable = variableName
            };
            var store = new FileSecretStore(config, new AesGcmSecretProtector(config));

            await store.SetAsync("db/main-password", "super-secret-value");

            var value = await store.GetAsync("db/main-password");
            var files = Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories);
            var rawPayload = string.Join(Environment.NewLine, files.Select(File.ReadAllText));

            Assert.Equal("super-secret-value", value);
            Assert.True(await store.ExistsAsync("db/main-password"));
            Assert.DoesNotContain("super-secret-value", rawPayload);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previous);
        }
    }

    private sealed class SampleObject
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly List<LogEvent> _events = [];

        public IReadOnlyList<LogEvent> Events => _events;

        public void Emit(LogEvent logEvent)
            => _events.Add(logEvent);
    }
}
