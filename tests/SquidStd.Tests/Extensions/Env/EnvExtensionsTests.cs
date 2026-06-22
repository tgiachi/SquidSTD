using SquidStd.Core.Extensions.Env;

namespace SquidStd.Tests.Extensions.Env;

public class EnvExtensionsTests
{
    private const string TestVariable = "SQUIDSTD_UNIT_TEST_VAR";

    [Theory, InlineData(""), InlineData("no variables here")]
    public void ExpandEnvironmentVariables_NoMatch_ReturnsInput(string input)
        => Assert.Equal(input, input.ExpandEnvironmentVariables());

    [Fact]
    public void ExpandEnvironmentVariables_ReplacesDollarPrefixedVariable()
    {
        Environment.SetEnvironmentVariable(TestVariable, "resolved");

        try
        {
            var result = $"${TestVariable}/path".ExpandEnvironmentVariables();

            Assert.Equal("resolved/path", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestVariable, null);
        }
    }
}
