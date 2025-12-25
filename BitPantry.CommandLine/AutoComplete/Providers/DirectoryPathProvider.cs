using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions for directory paths only.
/// </summary>
/// <remarks>
/// This provider uses IFileSystem for testability and completes
/// only directories, not files. Paths with spaces are quoted.
/// </remarks>
public class DirectoryPathProvider : ICompletionProvider
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryPathProvider"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    public DirectoryPathProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Priority 61 - just after file path provider.
    /// </remarks>
    public int Priority => 61;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Handle argument values - can be refined to check for directory attribute
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
            string dirPrefix;

            // Determine the directory to search and the prefix
            var lastSeparator = Math.Max(prefix.LastIndexOf('/'), prefix.LastIndexOf('\\'));
            if (lastSeparator >= 0)
            {
                var dirPart = prefix.Substring(0, lastSeparator + 1);
                searchDir = _fileSystem.Path.GetFullPath(dirPart);
                dirPrefix = prefix.Substring(lastSeparator + 1);
            }
            else
            {
                searchDir = _fileSystem.Directory.GetCurrentDirectory();
                dirPrefix = prefix;
            }

            if (!_fileSystem.Directory.Exists(searchDir))
                return Task.FromResult(CompletionResult.Empty);

            // Get directories only
            foreach (var dir in _fileSystem.Directory.GetDirectories(searchDir))
            {
                var dirName = _fileSystem.Path.GetFileName(dir);
                if (string.IsNullOrEmpty(dirPrefix) ||
                    dirName.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(CreateItem(dir, dirName, prefix, lastSeparator));
                }
            }
        }
        catch (Exception)
        {
            // Directory doesn't exist or access denied
            return Task.FromResult(CompletionResult.Empty);
        }

        // Sort alphabetically
        items = items.OrderBy(i => i.DisplayText).ToList();

        return Task.FromResult(new CompletionResult(items));
    }

    private CompletionItem CreateItem(string fullPath, string name, string originalPrefix, int lastSeparator)
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

        insertText += _fileSystem.Path.DirectorySeparatorChar;

        // Quote paths with spaces
        if (insertText.Contains(' '))
        {
            insertText = $"\"{insertText}\"";
        }

        return new CompletionItem
        {
            DisplayText = name + "/",
            InsertText = insertText,
            Kind = CompletionItemKind.Directory,
            SortPriority = 0
        };
    }
}
