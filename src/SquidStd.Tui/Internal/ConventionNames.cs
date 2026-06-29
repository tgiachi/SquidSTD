namespace SquidStd.Tui.Internal;

/// <summary>Maps a widget id to candidate ViewModel member names for convention binding.</summary>
internal static class ConventionNames
{
    private static readonly string[] _suffixes = ["Field", "Label", "Button", "Text", "View", "Box", "List"];

    public static string MemberName(string widgetId)
    {
        foreach (var suffix in _suffixes)
        {
            if (widgetId.Length > suffix.Length && widgetId.EndsWith(suffix, StringComparison.Ordinal))
            {
                return widgetId[..^suffix.Length];
            }
        }

        return widgetId;
    }

    public static string CommandName(string widgetId)
    {
        return MemberName(widgetId) + "Command";
    }
}
