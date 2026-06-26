using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Types.Files;

namespace SquidStd.Core.Data.Files;

/// <summary>
///     Published on the event bus when a watched file changes, after debouncing.
/// </summary>
/// <param name="Kind">The kind of change observed.</param>
/// <param name="FullPath">The absolute path of the affected file.</param>
/// <param name="OldFullPath">The previous absolute path for a rename, otherwise null.</param>
public sealed record FileChangedEvent(FileChangeKind Kind, string FullPath, string? OldFullPath = null) : IEvent;
