using SquidStd.Tui.Internal;

namespace SquidStd.Tests.Tui.Internal;

public class ConventionNamesTests
{
    [Theory]
    [InlineData("NameField", "Name")]
    [InlineData("TitleLabel", "Title")]
    [InlineData("SaveButton", "Save")]
    [InlineData("Name", "Name")]
    public void MemberName_StripsKnownWidgetSuffixes(string widgetId, string expected)
    {
        Assert.Equal(expected, ConventionNames.MemberName(widgetId));
    }

    [Fact]
    public void CommandName_AppendsCommandSuffix()
    {
        Assert.Equal("SaveCommand", ConventionNames.CommandName("SaveButton"));
    }
}
