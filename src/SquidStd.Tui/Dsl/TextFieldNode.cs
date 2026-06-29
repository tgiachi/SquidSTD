using System.Linq.Expressions;
using SquidStd.Tui.Types.Tui;

namespace SquidStd.Tui.Dsl;

/// <summary>An editable text field bound to a string property (two-way by default).</summary>
public sealed class TextFieldNode<TViewModel> : TuiNode<TViewModel>
{
    /// <summary>The source string property bound to the field text.</summary>
    public Expression<Func<TViewModel, string>> Text { get; }

    /// <summary>The binding direction.</summary>
    public BindMode Mode { get; }

    public TextFieldNode(Expression<Func<TViewModel, string>> text, BindMode mode)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
        Mode = mode;
    }
}
