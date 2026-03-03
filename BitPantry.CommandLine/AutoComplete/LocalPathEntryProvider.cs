using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Provides path entries from a local <see cref="IFileSystem"/>.
/// Always available. Used when the target filesystem is on the same side as execution.
/// </summary>
public class LocalPathEntryProvider : IPathEntryProvider
{
    private readonly IFileSystem _fileSystem;

    public LocalPathEntryProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    public string GetCurrentDirectory() => _fileSystem.Directory.GetCurrentDirectory();

    /// <inheritdoc />
    public Task<IReadOnlyList<PathEntry>> EnumerateAsync(
        string directoryPath,
        bool includeFiles,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<PathEntry>();

        try
        {
            if (!_fileSystem.Directory.Exists(directoryPath))
                return Task.FromResult<IReadOnlyList<PathEntry>>(entries);

            // Directories are always included
            var dirs = _fileSystem.Directory.GetDirectories(directoryPath)
                .Select(d => new PathEntry(_fileSystem.Path.GetFileName(d), IsDirectory: true))
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

            entries.AddRange(dirs);

            // Files only when requested
            if (includeFiles)
            {
                var files = _fileSystem.Directory.GetFiles(directoryPath)
                    .Select(f => new PathEntry(_fileSystem.Path.GetFileName(f), IsDirectory: false))
                    .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

                entries.AddRange(files);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
        {
            // Gracefully return empty on access errors
        }

        return Task.FromResult<IReadOnlyList<PathEntry>>(entries);
    }
}
