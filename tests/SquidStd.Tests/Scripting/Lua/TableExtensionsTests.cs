using MoonSharp.Interpreter;
using SquidStd.Scripting.Lua.Extensions.Scripts;

namespace SquidStd.Tests.Scripting.Lua;

public class TableExtensionsTests
{
    [Fact]
    public void ToProxy_DelegatesInterfaceCallsToLuaFunctions()
    {
        var script = new Script();
        var table = script.DoString(
            """
            return {
                Sum = function(left, right)
                    return left + right
                end
            }
            """
        ).Table;

        var proxy = table.ToProxy<ICalculator>();

        Assert.Equal(7, proxy.Sum(3, 4));
    }

    [Fact]
    public void ToProxy_MissingFunctionThrowsMissingMethodException()
    {
        var proxy = new Table(new Script()).ToProxy<ICalculator>();

        Assert.Throws<MissingMethodException>(() => proxy.Sum(1, 2));
    }

    public interface ICalculator
    {
        int Sum(int left, int right);
    }
}
