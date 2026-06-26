using Microsoft.CodeAnalysis;

namespace SquidStd.Generators.Events;

internal sealed class EventListenerCandidate
{
    public EventListenerCandidate(
        string eventTypeName,
        string listenerTypeName,
        string displayName,
        Location? location,
        bool isSupported
    )
    {
        EventTypeName = eventTypeName;
        ListenerTypeName = listenerTypeName;
        DisplayName = displayName;
        Location = location;
        IsSupported = isSupported;
    }

    public string EventTypeName { get; }

    public string ListenerTypeName { get; }

    public string DisplayName { get; }

    public Location? Location { get; }

    public bool IsSupported { get; }
}
