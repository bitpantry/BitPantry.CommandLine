using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.Client;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completions by fetching from a remote SignalR server.
/// This provider is used when the completion context indicates a remote command.
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
        // Only handle if this is a remote command and we have a connected server proxy
        if (!context.IsRemote)
            return false;

        // Check if server proxy is connected
        if (_serverProxy == null || _serverProxy.ConnectionState != ServerProxyConnectionState.Connected)
            return false;

        // Handle argument values for remote commands
        return context.ElementType == CompletionElementType.ArgumentValue;
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
