using Microsoft.CodeAnalysis;

namespace SquidStd.Generators.Workers;

internal sealed class JobHandlerRegistrationCandidate
{
    public string HandlerTypeName { get; }

    public string DisplayName { get; }

    public Location? Location { get; }

    public bool IsSupported { get; }

    public JobHandlerRegistrationCandidate(
        string handlerTypeName,
        string displayName,
        Location? location,
        bool isSupported
    )
    {
        HandlerTypeName = handlerTypeName;
        DisplayName = displayName;
        Location = location;
        IsSupported = isSupported;
    }
}
