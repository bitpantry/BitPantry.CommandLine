using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Client;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completions by fetching from a remote SignalR server.
/// This provider is used when:
/// 1. The completion context indicates a remote command (context.IsRemote), OR
/// 2. The argument has [RemoteFilePathCompletion] attribute (explicit remote completion)
/// </summary>
public class RemoteCompletionProvider : ICompletionProvider
{
    private readonly IServerProxy _serverProxy;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteCompletionProvider"/> class.
    /// </summary>
    /// <param name="serverProxy">The server proxy for remote communication.</param>
    public RemoteCompletionProvider(IServerProxy serverProxy)
    {
        _serverProxy = serverProxy;
    }

    /// <inheritdoc />
    public string Name => "Remote";

    /// <inheritdoc />
    public int Priority => 200; // High priority to handle remote before local providers

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Check if server proxy is connected - required for any remote completion
        if (_serverProxy == null || _serverProxy.ConnectionState != ServerProxyConnectionState.Connected)
            return false;

        // Only handle argument values
        if (context.ElementType != CompletionElementType.ArgumentValue)
            return false;

        // Handle if this is a remote command
        if (context.IsRemote)
            return true;

        // Also handle if the argument has [RemoteFilePathCompletion] attribute
        // This allows local commands like "file download" to get remote path completions
        if (context.CompletionAttribute is RemoteFilePathCompletionAttribute)
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

        if (_serverProxy.ConnectionState != ServerProxyConnectionState.Connected)
        {
            return CompletionResult.Error("(offline)");
        }

        try
        {
            // Fetch completions from server
            var result = await _serverProxy.GetCompletionsAsync(context, cancellationToken);
            
            // Apply local prefix filtering if we have a partial value
            if (!string.IsNullOrEmpty(context.PartialValue) && result.Items.Count > 0)
            {
                var filtered = result.Items
                    .Where(item => item.DisplayText.StartsWith(context.PartialValue, StringComparison.OrdinalIgnoreCase) ||
                                   item.InsertText.StartsWith(context.PartialValue, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                return new CompletionResult(filtered, filtered.Count)
                {
                    IsError = result.IsError,
                    ErrorMessage = result.ErrorMessage,
                    IsTimedOut = result.IsTimedOut
                };
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return CompletionResult.Empty;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("disconnected"))
        {
            return CompletionResult.Error("(offline)");
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
}
