using System;

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

    /// <inheritdoc />
    public bool Equals(CacheKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return CommandName == other.CommandName
            && ArgumentName == other.ArgumentName
            && PartialValue == other.PartialValue
            && ElementType == other.ElementType
            && ProviderType == other.ProviderType;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as CacheKey);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(CommandName, ArgumentName, PartialValue, ElementType, ProviderType);
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
    public CacheKey(string commandName, string argumentName, string partialValue)
    {
        CommandName = commandName;
        ArgumentName = argumentName;
        PartialValue = partialValue ?? string.Empty;
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
            ProviderType = context.CompletionAttribute?.ProviderType
        };
    }
}
