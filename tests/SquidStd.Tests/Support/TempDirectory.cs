using SysPath = System.IO.Path;

namespace SquidStd.Tests.Support;

/// <summary>
///     Creates a unique temporary directory for a test and removes it on dispose.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>
    ///     Gets the absolute path of the temporary directory.
    /// </summary>
    public string Path { get; }

    public TempDirectory()
    {
        Path = SysPath.Combine(SysPath.GetTempPath(), "squidstd-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    ///     Combines a relative path with the temporary directory root.
    /// </summary>
    /// <param name="relative">The relative path.</param>
    /// <returns>The combined absolute path.</returns>
    public string Combine(string relative)
    {
        return SysPath.Combine(Path, relative);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
        catch
        {
            // Best-effort cleanup; ignore failures during teardown.
        }
    }
}
