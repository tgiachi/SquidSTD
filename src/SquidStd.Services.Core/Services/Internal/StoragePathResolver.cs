namespace SquidStd.Services.Core.Services.Internal;

/// <summary>
/// Resolves logical storage keys into paths constrained to one root directory.
/// </summary>
internal static class StoragePathResolver
{
    public static string ResolveFilePath(string rootDirectory, string key, string? extension = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (Path.IsPathRooted(key))
        {
            throw new ArgumentException("Storage key cannot be rooted.", nameof(key));
        }

        var normalizedRoot = Path.GetFullPath(rootDirectory);
        var segments = key.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            throw new ArgumentException("Storage key cannot be empty.", nameof(key));
        }

        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i] is "." or "..")
            {
                throw new ArgumentException("Storage key cannot contain relative path segments.", nameof(key));
            }
        }

        var relativePath = Path.Combine(segments);

        if (!string.IsNullOrWhiteSpace(extension))
        {
            relativePath += extension;
        }

        var fullPath = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));
        var rootPrefix = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
                             ? normalizedRoot
                             : normalizedRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("Storage key resolves outside the storage root.", nameof(key));
        }

        return fullPath;
    }
}
