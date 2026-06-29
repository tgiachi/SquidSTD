namespace SquidStd.Tui.Interfaces;

/// <summary>Non-generic handle the navigator uses to bind a ViewModel and initialise a view.</summary>
public interface ITuiView
{
    /// <summary>Assigns the ViewModel instance to the view.</summary>
    void Bind(object viewModel);

    /// <summary>Builds the layout and declares bindings. Called once after <see cref="Bind" />.</summary>
    void Initialize();
}
