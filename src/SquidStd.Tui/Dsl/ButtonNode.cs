using System.Linq.Expressions;
using System.Windows.Input;

namespace SquidStd.Tui.Dsl;

/// <summary>A button whose activation runs a command resolved from the ViewModel.</summary>
public sealed class ButtonNode<TViewModel> : TuiNode<TViewModel>
{
    /// <summary>The button caption.</summary>
    public string Caption { get; }

    /// <summary>The source property providing the command to run.</summary>
    public Expression<Func<TViewModel, ICommand>> Command { get; }

    public ButtonNode(string caption, Expression<Func<TViewModel, ICommand>> command)
    {
        ArgumentException.ThrowIfNullOrEmpty(caption);
        ArgumentNullException.ThrowIfNull(command);

        Caption = caption;
        Command = command;
    }
}
