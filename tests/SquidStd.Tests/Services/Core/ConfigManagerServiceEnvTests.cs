using DryIoc;
using SquidStd.Abstractions.Extensions.Config;
using SquidStd.Database.Abstractions.Data.Database;
using SquidStd.Services.Core.Services;

namespace SquidStd.Tests.Services.Core;

public class ConfigManagerServiceEnvTests
{
    [Fact]
    public void Load_SubstitutesEnvTokensInStringProperties()
    {
        var dir = Path.Combine(Path.GetTempPath(), "squidstd-cfg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Environment.SetEnvironmentVariable("SQUID_DB_PASS", "p@ss");
        try
        {
            File.WriteAllText(
                Path.Combine(dir, "app.yaml"),
                "database:\n  ConnectionString: postgres://u:$SQUID_DB_PASS@h:5432/db\n  AutoMigrate: true\n");

            var container = new Container();
            container.RegisterConfigSection<DatabaseConfig>("database");
            var service = new ConfigManagerService(container, "app", dir);

            service.Load();

            var config = container.Resolve<DatabaseConfig>();
            Assert.Equal("postgres://u:p@ss@h:5432/db", config.ConnectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SQUID_DB_PASS", null);
            Directory.Delete(dir, recursive: true);
        }
    }
}
