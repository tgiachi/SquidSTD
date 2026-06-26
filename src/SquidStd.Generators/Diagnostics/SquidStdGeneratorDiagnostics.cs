using Microsoft.CodeAnalysis;

namespace SquidStd.Generators.Diagnostics;

internal static class SquidStdGeneratorDiagnostics
{
    public static readonly DiagnosticDescriptor UnsupportedEventListener = new(
        "SQDGEN001",
        "Event listener cannot be generated",
        "Event listener '{0}' must be a non-generic public or internal class with a public or internal event type",
        "SquidStd.Generators",
        DiagnosticSeverity.Warning,
        true
    );
}
