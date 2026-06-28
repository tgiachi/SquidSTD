using SquidStd.Vfs.Abstractions.Interfaces;
using SquidStd.Vfs.Abstractions;

namespace SquidStd.Vfs.Services;

/// <summary>A VFS-backed analogue of <c>DirectoriesConfig</c>: named logical directories over any backend.</summary>
public sealed class VfsDirectories
{
    private readonly IVirtualFileSystem _fileSystem;
    private readonly HashSet<string> _directories;

    /// <summary>The logical path of a named directory.</summary>
    public string this[string directoryName] => GetPath(directoryName);

    /// <summary>The logical path of a named directory, by enum name.</summary>
    public string this[Enum directoryType] => GetPath(directoryType.ToString());

    public VfsDirectories(IVirtualFileSystem fileSystem, IReadOnlyCollection<string> directories)
    {
        ArgumentNullException.ThrowIfNull(directories);
        _fileSystem = fileSystem;
        _directories = directories.Select(VfsPath.Normalize).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Resolves the logical path of a named directory; throws when it was not declared.</summary>
    public string GetPath(string directoryName)
    {
        var normalized = VfsPath.Normalize(directoryName);

        if (!_directories.Contains(normalized))
        {
            throw new KeyNotFoundException($"Directory '{directoryName}' is not declared.");
        }

        return normalized;
    }

    /// <summary>Combines a named directory with a relative sub-path into a normalized logical path.</summary>
    public string Combine(string directoryName, string relativePath)
    {
        return VfsPath.Normalize(GetPath(directoryName) + "/" + relativePath);
    }
}
