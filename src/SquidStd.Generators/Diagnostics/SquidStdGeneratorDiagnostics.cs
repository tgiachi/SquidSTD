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

    public static readonly DiagnosticDescriptor UnsupportedStdService = new(
        "SQDGEN002",
        "Standard service cannot be generated",
        "Standard service '{0}' must be a non-generic public or internal class assignable to a public or internal service contract",
        "SquidStd.Generators",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor UnsupportedConfigSection = new(
        "SQDGEN003",
        "Config section cannot be generated",
        "Config section '{0}' must be a non-generic public or internal class with a public parameterless constructor and a non-empty section name",
        "SquidStd.Generators",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor UnsupportedJobHandler = new(
        "SQDGEN004",
        "Job handler cannot be generated",
        "Job handler '{0}' must be a non-generic public or internal class implementing IJobHandler",
        "SquidStd.Generators",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor UnsupportedScriptModule = new(
        "SQDGEN005",
        "Script module cannot be generated",
        "Script module '{0}' must be a non-generic public or internal class",
        "SquidStd.Generators",
        DiagnosticSeverity.Warning,
        true
    );
}
