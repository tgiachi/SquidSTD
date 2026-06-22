using SquidStd.Scripting.Lua.Data.Scripts;

namespace SquidStd.Tests.Scripting.Lua;

public class ScriptResultBuilderTests
{
    [Fact]
    public void CreateSuccess_BuildsSuccessfulResult()
    {
        var result = ScriptResultBuilder.CreateSuccess()
                                        .WithMessage("ok")
                                        .WithData(42)
                                        .Build();

        Assert.True(result.Success);
        Assert.Equal("ok", result.Message);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public void CreateError_BuildsFailedResult()
    {
        var result = ScriptResultBuilder.CreateError()
                                        .WithMessage("failed")
                                        .Build();

        Assert.False(result.Success);
        Assert.Equal("failed", result.Message);
        Assert.Null(result.Data);
    }
}
