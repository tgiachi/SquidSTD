namespace SquidStd.Core.Interfaces.Files;

/// <summary>
///     Watches directories recursively and publishes debounced file-change events on the event bus.
/// </summary>
public interface IFileWatcherService : IDisposable
{
    /// <summary>
    ///     Starts watching a directory and all of its current and future subdirectories, for every file.
    ///     The directory is created if it does not exist.
    /// </summary>
    /// <param name="path">The directory to watch.</param>
    void Watch(string path);

    /// <summary>
    ///     Starts watching a directory and all of its current and future subdirectories, limited to files
    ///     matching the glob filter. The directory is created if it does not exist. Call this once per
    ///     directory to watch several at the same time, each with its own filter.
    /// </summary>
    /// <param name="path">The directory to watch.</param>
    /// <param name="filter">A glob pattern such as <c>*.lua</c> or <c>*</c> for all files.</param>
    void Watch(string path, string filter);

    /// <summary>
    ///     Stops watching a directory and its subdirectories.
    /// </summary>
    /// <param name="path">The directory to stop watching.</param>
    void Unwatch(string path);

    /// <summary>
    ///     Stops every watcher and cancels pending debounced notifications.
    /// </summary>
    void Stop();
}
