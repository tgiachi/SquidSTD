using SquidStd.Core.Utils;

namespace SquidStd.Tests.Utils;

public class StringUtilsTests
{
    [Theory, InlineData(""), InlineData(null)]
    public void ToCamelCase_NullOrEmpty_ReturnsEmpty(string? input)
        => Assert.Equal("", StringUtils.ToCamelCase(input!));

    [Fact]
    public void ToCamelCase_SingleCharacter_ReturnsLowerCase()
        => Assert.Equal("a", StringUtils.ToCamelCase("A"));

    [Theory, InlineData("HelloWorld", "helloWorld"), InlineData("API_RESPONSE", "apiResponse"),
     InlineData("user-id", "userId"), InlineData("hello world", "helloWorld")]
    public void ToCamelCase_VariousInputs_ReturnsCamelCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToCamelCase(input));

    [Theory, InlineData("HelloWorld", "hello.world"), InlineData("API_RESPONSE", "api.response")]
    public void ToDotCase_VariousInputs_ReturnsDotCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToDotCase(input));

    [Theory, InlineData("HelloWorld", "hello-world"), InlineData("API_RESPONSE", "api-response"),
     InlineData("userId", "user-id")]
    public void ToKebabCase_VariousInputs_ReturnsKebabCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToKebabCase(input));

    [Fact]
    public void ToPascalCase_SingleCharacter_ReturnsUpperCase()
        => Assert.Equal("A", StringUtils.ToPascalCase("a"));

    [Theory, InlineData("hello_world", "HelloWorld"), InlineData("api-response", "ApiResponse"),
     InlineData("userId", "UserId")]
    public void ToPascalCase_VariousInputs_ReturnsPascalCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToPascalCase(input));

    [Theory, InlineData("HelloWorld", "hello/world"), InlineData("API_RESPONSE", "api/response")]
    public void ToPathCase_VariousInputs_ReturnsPathCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToPathCase(input));

    [Theory, InlineData("hello world", "Hello world"), InlineData("API_RESPONSE", "Api response")]
    public void ToSentenceCase_VariousInputs_ReturnsSentenceCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToSentenceCase(input));

    [Theory, InlineData(""), InlineData(null)]
    public void ToSnakeCase_NullOrEmpty_ReturnsEmpty(string? input)
        => Assert.Equal("", StringUtils.ToSnakeCase(input!));

    [Theory, InlineData("HelloWorld", "hello_world"), InlineData("APIResponse", "api_response"),
     InlineData("userId", "user_id")]
    public void ToSnakeCase_VariousInputs_ReturnsSnakeCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToSnakeCase(input));

    [Theory, InlineData("hello_world", "Hello World"), InlineData("API_RESPONSE", "Api Response"),
     InlineData("user-id", "User Id")]
    public void ToTitleCase_VariousInputs_ReturnsTitleCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToTitleCase(input));

    [Theory, InlineData("hello_world", "Hello-World"), InlineData("apiResponse", "Api-Response")]
    public void ToTrainCase_VariousInputs_ReturnsTrainCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToTrainCase(input));

    [Theory, InlineData("HelloWorld", "HELLO_WORLD"), InlineData("apiResponse", "API_RESPONSE"),
     InlineData("user-id", "USER_ID")]
    public void ToUpperSnakeCase_VariousInputs_ReturnsScreamingSnakeCase(string input, string expected)
        => Assert.Equal(expected, StringUtils.ToUpperSnakeCase(input));
}
