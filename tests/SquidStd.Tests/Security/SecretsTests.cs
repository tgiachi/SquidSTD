using System.Security.Cryptography;
using System.Text;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SquidStd.Core.Data.Storage;
using SquidStd.Services.Core.Services.Storage;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Security;

[Collection(SerilogEventSinkCollection.Name)]
public class SecretsTests
{
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

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly List<LogEvent> _events = [];

        public IReadOnlyList<LogEvent> Events => _events;

        public void Emit(LogEvent logEvent)
            => _events.Add(logEvent);
    }
}
