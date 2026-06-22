using SquidStd.Core.Json;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Json;

public class JsonContextTypeResolverTests
{
    [Fact]
    public void GetRegisteredTypes_ReturnsAllSerializableTypes()
    {
        var types = JsonContextTypeResolver.GetRegisteredTypes(TestJsonContext.Default).ToList();

        Assert.Contains(typeof(SampleDto), types);
        Assert.Contains(typeof(OtherDto), types);
    }

    [Fact]
    public void GetRegisteredTypesGeneric_FiltersByBaseType()
    {
        var types = JsonContextTypeResolver.GetRegisteredTypes<SampleDto>(TestJsonContext.Default).ToList();

        Assert.Contains(typeof(SampleDto), types);
        Assert.DoesNotContain(typeof(OtherDto), types);
    }

    [Fact]
    public void GetRegisteredTypesWithInfo_ReturnsEntriesForAllTypes()
    {
        var map = JsonContextTypeResolver.GetRegisteredTypesWithInfo(TestJsonContext.Default);

        Assert.True(map.ContainsKey(typeof(SampleDto)));
        Assert.True(map.ContainsKey(typeof(OtherDto)));
    }

    [Fact]
    public void GetTypeInfo_RegisteredType_ReturnsTypeInfo()
    {
        var typeInfo = JsonContextTypeResolver.GetTypeInfo<SampleDto>(TestJsonContext.Default);

        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(SampleDto), typeInfo.Type);
    }

    [Fact]
    public void IsTypeRegistered_RegisteredType_ReturnsTrue()
        => Assert.True(JsonContextTypeResolver.IsTypeRegistered(TestJsonContext.Default, typeof(SampleDto)));

    [Fact]
    public void IsTypeRegistered_UnregisteredType_ReturnsFalse()
        => Assert.False(
            JsonContextTypeResolver.IsTypeRegistered(TestJsonContext.Default, typeof(JsonContextTypeResolverTests))
        );
}
