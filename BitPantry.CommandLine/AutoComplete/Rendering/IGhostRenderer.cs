namespace BitPantry.CommandLine.AutoComplete.Rendering;

/// <summary>
/// Abstraction for ghost text rendering.
/// </summary>
public interface IGhostRenderer
{
    /// <summary>
    /// Gets whether ghost text is currently visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Shows ghost text at current cursor position.
    /// </summary>
    /// <param name="text">Ghost text to display (portion after user input).</param>
    void Show(string text);

    /// <summary>
    /// Updates visible ghost text to new value.
    /// </summary>
    /// <param name="text">New ghost text to display.</param>
    void Update(string text);

    /// <summary>
    /// Clears any visible ghost text.
    /// </summary>
    void Clear();
}
