using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Core interface for autocomplete capability.
/// Used directly by Attribute Handlers (explicit binding via [AutoComplete] attribute).
/// Type Handlers extend this with CanHandle for runtime matching.
/// </summary>
public interface IAutoCompleteHandler
{
    /// <summary>
    /// Gets autocomplete options for the argument.
    /// </summary>
    /// <param name="context">Context information about the current autocomplete operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of autocomplete options.</returns>
    Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default);
}
