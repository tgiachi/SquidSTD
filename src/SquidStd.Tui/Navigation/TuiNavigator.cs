using DryIoc;
using SquidStd.Tui.Interfaces;
using SquidStd.Tui.Internal;

namespace SquidStd.Tui.Navigation;

/// <summary>Stack-based ViewModel-first navigator: resolves the ViewModel and its view from the container.</summary>
public sealed class TuiNavigator : ITuiNavigator
{
    private readonly IResolver _resolver;
    private readonly TuiViewRegistry _registry;
    private readonly ITuiViewHost _viewHost;
    private readonly Stack<Screen> _stack = new();

    public int Depth
    {
        get { return _stack.Count; }
    }

    public TuiNavigator(IResolver resolver, TuiViewRegistry registry, ITuiViewHost viewHost)
    {
        _resolver = resolver;
        _registry = registry;
        _viewHost = viewHost;
    }

    public async Task NavigateToAsync<TViewModel>(CancellationToken cancellationToken = default)
        where TViewModel : TuiViewModel
    {
        var viewModel = (TViewModel)_resolver.Resolve(typeof(TViewModel));
        viewModel.Navigator = this;

        var viewType = _registry.ViewTypeFor(typeof(TViewModel));
        var view = (ITuiView)_resolver.Resolve(viewType);
        view.Bind(viewModel);
        view.Initialize();

        _stack.Push(new Screen(viewModel, view));
        _viewHost.Show(view);

        await viewModel.OnActivatedAsync();
    }

    public async Task BackAsync(CancellationToken cancellationToken = default)
    {
        if (_stack.Count <= 1)
        {
            return;
        }

        var current = _stack.Pop();
        _viewHost.Remove(current.View);
        await current.ViewModel.OnDeactivatedAsync();

        var beneath = _stack.Peek();
        await beneath.ViewModel.OnActivatedAsync();
    }

    private sealed class Screen
    {
        public TuiViewModel ViewModel { get; }
        public ITuiView View { get; }

        public Screen(TuiViewModel viewModel, ITuiView view)
        {
            ViewModel = viewModel;
            View = view;
        }
    }
}
