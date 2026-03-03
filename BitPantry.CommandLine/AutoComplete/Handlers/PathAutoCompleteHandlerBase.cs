using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Base class for path-based autocomplete handlers.
/// Uses <see cref="IPathEntryProvider"/> for path enumeration — the provider
/// may be backed by a local file system or a remote RPC call depending on
/// execution context and DI wiring.
/// Subclasses control whether files are included via the <paramref name="includeFiles"/> constructor parameter.
/// </summary>
public abstract class PathAutoCompleteHandlerBase : IAutoCompleteHandler
{
    private readonly IPathEntryProvider _provider;
    private readonly Style _directoryStyle;
    private readonly bool _includeFiles;

    /// <summary>
    /// Creates a new PathAutoCompleteHandlerBase.
    /// </summary>
    /// <param name="provider">The path entry provider (local or remote).</param>
    /// <param name="theme">The theme providing directory styling.</param>
    /// <param name="includeFiles">Whether to include files in the results (true) or only directories (false).</param>
    protected PathAutoCompleteHandlerBase(IPathEntryProvider provider, Theme theme, bool includeFiles)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _directoryStyle = (theme ?? throw new ArgumentNullException(nameof(theme))).MenuGroup;
        _includeFiles = includeFiles;
    }

    /// <inheritdoc/>
    public async Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var query = context.QueryString ?? string.Empty;
        var options = new List<AutoCompleteOption>();

        try
        {
            // Split query into directory prefix and filename fragment
            var (directoryPath, fragment) = PathQueryHelper.SplitQueryIntoDirectoryAndFragment(query);

            // Resolve the directory to enumerate
            string targetDir;
            if (string.IsNullOrEmpty(directoryPath))
            {
                targetDir = _provider.GetCurrentDirectory();
            }
            else
            {
                targetDir = directoryPath;
            }

            // Enumerate entries from the provider
            var entries = await _provider.EnumerateAsync(targetDir, _includeFiles, cancellationToken);

            var separator = Path.DirectorySeparatorChar;

            // Build directory options
            var dirOptions = entries
                .Where(e => e.IsDirectory)
                .Where(e => e.Name.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
                .Select(e =>
                {
                    var value = string.IsNullOrEmpty(directoryPath)
                        ? e.Name + separator
                        : directoryPath + e.Name + separator;
                    return new AutoCompleteOption(value, menuStyle: _directoryStyle);
                });

            options.AddRange(dirOptions);

            // Build file options (only when provider returned them)
            if (_includeFiles)
            {
                var fileOptions = entries
                    .Where(e => !e.IsDirectory)
                    .Where(e => e.Name.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
                    .Select(e =>
                    {
                        var value = string.IsNullOrEmpty(directoryPath)
                            ? e.Name
                            : directoryPath + e.Name;
                        return new AutoCompleteOption(value);
                    });

                options.AddRange(fileOptions);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected
        }
        catch (Exception)
        {
            // Gracefully return empty on errors
        }

        return options;
    }
}
