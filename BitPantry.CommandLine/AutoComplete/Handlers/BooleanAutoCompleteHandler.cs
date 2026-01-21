using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Built-in handler for autocompleting boolean values.
/// Returns "true" and "false" as options.
/// </summary>
public class BooleanAutoCompleteHandler : ITypeAutoCompleteHandler
{
    /// <summary>
    /// Returns true if the argument type is a boolean.
    /// </summary>
    /// <param name="argumentType">The type to check.</param>
    /// <returns>True if the type is bool; otherwise false.</returns>
    public bool CanHandle(Type argumentType)
    {
        return argumentType == typeof(bool);
    }

    /// <summary>
    /// Returns autocomplete options for boolean values.
    /// </summary>
    /// <param name="context">The autocomplete context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of autocomplete options.</returns>
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var values = new[] { "false", "true" };
        var query = context.QueryString ?? string.Empty;

        var options = values
            .Where(v => v.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(v => new AutoCompleteOption(v))
            .ToList();

        return Task.FromResult(options);
    }
}
