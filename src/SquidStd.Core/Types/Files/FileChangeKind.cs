namespace SquidStd.Core.Types.Files;

/// <summary>
///     The kind of change observed on a watched file.
/// </summary>
public enum FileChangeKind
{
    /// <summary>The file was created.</summary>
    Created,

    /// <summary>The file content or metadata changed.</summary>
    Changed,

    /// <summary>The file was deleted.</summary>
    Deleted,

    /// <summary>The file was renamed or moved.</summary>
    Renamed
}
