using System.Collections.Generic;
using BitPantry.CommandLine.Component;

namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Context information passed to autocomplete handlers.
/// </summary>
public class AutoCompleteContext
{
    /// <summary>
    /// Current partial input being typed.
    /// </summary>
    public required string QueryString { get; init; }

    /// <summary>
    /// Complete input line.
    /// </summary>
    public required string FullInput { get; init; }

    /// <summary>
    /// Cursor position (1-based).
    /// </summary>
    public required int CursorPosition { get; init; }

    /// <summary>
    /// Metadata about argument being completed.
    /// </summary>
    public required ArgumentInfo ArgumentInfo { get; init; }

    /// <summary>
    /// Already-provided argument values.
    /// </summary>
    public required IReadOnlyDictionary<ArgumentInfo, string> ProvidedValues { get; init; }

    /// <summary>
    /// Command being executed.
    /// </summary>
    public required CommandInfo CommandInfo { get; init; }
}
