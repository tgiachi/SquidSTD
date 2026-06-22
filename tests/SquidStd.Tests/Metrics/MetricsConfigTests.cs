using SquidStd.Core.Data.Metrics;
using SquidStd.Core.Interfaces.Config;

namespace SquidStd.Tests.Metrics;

public class MetricsConfigTests
{
    [Fact]
    public void MetricsConfig_ImplementsConfigEntry()
    {
        IConfigEntry entry = new MetricsConfig();

        Assert.Equal("metrics", entry.SectionName);
        Assert.Equal(typeof(MetricsConfig), entry.ConfigType);
        Assert.IsType<MetricsConfig>(entry.CreateDefault());
    }
}
