# Terminal UI (MVVM)

Build a Counter terminal app with an observable ViewModel, then display it two ways — the
imperative `TuiView<T>` and the declarative `TuiComposedView<T>` DSL.

## What you'll build

A small Counter app where a `CounterViewModel` holds a `Value` property and an `IncrementCommand`.
You will see both ways to build the view:

1. **Imperative** — `TuiView<T>`: override `BuildLayout` to add Terminal.Gui widgets, then
   override `Bind` to wire them through `ViewBinder`.
2. **Declarative** — `TuiComposedView<T>`: override `Compose` and return a `TuiNode` tree built
   with the `Ui.*` DSL; bindings and layout are inferred from the node types.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Tui`

## Steps

### 1. Define the ViewModel

Derive from `TuiViewModel` (which extends `ObservableObject` from CommunityToolkit.Mvvm) and mark
observable properties and relay commands with the standard source-generator attributes.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SquidStd.Tui.ViewModels;

public sealed partial class CounterViewModel : TuiViewModel
{
    [ObservableProperty]
    private string _title = "Counter";

    [ObservableProperty]
    private string _value = "0";

    [RelayCommand]
    private void Increment() => Value = (int.Parse(Value) + 1).ToString();
}
```

### 2. Imperative view — `TuiView<T>`

Override `BuildLayout` to create and add Terminal.Gui widgets, then override `Bind` to wire them
through `ViewBinder`. The binder tracks property-changed notifications and updates widgets without
polling.

```csharp
using Terminal.Gui;
using SquidStd.Tui.Views;
using SquidStd.Tui.Binders;

public sealed class CounterView : TuiView<CounterViewModel>
{
    private Label _value = null!;
    private Button _inc = null!;

    protected override void BuildLayout()
    {
        _value = new Label { X = 1, Y = 1 };
        _inc   = new Button { X = 1, Y = 3, Text = "+1" };
        Add(_value, _inc);
    }

    protected override void Bind(ViewBinder b)
    {
        b.OneWayTitle(ViewModel, x => x.Title, this);
        b.OneWayText(ViewModel, x => x.Value, _value);
        b.Command(_inc, ViewModel.IncrementCommand);
    }
}
```

### 3. Declarative view — `TuiComposedView<T>`

Instead of `BuildLayout` + `Bind`, derive from `TuiComposedView<T>` and return a node tree from
`Compose`. The framework infers binding direction from the node type (Label → one-way, TextField →
two-way, Button → command) and applies layout automatically.

```csharp
using SquidStd.Tui.Views;
using SquidStd.Tui.Dsl;

public sealed class CounterView : TuiComposedView<CounterViewModel>
{
    protected override TuiNode<CounterViewModel> Compose() =>
        Ui.VStack(
            Ui.Label(x => x.Title),
            Ui.Label(x => x.Value),
            Ui.Button("+1", x => x.IncrementCommand));
}
```

`Ui.VStack` / `Ui.HStack` stack children vertically or horizontally and auto-assign `Y`/`X`
offsets. `Ui.TextField(x => x.Name)` is two-way by default; pass `BindMode.OneWay` to override.

### 4. Register and run

`RegisterTui()` adds the core TUI services and `TuiApplicationHost` to the DryIoc container.
`RegisterView<TView, TViewModel>()` registers the view for ViewModel-first resolution.
`TuiApplicationHost.RunAsync<TViewModel>()` resolves the root ViewModel, shows its view, and starts
the Terminal.Gui event loop.

```csharp
using DryIoc;
using SquidStd.Tui.Extensions;
using SquidStd.Tui.Hosts;

var container = new Container().RegisterTui();
container.RegisterView<CounterView, CounterViewModel>();

await container.Resolve<TuiApplicationHost>().RunAsync<CounterViewModel>();
```

### 5. ViewModel-first navigation

Push a new ViewModel onto the navigation stack from inside an existing ViewModel via the injected
`ITuiNavigator`:

```csharp
// inside CounterViewModel
await Navigator.NavigateToAsync<DetailViewModel>();
await Navigator.BackAsync();
```

The navigator resolves the view that was registered for `DetailViewModel` and shows it; `BackAsync`
pops the stack and restores the previous view.

## Next steps

- [SquidStd.Tui reference](../articles/tui.md)
