using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using SquidStd.Tui.Dsl;
using SquidStd.Tui.Internal;
using SquidStd.Tui.Types.Tui;

namespace SquidStd.Tests.Tui.Dsl;

public class TuiNodeTests
{
    private sealed class SampleViewModel
    {
        public string Title { get; set; } = string.Empty;
        public ICommand Save { get; } = new RelayCommand(() => { });
    }

    [Fact]
    public void LabelNode_CapturesTextExpression()
    {
        var node = new LabelNode<SampleViewModel>(x => x.Title);

        Assert.Equal(nameof(SampleViewModel.Title), PropertyPath.NameOf(node.Text));
    }

    [Fact]
    public void TextFieldNode_DefaultsToTwoWay()
    {
        var node = new TextFieldNode<SampleViewModel>(x => x.Title, BindMode.TwoWay);

        Assert.Equal(BindMode.TwoWay, node.Mode);
        Assert.Equal(nameof(SampleViewModel.Title), PropertyPath.NameOf(node.Text));
    }

    [Fact]
    public void ButtonNode_CapturesCaptionAndCommand()
    {
        var node = new ButtonNode<SampleViewModel>("Save", x => x.Save);

        Assert.Equal("Save", node.Caption);
        Assert.NotNull(node.Command);
    }

    [Fact]
    public void StackNode_HoldsOrientationAndChildren()
    {
        var child = new LabelNode<SampleViewModel>(x => x.Title);
        var node = new StackNode<SampleViewModel>(StackOrientation.Vertical, new TuiNode<SampleViewModel>[] { child });

        Assert.Equal(StackOrientation.Vertical, node.Orientation);
        Assert.Single(node.Children);
        Assert.Same(child, node.Children[0]);
    }
}
