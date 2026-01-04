using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides directory path completions for file system navigation.
/// Used when an argument has the [DirectoryPathCompletion] attribute.
/// </summary>
public sealed class DirectoryPathCompletionProvider : ICompletionProvider
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryPathCompletionProvider"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    public DirectoryPathCompletionProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    public int Priority => 20;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Handles when attribute is DirectoryPathCompletionAttribute
        return context.CompletionAttribute is Attributes.DirectoryPathCompletionAttribute;
    }

    /// <inheritdoc />
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(CompletionResult.Empty);

        var attr = context.CompletionAttribute as Attributes.DirectoryPathCompletionAttribute;
        var prefix = context.CurrentWord ?? string.Empty;
        var items = new List<CompletionItem>();

        try
        {
            string searchDir;
            string dirPrefix;

            // Determine the directory to search and the directory prefix
            var lastSeparator = Math.Max(prefix.LastIndexOf('/'), prefix.LastIndexOf('\\'));
            if (lastSeparator >= 0)
            {
                var dirPart = prefix.Substring(0, lastSeparator + 1);
                searchDir = _fileSystem.Path.GetFullPath(dirPart);
                dirPrefix = prefix.Substring(lastSeparator + 1);
            }
            else if (!string.IsNullOrEmpty(attr?.BasePath))
            {
                searchDir = attr.BasePath;
                dirPrefix = prefix;
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
                if (!ShouldInclude(dirName, dirPrefix, attr?.IncludeHidden ?? false))
                    continue;
                items.Add(CreateItem(dir, dirName, prefix, lastSeparator));
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

    private bool ShouldInclude(string name, string prefix, bool includeHidden)
    {
        // Check prefix match
        if (!string.IsNullOrEmpty(prefix) && !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        // Check hidden directories (starts with '.')
        if (!includeHidden && name.StartsWith("."))
            return false;

        return true;
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
