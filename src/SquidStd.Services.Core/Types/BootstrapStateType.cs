namespace SquidStd.Services.Core.Types;

/// <summary>
/// Lifecycle state of the SquidStd bootstrapper.
/// </summary>
internal enum BootstrapStateType
{
    /// <summary>Created but not started.</summary>
    Created,

    /// <summary>Started and running.</summary>
    Started,

    /// <summary>Stopped; cannot be restarted.</summary>
    Stopped
}
