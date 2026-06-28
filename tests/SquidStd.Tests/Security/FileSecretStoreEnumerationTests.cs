using System.Security.Cryptography;
using SquidStd.Core.Data.Storage;
using SquidStd.Services.Core.Services.Storage;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Security;

public class FileSecretStoreEnumerationTests
{
    [Fact]
    public async Task ListNamesAsync_ReturnsSetNames_AndHonoursPrefix()
    {
        using var temp = new TempDirectory();
        var variableName = "SQUIDSTD_TEST_SECRET_ENUM_KEY";
        var previous = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            var config = new SecretsConfig { RootDirectory = temp.Path, KeyEnvironmentVariable = variableName };
            var store = new FileSecretStore(config, new AesGcmSecretProtector(config));

            await store.SetAsync("db/main", "a");
            await store.SetAsync("db/replica", "b");
            await store.SetAsync("api/key", "c");

            var all = new List<string>();
            await foreach (var name in store.ListNamesAsync())
            {
                all.Add(name);
            }

            var db = new List<string>();
            await foreach (var name in store.ListNamesAsync("db/"))
            {
                db.Add(name);
            }

            Assert.Equal(["api/key", "db/main", "db/replica"], all.OrderBy(x => x).ToArray());
            Assert.Equal(["db/main", "db/replica"], db.OrderBy(x => x).ToArray());
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previous);
        }
    }
}
