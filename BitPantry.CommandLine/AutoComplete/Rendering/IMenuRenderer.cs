using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete.Rendering;

/// <summary>
/// Renders autocomplete menu with in-place updates using LiveRenderable pattern.
/// </summary>
public interface IMenuRenderer
{
    /// <summary>
    /// Gets whether the menu is currently visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Shows the menu with the specified items and selection.
    /// </summary>
    /// <param name="items">Menu items to display.</param>
    /// <param name="selectedIndex">Index of selected item.</param>
    /// <param name="viewportStart">First visible item index for scrolling.</param>
    /// <param name="viewportSize">Maximum items to display.</param>
    void Show(IReadOnlyList<string> items, int selectedIndex, int viewportStart, int viewportSize);

    /// <summary>
    /// Updates the menu in-place without flicker.
    /// Uses Inflate pattern to track max height and prevent phantom lines.
    /// </summary>
    void Update(IReadOnlyList<string> items, int selectedIndex, int viewportStart, int viewportSize);

    /// <summary>
    /// Hides and clears the menu, restoring cursor to input line.
    /// </summary>
    void Hide();
}
