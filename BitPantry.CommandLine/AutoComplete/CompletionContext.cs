using System;
using System.Collections.Generic;
using BitPantry.CommandLine.AutoComplete.Attributes;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Provides context for completion providers to generate suggestions.
/// </summary>
public sealed class CompletionContext
{
    /// <summary>
    /// The current input buffer text up to the cursor position.
    /// </summary>
    public string InputText { get; init; } = string.Empty;

    /// <summary>
    /// The full input text (alias for InputText for compatibility).
    /// </summary>
    public string FullInput
    {
        get => InputText;
        init => InputText = value;
    }

    /// <summary>
    /// The cursor position within the input buffer.
    /// </summary>
    public int CursorPosition { get; init; }

    /// <summary>
    /// The name of the command being completed, if known.
    /// </summary>
    public string? CommandName { get; init; }

    /// <summary>
    /// The name of the argument being completed, if applicable.
    /// </summary>
    public string? ArgumentName { get; init; }

    /// <summary>
    /// The partial value typed so far for the current token.
    /// </summary>
    public string PartialValue { get; init; } = string.Empty;

    /// <summary>
    /// The current word being typed (alias for PartialValue for compatibility).
    /// </summary>
    public string CurrentWord
    {
        get => PartialValue;
        init => PartialValue = value;
    }

    /// <summary>
    /// The type of element being completed.
    /// </summary>
    public CompletionElementType ElementType { get; init; }

    /// <summary>
    /// The CLR type of the property being completed (for enum detection, etc.).
    /// </summary>
    public Type? PropertyType { get; init; }

    /// <summary>
    /// The [Completion] attribute on the property, if any.
    /// </summary>
    public CompletionAttribute? CompletionAttribute { get; init; }

    /// <summary>
    /// Whether the completion is for a remote command.
    /// </summary>
    public bool IsRemote { get; init; }

    /// <summary>
    /// Service provider for resolving dependencies within providers.
    /// </summary>
    public IServiceProvider Services { get; init; } = null!;

    /// <summary>
    /// The command instance, if available (for method-based completion).
    /// </summary>
    public object? CommandInstance { get; init; }

    /// <summary>
    /// Dictionary of already-parsed argument values from the current input.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? ParsedArguments { get; init; }

    /// <summary>
    /// Set of argument names that have already been used in the current input.
    /// Used to exclude already-specified arguments from completion.
    /// </summary>
    public ISet<string> UsedArguments { get; init; } = new HashSet<string>();
}
