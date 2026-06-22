using DryIoc;
using SquidStd.Abstractions.Extensions.Container;

namespace SquidStd.Tests.Abstractions;

public class AddTypedListMethodExtensionTests
{
    [Fact]
    public void AddToRegisterTypedList_DistinctTypes_RegisterSeparateLists()
    {
        using var container = new Container();

        container.AddToRegisterTypedList("text");
        container.AddToRegisterTypedList(42);

        Assert.Equal(["text"], container.Resolve<List<string>>());
        Assert.Equal([42], container.Resolve<List<int>>());
    }

    [Fact]
    public void AddToRegisterTypedList_FirstEntry_RegistersNewList()
    {
        using var container = new Container();

        container.AddToRegisterTypedList("a");

        Assert.Equal(["a"], container.Resolve<List<string>>());
    }

    [Fact]
    public void AddToRegisterTypedList_MultipleEntries_AppendsToSameList()
    {
        using var container = new Container();

        container.AddToRegisterTypedList("a");
        container.AddToRegisterTypedList("b");

        Assert.Equal(["a", "b"], container.Resolve<List<string>>());
    }

    [Fact]
    public void AddToRegisterTypedList_ReturnsSameContainerForChaining()
    {
        using var container = new Container();

        var result = container.AddToRegisterTypedList(1);

        Assert.Same(container, result);
    }
}
