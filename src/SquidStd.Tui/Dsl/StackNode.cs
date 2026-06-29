using SquidStd.Tui.Types.Tui;

namespace SquidStd.Tui.Dsl;

/// <summary>A container that arranges its children vertically or horizontally.</summary>
public sealed class StackNode<TViewModel> : TuiNode<TViewModel>
{
    /// <summary>The direction children are arranged.</summary>
    public StackOrientation Orientation { get; }

    /// <summary>The child nodes, in arrangement order.</summary>
    public IReadOnlyList<TuiNode<TViewModel>> Children { get; }

    public StackNode(StackOrientation orientation, IReadOnlyList<TuiNode<TViewModel>> children)
    {
        ArgumentNullException.ThrowIfNull(children);

        Orientation = orientation;
        Children = children;
    }
}
