namespace SquidStd.Vfs.Abstractions;

/// <summary>Normalizes logical VFS paths to forward-slash, root-relative form and rejects traversal.</summary>
public static class VfsPath
{
    /// <summary>Normalizes a logical path: forward slashes, no leading/empty segments, no <c>.</c>/<c>..</c>.</summary>
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var segments = path.Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments)
        {
            if (segment is "." or "..")
            {
                throw new ArgumentException($"Path '{path}' must not contain relative segments.", nameof(path));
            }
        }

        return string.Join('/', segments);
    }
}
