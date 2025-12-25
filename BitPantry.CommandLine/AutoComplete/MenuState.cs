using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Represents the current state of the completion menu for rendering.
/// </summary>
public sealed class MenuState
{
    /// <summary>
    /// Gets or sets the list of items currently displayed in the menu.
    /// </summary>
    public IReadOnlyList<CompletionItem> Items { get; init; } = [];

    /// <summary>
    /// Gets or sets the index of the currently selected item.
    /// </summary>
    public int SelectedIndex { get; set; }

    /// <summary>
    /// Gets or sets the current filter text.
    /// </summary>
    public string FilterText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the menu is loading results.
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets the error message to display, if any.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the total number of available items (before filtering).
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of visible items in the menu.
    /// </summary>
    public int MaxVisibleItems { get; set; } = 10;

    /// <summary>
    /// Gets the currently selected item, or null if no selection.
    /// </summary>
    public CompletionItem? SelectedItem =>
        SelectedIndex >= 0 && SelectedIndex < Items.Count
            ? Items[SelectedIndex]
            : null;

    /// <summary>
    /// Gets whether the menu has any items to display.
    /// </summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// Gets or sets the starting index of the viewport (for scrolling).
    /// </summary>
    public int ViewportStart { get; set; }

    /// <summary>
    /// Gets or sets the number of items visible in the viewport.
    /// </summary>
    public int ViewportSize { get; set; } = 10;

    /// <summary>
    /// Moves selection up in the menu (wrapping to bottom).
    /// </summary>
    public void MoveUp()
    {
        if (Items.Count == 0) return;
        SelectedIndex = SelectedIndex <= 0 ? Items.Count - 1 : SelectedIndex - 1;
    }

    /// <summary>
    /// Moves selection down in the menu (wrapping to top).
    /// </summary>
    public void MoveDown()
    {
        if (Items.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Items.Count;
    }
}
