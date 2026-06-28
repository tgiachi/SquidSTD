using System.Collections.Concurrent;
using Serilog;
using SquidStd.Core.Data.Files;
using SquidStd.Core.Interfaces.Events;
using SquidStd.Core.Interfaces.Files;
using SquidStd.Core.Types.Files;
using ILogger = Serilog.ILogger;

namespace SquidStd.Core.Files;

/// <summary>
///     Recursive directory watcher that coalesces rapid file-system notifications with a debounce window and
///     publishes a single <see cref="FileChangedEvent" /> per path on the event bus. Each <see cref="Watch(string,string)" />
///     call adds one recursive watcher with its own glob filter, so several directories (each with a different
///     pattern, e.g. <c>*.lua</c> and <c>*.json</c>) can be watched at once. The watcher stays decoupled from
///     any reload logic.
/// </summary>
public sealed class FileWatcherService : IFileWatcherService
{
    private readonly IEventBus _eventBus;
    private readonly TimeSpan _debounce;
    private readonly ILogger _logger = Log.ForContext<FileWatcherService>();
    private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, FileChangedEvent> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _registeredKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly Lock _sync = new();
    private bool _disposed;

    /// <summary>
    ///     Initializes the watcher with the default 300ms debounce window.
    /// </summary>
    /// <param name="eventBus">The bus that receives <see cref="FileChangedEvent" /> notifications.</param>
    public FileWatcherService(IEventBus eventBus)
        : this(eventBus, TimeSpan.FromMilliseconds(300))
    {
    }

    /// <summary>
    ///     Initializes the watcher with an explicit debounce window.
    /// </summary>
    /// <param name="eventBus">The bus that receives <see cref="FileChangedEvent" /> notifications.</param>
    /// <param name="debounceDelay">How long a path must be quiet before its change is published.</param>
    public FileWatcherService(IEventBus eventBus, TimeSpan debounceDelay)
    {
        if (debounceDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(debounceDelay), "Debounce delay cannot be negative.");
        }

        _eventBus = eventBus;
        _debounce = debounceDelay;
    }

    /// <inheritdoc />
    public void Watch(string path)
    {
        Watch(path, "*");
    }

    /// <inheritdoc />
    public void Watch(string path, string filter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(filter);
        ThrowIfDisposed();

        var fullPath = Path.GetFullPath(path);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        FileSystemWatcher watcher;

        lock (_sync)
        {
            if (!_registeredKeys.Add(KeyFor(fullPath, filter)))
            {
                return;
            }

            watcher = new FileSystemWatcher(fullPath, filter)
            {
                NotifyFilter = NotifyFilters.LastWrite |
                               NotifyFilters.FileName |
                               NotifyFilters.DirectoryName |
                               NotifyFilters.CreationTime |
                               NotifyFilters.Size,
                IncludeSubdirectories = true
            };

            _watchers.Add(watcher);
        }

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileRenamed;
        watcher.EnableRaisingEvents = true;
    }

    /// <inheritdoc />
    public void Unwatch(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        List<FileSystemWatcher> removed = [];

        lock (_sync)
        {
            for (var i = _watchers.Count - 1; i >= 0; i--)
            {
                var watcher = _watchers[i];

                if (!IsAtOrUnder(watcher.Path, fullPath))
                {
                    continue;
                }

                removed.Add(watcher);
                _watchers.RemoveAt(i);
                _registeredKeys.Remove(KeyFor(watcher.Path, watcher.Filter));
            }
        }

        foreach (var watcher in removed)
        {
            DisposeWatcher(watcher);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        List<FileSystemWatcher> watchers;

        lock (_sync)
        {
            watchers = [.. _watchers];
            _watchers.Clear();
            _registeredKeys.Clear();
        }

        foreach (var watcher in watchers)
        {
            DisposeWatcher(watcher);
        }

        foreach (var (_, timer) in _debounceTimers)
        {
            timer.Dispose();
        }

        _debounceTimers.Clear();
        _pending.Clear();
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Directory create/change notifications are not file changes; recursion is handled by the watcher.
        if (e.ChangeType != WatcherChangeTypes.Deleted && Directory.Exists(e.FullPath))
        {
            return;
        }

        var kind = e.ChangeType switch
        {
            WatcherChangeTypes.Created => FileChangeKind.Created,
            WatcherChangeTypes.Deleted => FileChangeKind.Deleted,
            _                          => FileChangeKind.Changed
        };

        Schedule(new FileChangedEvent(kind, Path.GetFullPath(e.FullPath)));
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (Directory.Exists(e.FullPath))
        {
            return;
        }

        Schedule(
            new FileChangedEvent(FileChangeKind.Renamed, Path.GetFullPath(e.FullPath), Path.GetFullPath(e.OldFullPath))
        );
    }

    private void Schedule(FileChangedEvent change)
    {
        if (_disposed)
        {
            return;
        }

        var key = change.FullPath;
        _pending[key] = change;

        var timer = _debounceTimers.AddOrUpdate(
            key,
            k => new Timer(OnDebounceElapsed, k, _debounce, Timeout.InfiniteTimeSpan),
            (_, existing) =>
            {
                existing.Change(_debounce, Timeout.InfiniteTimeSpan);

                return existing;
            }
        );

        timer.Change(_debounce, Timeout.InfiniteTimeSpan);
    }

    private void OnDebounceElapsed(object? state)
    {
        if (state is not string key)
        {
            return;
        }

        if (_debounceTimers.TryRemove(key, out var timer))
        {
            timer.Dispose();
        }

        if (!_pending.TryRemove(key, out var change))
        {
            return;
        }

        _ = PublishAsync(change);
    }

    private async Task PublishAsync(FileChangedEvent change)
    {
        try
        {
            await _eventBus.PublishAsync(change);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to publish file change for {Path}", change.FullPath);
        }
    }

    private static string KeyFor(string path, string filter)
    {
        return path + "|" + filter;
    }

    private static void DisposeWatcher(FileSystemWatcher watcher)
    {
        watcher.EnableRaisingEvents = false;
        watcher.Dispose();
    }

    private static bool IsAtOrUnder(string candidate, string root)
    {
        if (string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FileWatcherService));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }
}
