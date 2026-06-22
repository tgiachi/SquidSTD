using SquidStd.Core.Extensions.Strings;

namespace SquidStd.Tests.Extensions.Strings;

public class StringMethodExtensionTests
{
    [Fact]
    public void ToCamelCase_DelegatesToStringUtils()
        => Assert.Equal("helloWorld", "HelloWorld".ToCamelCase());

    [Fact]
    public void ToDotCase_DelegatesToStringUtils()
        => Assert.Equal("hello.world", "HelloWorld".ToDotCase());

    [Fact]
    public void ToKebabCase_DelegatesToStringUtils()
        => Assert.Equal("hello-world", "HelloWorld".ToKebabCase());

    [Fact]
    public void ToPascalCase_DelegatesToStringUtils()
        => Assert.Equal("HelloWorld", "hello_world".ToPascalCase());

    [Fact]
    public void ToPathCase_DelegatesToStringUtils()
        => Assert.Equal("hello/world", "HelloWorld".ToPathCase());

    [Fact]
    public void ToSentenceCase_DelegatesToStringUtils()
        => Assert.Equal("Hello world", "hello world".ToSentenceCase());

    [Fact]
    public void ToSnakeCase_DelegatesToStringUtils()
        => Assert.Equal("hello_world", "HelloWorld".ToSnakeCase());

    [Fact]
    public void ToSnakeCaseUpper_DelegatesToStringUtils()
        => Assert.Equal("HELLO_WORLD", "HelloWorld".ToSnakeCaseUpper());

    [Fact]
    public void ToTitleCase_DelegatesToStringUtils()
        => Assert.Equal("Hello World", "hello_world".ToTitleCase());

    [Fact]
    public void ToTrainCase_DelegatesToStringUtils()
        => Assert.Equal("Hello-World", "hello_world".ToTrainCase());
}
