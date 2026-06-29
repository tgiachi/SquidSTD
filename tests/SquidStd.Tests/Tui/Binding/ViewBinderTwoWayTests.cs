using System.ComponentModel;
using SquidStd.Tui.Binding;

namespace SquidStd.Tests.Tui.Binding;

public class ViewBinderTwoWayTests
{
    private sealed class FakeViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;

        public string Name
        {
            get { return _name; }
            set
            {
                if (string.Equals(_name, value, StringComparison.Ordinal))
                {
                    return;
                }

                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class FakeField
    {
        private string _text = string.Empty;

        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        public event Action? Changed;

        public void UserTypes(string value)
        {
            _text = value;
            Changed?.Invoke();
        }
    }

    [Fact]
    public void TwoWay_PropagatesBothDirections_WithoutLooping()
    {
        var vm = new FakeViewModel { Name = "init" };
        var field = new FakeField();
        var binder = new ViewBinder();

        binder.TwoWay(
            vm, nameof(FakeViewModel.Name),
            applyToTarget: () => field.Text = vm.Name,
            subscribeTargetChanged: cb => field.Changed += () => cb(),
            writeToSource: () => vm.Name = field.Text
        );

        Assert.Equal("init", field.Text);

        vm.Name = "from-vm";
        Assert.Equal("from-vm", field.Text);

        field.UserTypes("from-ui");
        Assert.Equal("from-ui", vm.Name);
    }

    [Fact]
    public void TwoWay_Typed_BindsViaExpressions()
    {
        var vm = new FakeViewModel { Name = "a" };
        var field = new FakeField();
        var binder = new ViewBinder();

        binder.TwoWay(vm, x => x.Name, field, f => f.Text, cb => field.Changed += () => cb());

        vm.Name = "b";
        Assert.Equal("b", field.Text);

        field.UserTypes("c");
        Assert.Equal("c", vm.Name);
    }
}
