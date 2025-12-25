using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions for file paths.
/// </summary>
/// <remarks>
/// This provider uses IFileSystem for testability and completes
/// both files and directories. Paths with spaces are quoted.
/// </remarks>
public class FilePathProvider : ICompletionProvider
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePathProvider"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    public FilePathProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Priority 60 - after argument names, for value completion.
    /// </remarks>
    public int Priority => 60;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Handle argument values - can be refined to check for file path attribute
        return context.ElementType == CompletionElementType.ArgumentValue;
    }

    /// <inheritdoc />
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(CompletionResult.Empty);

        var prefix = context.CurrentWord ?? string.Empty;
        var items = new List<CompletionItem>();

        try
        {
            string searchDir;
            string filePrefix;

            // Determine the directory to search and the file prefix
            var lastSeparator = Math.Max(prefix.LastIndexOf('/'), prefix.LastIndexOf('\\'));
            if (lastSeparator >= 0)
            {
                var dirPart = prefix.Substring(0, lastSeparator + 1);
                searchDir = _fileSystem.Path.GetFullPath(dirPart);
                filePrefix = prefix.Substring(lastSeparator + 1);
            }
            else
            {
                searchDir = _fileSystem.Directory.GetCurrentDirectory();
                filePrefix = prefix;
            }

            if (!_fileSystem.Directory.Exists(searchDir))
                return Task.FromResult(CompletionResult.Empty);

            // Get directories
            foreach (var dir in _fileSystem.Directory.GetDirectories(searchDir))
            {
                var dirName = _fileSystem.Path.GetFileName(dir);
                if (string.IsNullOrEmpty(filePrefix) ||
                    dirName.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(CreateItem(dir, dirName, isDirectory: true, prefix, lastSeparator));
                }
            }

            // Get files
            foreach (var file in _fileSystem.Directory.GetFiles(searchDir))
            {
                var fileName = _fileSystem.Path.GetFileName(file);
                if (string.IsNullOrEmpty(filePrefix) ||
                    fileName.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(CreateItem(file, fileName, isDirectory: false, prefix, lastSeparator));
                }
            }
        }
        catch (Exception)
        {
            // Directory doesn't exist or access denied
            return Task.FromResult(CompletionResult.Empty);
        }

        // Sort: directories first, then alphabetically
        items = items
            .OrderBy(i => i.Kind != CompletionItemKind.Directory)
            .ThenBy(i => i.DisplayText)
            .ToList();

        return Task.FromResult(new CompletionResult(items));
    }

    private CompletionItem CreateItem(string fullPath, string name, bool isDirectory, string originalPrefix, int lastSeparator)
    {
        // Build the insert text
        string insertText;
        if (lastSeparator >= 0)
        {
            // Include the original path prefix
            insertText = originalPrefix.Substring(0, lastSeparator + 1) + name;
        }
        else
        {
            insertText = name;
        }

        if (isDirectory)
        {
            insertText += _fileSystem.Path.DirectorySeparatorChar;
        }

        // Quote paths with spaces
        if (insertText.Contains(' '))
        {
            insertText = $"\"{insertText}\"";
        }

        return new CompletionItem
        {
            DisplayText = name + (isDirectory ? "/" : ""),
            InsertText = insertText,
            Kind = isDirectory ? CompletionItemKind.Directory : CompletionItemKind.File,
            SortPriority = isDirectory ? 1 : 0
        };
    }
}
