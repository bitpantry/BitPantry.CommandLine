using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;

namespace BitPantry.CommandLine.Remote.SignalR.Client;

/// <summary>
/// Provides autocomplete options for the --consent-mode argument.
/// </summary>
public class ConsentModeProvider : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var query = context.QueryString ?? string.Empty;

        var options = Enum.GetNames<ConsentMode>()
            .Where(n => n.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(n => new AutoCompleteOption(n))
            .ToList();

        return Task.FromResult(options);
    }
}
