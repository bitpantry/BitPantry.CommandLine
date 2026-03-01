using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Built-in handler for autocompleting file system paths.
/// Uses IFileSystem for transparent local/remote (sandboxed) file system access.
/// Bound via [FilePathAutoComplete] attribute — not a type handler since string is too broad.
/// </summary>
public class FilePathAutoCompleteHandler : IAutoCompleteHandler
{
    private readonly IFileSystem _fileSystem;
    private readonly Style _directoryStyle;

    /// <summary>
    /// Creates a new FilePathAutoCompleteHandler.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction for directory/file enumeration.</param>
    /// <param name="theme">The theme providing directory styling.</param>
    public FilePathAutoCompleteHandler(IFileSystem fileSystem, Theme theme)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _directoryStyle = (theme ?? throw new ArgumentNullException(nameof(theme))).MenuGroup;
    }

    /// <inheritdoc/>
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var query = context.QueryString ?? string.Empty;
        var options = new List<AutoCompleteOption>();

        try
        {
            // Split query into directory prefix and filename fragment
            var (directoryPath, fragment) = SplitQueryIntoDirectoryAndFragment(query);

            // Resolve the directory to enumerate
            string targetDir;
            if (string.IsNullOrEmpty(directoryPath))
            {
                targetDir = _fileSystem.Directory.GetCurrentDirectory();
            }
            else
            {
                targetDir = directoryPath;
            }

            if (!_fileSystem.Directory.Exists(targetDir))
            {
                return Task.FromResult(options);
            }

            var separator = Path.DirectorySeparatorChar;

            // Enumerate directories
            var directories = _fileSystem.Directory.GetDirectories(targetDir)
                .Select(d => _fileSystem.Path.GetFileName(d))
                .Where(name => name.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name =>
                {
                    var value = string.IsNullOrEmpty(directoryPath)
                        ? name + separator
                        : directoryPath + name + separator;
                    return new AutoCompleteOption(value, menuStyle: _directoryStyle);
                });

            // Enumerate files
            var files = _fileSystem.Directory.GetFiles(targetDir)
                .Select(f => _fileSystem.Path.GetFileName(f))
                .Where(name => name.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name =>
                {
                    var value = string.IsNullOrEmpty(directoryPath)
                        ? name
                        : directoryPath + name;
                    return new AutoCompleteOption(value);
                });

            // Directories first, then files
            options.AddRange(directories);
            options.AddRange(files);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is DirectoryNotFoundException || ex is IOException)
        {
            // Gracefully return empty on access errors
        }

        return Task.FromResult(options);
    }

    /// <summary>
    /// Splits a query string into a directory path prefix and a filename fragment.
    /// For "dir/sub/fi" → ("dir/sub/", "fi")
    /// For "fi" → ("", "fi")
    /// For "dir/" → ("dir/", "")
    /// </summary>
    internal static (string directoryPath, string fragment) SplitQueryIntoDirectoryAndFragment(string query)
    {
        if (string.IsNullOrEmpty(query))
            return (string.Empty, string.Empty);

        var lastSepIndex = query.LastIndexOfAny(new[] { '/', '\\' });
        if (lastSepIndex < 0)
            return (string.Empty, query);

        var directoryPath = query.Substring(0, lastSepIndex + 1);
        var fragment = query.Substring(lastSepIndex + 1);
        return (directoryPath, fragment);
    }
}
