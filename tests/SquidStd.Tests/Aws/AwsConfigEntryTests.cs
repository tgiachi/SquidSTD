using SquidStd.Aws.Abstractions.Data.Config;

namespace SquidStd.Tests.Aws;

public class AwsConfigEntryTests
{
    [Fact]
    public void Defaults_RegionIsUsEast1_AndCredentialsAreNull()
    {
        var entry = new AwsConfigEntry();

        Assert.Equal("us-east-1", entry.Region);
        Assert.Null(entry.AccessKey);
        Assert.Null(entry.SecretKey);
        Assert.Null(entry.SessionToken);
        Assert.Null(entry.ServiceUrl);
    }

    [Fact]
    public void InitProperties_AreRetained()
    {
        var entry = new AwsConfigEntry
        {
            Region = "eu-west-1",
            AccessKey = "ak",
            SecretKey = "sk",
            SessionToken = "st",
            ServiceUrl = "http://localhost:4566"
        };

        Assert.Equal("eu-west-1", entry.Region);
        Assert.Equal("ak", entry.AccessKey);
        Assert.Equal("sk", entry.SecretKey);
        Assert.Equal("st", entry.SessionToken);
        Assert.Equal("http://localhost:4566", entry.ServiceUrl);
    }
}
