namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// The result of a completion action, indicating what the caller should do.
/// </summary>
public sealed class CompletionAction
{
    /// <summary>
    /// The type of action to take.
    /// </summary>
    public CompletionActionType Type { get; init; }

    /// <summary>
    /// Text to insert into the input buffer, if any.
    /// </summary>
    public string? InsertText { get; init; }

    /// <summary>
    /// Whether the menu state changed and needs re-rendering.
    /// </summary>
    public bool RequiresMenuRedraw { get; init; }

    /// <summary>
    /// Whether the input line needs re-rendering.
    /// </summary>
    public bool RequiresInputRedraw { get; init; }

    /// <summary>
    /// Error message to display, if applicable.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The menu state for rendering, if applicable.
    /// </summary>
    public MenuState? MenuState { get; init; }

    /// <summary>
    /// No action needed.
    /// </summary>
    public static CompletionAction None() => new() { Type = CompletionActionType.None };

    /// <summary>
    /// Close the menu without inserting.
    /// </summary>
    public static CompletionAction Close() => new()
    {
        Type = CompletionActionType.CloseMenu,
        RequiresMenuRedraw = true
    };

    /// <summary>
    /// Creates an insert text action.
    /// </summary>
    public static CompletionAction Accept(string text) => new()
    {
        Type = CompletionActionType.InsertText,
        InsertText = text,
        RequiresMenuRedraw = true,
        RequiresInputRedraw = true
    };

    /// <summary>
    /// Creates a show menu action with the initial menu state.
    /// </summary>
    public static CompletionAction ShowMenu(MenuState state) => new()
    {
        Type = CompletionActionType.OpenMenu,
        MenuState = state,
        RequiresMenuRedraw = true
    };

    /// <summary>
    /// Creates an update menu action with the new menu state.
    /// </summary>
    public static CompletionAction UpdateMenu(MenuState state) => new()
    {
        Type = CompletionActionType.SelectionChanged,
        MenuState = state,
        RequiresMenuRedraw = true
    };

    /// <summary>
    /// Creates a no matches action.
    /// </summary>
    public static CompletionAction NoMatches() => new()
    {
        Type = CompletionActionType.NoMatches
    };

    /// <summary>
    /// Creates a loading action.
    /// </summary>
    public static CompletionAction Loading() => new()
    {
        Type = CompletionActionType.Loading
    };

    /// <summary>
    /// Creates an error action.
    /// </summary>
    public static CompletionAction Error(string message) => new()
    {
        Type = CompletionActionType.Error,
        ErrorMessage = message
    };
}

/// <summary>
/// The type of completion action.
/// </summary>
public enum CompletionActionType
{
    /// <summary>No action needed.</summary>
    None,

    /// <summary>Open/show the completion menu.</summary>
    OpenMenu,

    /// <summary>Close the completion menu.</summary>
    CloseMenu,

    /// <summary>Insert text from selected completion.</summary>
    InsertText,

    /// <summary>Menu selection changed.</summary>
    SelectionChanged,

    /// <summary>Menu items filtered.</summary>
    FilterChanged,

    /// <summary>Show loading indicator.</summary>
    Loading,

    /// <summary>Show error message.</summary>
    Error,

    /// <summary>No matches found.</summary>
    NoMatches
}
