using System.Diagnostics;

namespace SquidStd.Telemetry.Abstractions;

/// <summary>
/// Well-known SquidStd ActivitySource for app-level custom spans. SquidStd subsystems name their own
/// sources with the "SquidStd." prefix; the OpenTelemetry provider captures them via "SquidStd.*".
/// </summary>
public static class SquidStdActivity
{
    /// <summary>The naming prefix for SquidStd activity sources.</summary>
    public const string SourcePrefix = "SquidStd";

    /// <summary>The shared app-level activity source (named "SquidStd").</summary>
    public static ActivitySource Source { get; } = new(SourcePrefix);
}
