using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;

namespace BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;

/// <summary>
/// Provides completions by fetching file/directory listings from a remote SignalR server.
/// This provider is used when the argument has [RemoteFilePathCompletion] or [RemoteDirectoryPathCompletion] attributes.
/// </summary>
public class RemoteCompletionProvider : ICompletionProvider
{
    private readonly IServerProxy _serverProxy;
    private readonly RemoteFileSystemService _remoteFileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteCompletionProvider"/> class.
    /// </summary>
    /// <param name="serverProxy">The server proxy for checking connection state.</param>
    /// <param name="remoteFileSystem">The remote file system service for listing files.</param>
    public RemoteCompletionProvider(IServerProxy serverProxy, RemoteFileSystemService remoteFileSystem)
    {
        _serverProxy = serverProxy;
        _remoteFileSystem = remoteFileSystem;
    }

    /// <inheritdoc />
    public string Name => "Remote";

    /// <inheritdoc />
    public int Priority => 200; // High priority to handle remote before local providers

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Only handle argument values
        if (context.ElementType != CompletionElementType.ArgumentValue)
            return false;

        // Handle if this is a remote command
        if (context.IsRemote)
            return _serverProxy != null && _serverProxy.ConnectionState == ServerProxyConnectionState.Connected;

        // Handle if the argument has remote file/directory completion attributes (only when connected)
        if (_serverProxy?.ConnectionState == ServerProxyConnectionState.Connected &&
            (context.CompletionAttribute is RemoteFilePathCompletionAttribute ||
             context.CompletionAttribute is RemoteDirectoryPathCompletionAttribute))
            return true;

        return false;
    }

    /// <inheritdoc />
    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (_serverProxy == null)
        {
            return CompletionResult.Error("Server proxy not available");
        }


        try
        {
            // Handle remote file/directory path completion
            if (context.CompletionAttribute is RemoteFilePathCompletionAttribute ||
                context.CompletionAttribute is RemoteDirectoryPathCompletionAttribute)
            {
                return await GetRemotePathCompletionsAsync(context, cancellationToken);
            }

            // For other remote commands, use the general autocomplete RPC
            var result = await _serverProxy.GetCompletionsAsync(context, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            return CompletionResult.Empty;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("disconnected"))
        {
            return CompletionResult.Empty;
        }
        catch (TimeoutException)
        {
            return CompletionResult.TimedOut;
        }
        catch (Exception ex)
        {
            return CompletionResult.Error($"Remote completion error: {ex.Message}");
        }
    }

    private async Task<CompletionResult> GetRemotePathCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken)
    {
        // Parse the partial value to extract directory and search prefix
        var partialValue = context.PartialValue ?? string.Empty;
        
        // Split into directory path and filename prefix
        string directoryPath;
        string searchPrefix;

        var lastSlash = Math.Max(partialValue.LastIndexOf('/'), partialValue.LastIndexOf('\\'));
        if (lastSlash >= 0)
        {
            directoryPath = partialValue.Substring(0, lastSlash + 1);
            searchPrefix = partialValue.Substring(lastSlash + 1);
        }
        else
        {
            directoryPath = string.Empty;
            searchPrefix = partialValue;
        }

        // Normalize directory path (remove leading slash for server)
        var normalizedPath = directoryPath.TrimStart('/', '\\');

        List<FileMetadata> items;

        // Determine if we need directories only or files + directories
        if (context.CompletionAttribute is RemoteDirectoryPathCompletionAttribute)
        {
            // Directories only - map them to FileMetadata
            var dirNames = await _remoteFileSystem.ListDirectoriesAsync(normalizedPath, searchPrefix, cancellationToken);
            items = dirNames.Select(name => new FileMetadata { Name = name, IsDirectory = true }).ToList();
        }
        else
        {
            // Files and directories (for navigation)
            items = await _remoteFileSystem.ListFilesWithMetadataAsync(normalizedPath, searchPrefix, cancellationToken);
        }

        // Transform to completion items
        var completionItems = new List<CompletionItem>();

        foreach (var item in items)
        {
            var fullPath = string.IsNullOrEmpty(directoryPath) ? item.Name : directoryPath + item.Name;

            // Quote if contains spaces
            var displayText = item.Name;
            var insertText = fullPath.Contains(' ') ? $"\"{fullPath}\"" : fullPath;

            completionItems.Add(new CompletionItem
            {
                DisplayText = displayText,
                InsertText = insertText,
                Kind = item.IsDirectory ? CompletionItemKind.Directory : CompletionItemKind.File,
                SortPriority = item.IsDirectory ? 0 : 1 // Directories first
            });
        }

        return new CompletionResult(completionItems, completionItems.Count);
    }
}
