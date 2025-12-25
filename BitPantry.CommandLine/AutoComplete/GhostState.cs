namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Represents the current state of ghost text suggestion.
/// </summary>
public sealed class GhostState
{
    /// <summary>
    /// Gets or sets the ghost text to display (the completion suffix).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the ghost text to display. Alias for <see cref="Text"/>.
    /// </summary>
    public string? GhostText
    {
        get => Text;
        set => Text = value;
    }

    /// <summary>
    /// Gets or sets the current input text that triggered the ghost.
    /// </summary>
    public string? InputText { get; set; }

    /// <summary>
    /// Gets or sets the full suggestion (input + ghost).
    /// </summary>
    public string? FullSuggestion { get; set; }

    /// <summary>
    /// Gets or sets the column position where ghost text starts.
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// Gets or sets the source completion item for the ghost text.
    /// </summary>
    public CompletionItem? SourceItem { get; set; }

    /// <summary>
    /// Gets or sets the source of the ghost suggestion.
    /// </summary>
    public GhostSuggestionSource Source { get; set; }

    /// <summary>
    /// Gets whether ghost text is currently active.
    /// </summary>
    public bool IsActive => !string.IsNullOrEmpty(Text);

    /// <summary>
    /// Gets whether ghost text is visible (has text to display).
    /// </summary>
    public bool IsVisible => !string.IsNullOrEmpty(Text);

    /// <summary>
    /// Clears the ghost text state.
    /// </summary>
    public void Clear()
    {
        Text = null;
        InputText = null;
        FullSuggestion = null;
        StartColumn = 0;
        SourceItem = null;
        Source = GhostSuggestionSource.None;
    }

    /// <summary>
    /// Creates an empty ghost state.
    /// </summary>
    public static GhostState Empty => new();

    /// <summary>
    /// Creates a ghost state with the specified text.
    /// </summary>
    /// <param name="text">The ghost text to display.</param>
    /// <param name="startColumn">Where the ghost text starts.</param>
    /// <param name="sourceItem">The source completion item.</param>
    /// <returns>A new ghost state.</returns>
    public static GhostState Create(string text, int startColumn, CompletionItem sourceItem) => new()
    {
        Text = text,
        StartColumn = startColumn,
        SourceItem = sourceItem
    };

    /// <summary>
    /// Creates a ghost state from input and suggestion.
    /// </summary>
    /// <param name="inputText">The current input text.</param>
    /// <param name="fullSuggestion">The full suggested completion.</param>
    /// <param name="source">The source of the suggestion.</param>
    /// <returns>A new ghost state.</returns>
    public static GhostState FromSuggestion(string inputText, string fullSuggestion, GhostSuggestionSource source = GhostSuggestionSource.None)
    {
        if (string.IsNullOrEmpty(fullSuggestion) || !fullSuggestion.StartsWith(inputText, System.StringComparison.OrdinalIgnoreCase))
            return Empty;

        return new GhostState
        {
            InputText = inputText,
            FullSuggestion = fullSuggestion,
            Text = fullSuggestion.Substring(inputText.Length),
            StartColumn = inputText.Length,
            Source = source
        };
    }
}

/// <summary>
/// The source of a ghost text suggestion.
/// </summary>
public enum GhostSuggestionSource
{
    /// <summary>No source (no ghost).</summary>
    None,

    /// <summary>Suggestion from command history.</summary>
    History,

    /// <summary>Suggestion from registered command.</summary>
    Command
}
