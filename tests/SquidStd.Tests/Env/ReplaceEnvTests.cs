using SquidStd.Core.Extensions.Env;

namespace SquidStd.Tests.Env;

public class ReplaceEnvTests
{
    [Fact]
    public void ReplaceEnv_SubstitutesKnownVariable()
    {
        Environment.SetEnvironmentVariable("SQUID_TEST_VAR", "secret");
        try
        {
            Assert.Equal("pwd=secret;", "pwd=$SQUID_TEST_VAR;".ReplaceEnv());
        }
        finally
        {
            Environment.SetEnvironmentVariable("SQUID_TEST_VAR", null);
        }
    }

    [Fact]
    public void ReplaceEnv_LeavesUnknownVariableUntouched()
    {
        Environment.SetEnvironmentVariable("SQUID_MISSING_VAR", null);
        Assert.Equal("x=$SQUID_MISSING_VAR", "x=$SQUID_MISSING_VAR".ReplaceEnv());
    }

    [Fact]
    public void ReplaceEnv_SubstitutesMultipleTokens()
    {
        Environment.SetEnvironmentVariable("SQUID_A", "1");
        Environment.SetEnvironmentVariable("SQUID_B", "2");
        try
        {
            Assert.Equal("1-2", "$SQUID_A-$SQUID_B".ReplaceEnv());
        }
        finally
        {
            Environment.SetEnvironmentVariable("SQUID_A", null);
            Environment.SetEnvironmentVariable("SQUID_B", null);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no tokens here")]
    public void ReplaceEnv_PassesThroughWhenNothingToReplace(string? input)
    {
        Assert.Equal(input, input!.ReplaceEnv());
    }
}
