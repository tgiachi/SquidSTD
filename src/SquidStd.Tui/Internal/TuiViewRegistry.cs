namespace SquidStd.Tui.Internal;

/// <summary>Maps a ViewModel type to the View type that renders it.</summary>
public sealed class TuiViewRegistry
{
    private readonly Dictionary<Type, Type> _map = new();

    public void Map(Type viewModelType, Type viewType)
    {
        _map[viewModelType] = viewType;
    }

    public Type ViewTypeFor(Type viewModelType)
    {
        if (_map.TryGetValue(viewModelType, out var viewType))
        {
            return viewType;
        }

        throw new InvalidOperationException($"No view registered for ViewModel '{viewModelType.Name}'.");
    }
}
