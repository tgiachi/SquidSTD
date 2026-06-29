namespace SquidStd.Tui.Interfaces;

/// <summary>Displays and removes views. Abstracts the Terminal.Gui application from the navigator.</summary>
public interface ITuiViewHost
{
    /// <summary>Shows a view as the current screen.</summary>
    void Show(object view);

    /// <summary>Removes a previously shown view.</summary>
    void Remove(object view);
}
