using Microsoft.CodeAnalysis;

namespace SquidStd.Generators.Services;

internal sealed class StdServiceRegistrationCandidate
{
    public string ServiceTypeName { get; }

    public string ImplementationTypeName { get; }

    public string DisplayName { get; }

    public Location? Location { get; }

    public int Priority { get; }

    public bool IsSupported { get; }

    public StdServiceRegistrationCandidate(
        string serviceTypeName,
        string implementationTypeName,
        string displayName,
        Location? location,
        int priority,
        bool isSupported
    )
    {
        ServiceTypeName = serviceTypeName;
        ImplementationTypeName = implementationTypeName;
        DisplayName = displayName;
        Location = location;
        Priority = priority;
        IsSupported = isSupported;
    }
}
