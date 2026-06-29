using System.Linq.Expressions;

namespace SquidStd.Tui.Dsl;

/// <summary>A label whose text is one-way bound to a string property.</summary>
public sealed class LabelNode<TViewModel> : TuiNode<TViewModel>
{
    /// <summary>The source string property bound to the label text.</summary>
    public Expression<Func<TViewModel, string>> Text { get; }

    public LabelNode(Expression<Func<TViewModel, string>> text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
    }
}
