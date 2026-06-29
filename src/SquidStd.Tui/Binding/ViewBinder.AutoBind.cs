using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using SquidStd.Tui.Internal;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace SquidStd.Tui.Binding;

public sealed partial class ViewBinder
{
    /// <summary>
    /// Convention binding: for each named subview, binds it to a matching ViewModel member by name
    /// (e.g. <c>NameField</c> ↔ <c>Name</c>, <c>SaveButton</c> ↔ <c>SaveCommand</c>). Explicit bindings
    /// declared before AutoBind win; AutoBind only fills the gaps it recognises (Label, TextField, Button).
    /// </summary>
    public void AutoBind(View view, INotifyPropertyChanged viewModel)
    {
        var vmType = viewModel.GetType();

        foreach (var subview in view.SubViews)
        {
            var id = subview.Id;

            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            switch (subview)
            {
                case Button button:
                    BindCommandByName(vmType, viewModel, ConventionNames.CommandName(id), button);
                    break;
                case TextField field:
                    BindStringByName(vmType, viewModel, ConventionNames.MemberName(id), field, twoWay: true, null);
                    break;
                case Label label:
                    BindStringByName(vmType, viewModel, ConventionNames.MemberName(id), null, twoWay: false, label);
                    break;
            }
        }
    }

    private void BindCommandByName(Type vmType, INotifyPropertyChanged vm, string memberName, Button button)
    {
        var property = vmType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);

        if (property is not null && property.GetValue(vm) is ICommand command)
        {
            Command(button, command);
        }
    }

    private void BindStringByName(
        Type vmType, INotifyPropertyChanged vm, string memberName, TextField? field, bool twoWay, Label? label
    )
    {
        var property = vmType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);

        if (property is null || property.PropertyType != typeof(string))
        {
            return;
        }

        if (twoWay && field is not null)
        {
            TwoWay(
                vm,
                memberName,
                () => field.Text = (string)(property.GetValue(vm) ?? string.Empty),
                callback => field.ValueChanged += (_, _) => callback(),
                () => property.SetValue(vm, field.Text)
            );
        }
        else if (label is not null)
        {
            OneWay(vm, memberName, () => label.Text = (string)(property.GetValue(vm) ?? string.Empty));
        }
    }
}
