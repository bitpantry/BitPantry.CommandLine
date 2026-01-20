using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Built-in handler for autocompleting enum values.
/// Matches any enum type and returns all enum values as options.
/// </summary>
public class EnumAutoCompleteHandler : ITypeAutoCompleteHandler
{
    /// <summary>
    /// Returns true if the argument type is an enum.
    /// </summary>
    /// <param name="argumentType">The type to check.</param>
    /// <returns>True if the type is an enum; otherwise false.</returns>
    public bool CanHandle(Type argumentType)
    {
        return argumentType.IsEnum;
    }

    /// <summary>
    /// Returns autocomplete options for the enum values.
    /// </summary>
    /// <param name="context">The autocomplete context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of autocomplete options.</returns>
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var propertyType = context.ArgumentInfo.PropertyInfo.GetPropertyInfo().PropertyType;
        
        // Unwrap Nullable<T> to get the underlying enum type
        var enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        // Get all enum value names
        var enumValues = Enum.GetNames(enumType);

        // Filter by prefix (case-insensitive)
        var query = context.QueryString ?? string.Empty;
        var filteredValues = enumValues
            .Where(name => name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        var options = filteredValues
            .Select(name => new AutoCompleteOption(name))
            .ToList();

        return Task.FromResult(options);
    }
}
