using Spectre.Console;

namespace BitPantry.CommandLine.Input;

/// <summary>
/// Represents a segment of text with associated styling.
/// Immutable record type created by SyntaxHighlighter, consumed by rendering.
/// </summary>
/// <param name="Text">The text content of the segment.</param>
/// <param name="Start">Start position in input (0-based).</param>
/// <param name="End">End position in input (0-based, exclusive).</param>
/// <param name="Style">The Spectre.Console style to apply.</param>
public record StyledSegment(string Text, int Start, int End, Style Style);
