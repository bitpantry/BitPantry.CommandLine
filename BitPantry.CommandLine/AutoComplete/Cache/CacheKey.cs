using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete.Cache;

/// <summary>
/// Represents a unique key for cached completion results.
/// </summary>
public sealed class CacheKey : IEquatable<CacheKey>
{
    /// <summary>
    /// Gets the command name, if applicable.
    /// </summary>
    public string? CommandName { get; init; }

    /// <summary>
    /// Gets the argument name, if applicable.
    /// </summary>
    public string? ArgumentName { get; init; }

    /// <summary>
    /// Gets the partial value used to generate completions.
    /// </summary>
    public string PartialValue { get; init; } = string.Empty;

    /// <summary>
    /// Gets the element type being completed.
    /// </summary>
    public CompletionElementType ElementType { get; init; }

    /// <summary>
    /// Gets the provider type used for completions, if any.
    /// </summary>
    public Type? ProviderType { get; init; }

    /// <summary>
    /// Gets the hash of used arguments to ensure cache invalidation when args change.
    /// </summary>
    public int UsedArgumentsHash { get; init; }

    /// <inheritdoc />
    public bool Equals(CacheKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return CommandName == other.CommandName
            && ArgumentName == other.ArgumentName
            && PartialValue == other.PartialValue
            && ElementType == other.ElementType
            && ProviderType == other.ProviderType
            && UsedArgumentsHash == other.UsedArgumentsHash;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as CacheKey);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(CommandName, ArgumentName, PartialValue, ElementType, ProviderType, UsedArgumentsHash);
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public CacheKey() { }

    /// <summary>
    /// Creates a cache key with the specified values.
    /// </summary>
    /// <param name="commandName">The command name.</param>
    /// <param name="argumentName">The argument name.</param>
    /// <param name="partialValue">The partial value.</param>
    /// <param name="usedArguments">The set of already-used arguments.</param>
    public CacheKey(string commandName, string argumentName, string partialValue, ISet<string> usedArguments = null)
    {
        CommandName = commandName;
        ArgumentName = argumentName;
        PartialValue = partialValue ?? string.Empty;
        UsedArgumentsHash = ComputeUsedArgsHash(usedArguments);
    }

    /// <summary>
    /// Creates a cache key from a completion context.
    /// </summary>
    /// <param name="context">The completion context.</param>
    /// <returns>A cache key for the context.</returns>
    public static CacheKey FromContext(CompletionContext context)
    {
        return new CacheKey
        {
            CommandName = context.CommandName,
            ArgumentName = context.ArgumentName,
            PartialValue = context.PartialValue,
            ElementType = context.ElementType,
            ProviderType = context.CompletionAttribute?.ProviderType,
            UsedArgumentsHash = ComputeUsedArgsHash(context.UsedArguments)
        };
    }

    /// <summary>
    /// Computes a hash from a set of used arguments for cache key comparison.
    /// </summary>
    private static int ComputeUsedArgsHash(ISet<string> usedArguments)
    {
        if (usedArguments == null || usedArguments.Count == 0)
            return 0;
        
        // Sort for consistent hashing regardless of insertion order
        var sorted = usedArguments.OrderBy(a => a, StringComparer.OrdinalIgnoreCase);
        var combined = string.Join("|", sorted);
        return combined.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
