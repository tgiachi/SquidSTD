using CommunityToolkit.Mvvm.Input;
using SquidStd.Tui.Binding;

namespace SquidStd.Tests.Tui.Binding;

public class ViewBinderCommandTests
{
    [Fact]
    public void Command_ExecutesTrigger_AndReflectsCanExecute()
    {
        var executed = 0;
        var canRun = false;
        // ReSharper disable once AccessToModifiedClosure
        var command = new RelayCommand(() => executed++, () => canRun);
        var enabled = true;
        Action? trigger = null;
        var binder = new ViewBinder();

        binder.Command(command, isEnabled => enabled = isEnabled, cb => trigger = cb);

        Assert.False(enabled);               // initial CanExecute == false -> disabled

        canRun = true;
        command.NotifyCanExecuteChanged();
        Assert.True(enabled);                // CanExecuteChanged -> enabled

        trigger!();                          // user activates the control
        Assert.Equal(1, executed);
    }

    [Fact]
    public void Command_DoesNotExecuteWhenCanExecuteFalse()
    {
        var executed = 0;
        var command = new RelayCommand(() => executed++, () => false);
        Action? trigger = null;
        var binder = new ViewBinder();
        binder.Command(command, _ => { }, cb => trigger = cb);

        trigger!();

        Assert.Equal(0, executed);
    }
}
