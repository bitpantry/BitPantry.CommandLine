using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions for enum values.
/// </summary>
/// <remarks>
/// This provider auto-detects enum types from the PropertyType
/// and returns all enum values, filtered by the current prefix.
/// </remarks>
public class EnumProvider : ICompletionProvider
{
    /// <inheritdoc />
    /// <remarks>
    /// Priority 65 - for value completion after paths.
    /// </remarks>
    public int Priority => 65;

    /// <inheritdoc />
    public bool CanHandle(CompletionContext context)
    {
        // Only handle argument values with enum property type
        if (context.ElementType != CompletionElementType.ArgumentValue)
            return false;

        return GetUnderlyingEnumType(context.PropertyType) != null;
    }

    /// <inheritdoc />
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(CompletionResult.Empty);

        var enumType = GetUnderlyingEnumType(context.PropertyType);
        if (enumType == null)
            return Task.FromResult(CompletionResult.Empty);

        var prefix = context.CurrentWord ?? string.Empty;
        var items = new List<CompletionItem>();

        // Get all enum values
        var enumNames = Enum.GetNames(enumType);

        foreach (var enumName in enumNames)
        {
            // Filter by prefix (case-insensitive)
            if (!string.IsNullOrEmpty(prefix) &&
                !enumName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            items.Add(new CompletionItem
            {
                DisplayText = enumName,
                InsertText = enumName,
                Kind = CompletionItemKind.ArgumentValue,
                SortPriority = 0
            });
        }

        // Sort alphabetically
        items = items.OrderBy(i => i.DisplayText).ToList();

        return Task.FromResult(new CompletionResult(items));
    }

    /// <summary>
    /// Gets the underlying enum type from a type, handling nullable and array types.
    /// </summary>
    private static Type GetUnderlyingEnumType(Type type)
    {
        if (type == null)
            return null;

        // Direct enum type
        if (type.IsEnum)
            return type;

        // Nullable<EnumType>
        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType?.IsEnum == true)
            return nullableType;

        // Array of enum (EnumType[])
        if (type.IsArray && type.GetElementType()?.IsEnum == true)
            return type.GetElementType();

        // Generic IEnumerable<EnumType>
        if (type.IsGenericType && type.GetGenericArguments().Length == 1)
        {
            var genericArg = type.GetGenericArguments()[0];
            if (genericArg.IsEnum)
                return genericArg;
        }

        return null;
    }
}
