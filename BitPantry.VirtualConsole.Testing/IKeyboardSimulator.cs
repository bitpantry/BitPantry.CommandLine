namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// Interface for simulating keyboard input in autocomplete tests.
/// Provides methods for typing text and pressing special keys.
/// </summary>
public interface IKeyboardSimulator
{
    /// <summary>
    /// Types a string of text, processing each character as keyboard input.
    /// </summary>
    /// <param name="text">The text to type.</param>
    Task TypeTextAsync(string text);

    /// <summary>
    /// Types a string of text synchronously.
    /// </summary>
    /// <param name="text">The text to type.</param>
    void TypeText(string text);

    /// <summary>
    /// Presses a specific console key.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="shift">Whether shift is held.</param>
    /// <param name="alt">Whether alt is held.</param>
    /// <param name="control">Whether control is held.</param>
    Task PressKeyAsync(ConsoleKey key, bool shift = false, bool alt = false, bool control = false);

    /// <summary>
    /// Presses a specific console key synchronously.
    /// </summary>
    void PressKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false);

    /// <summary>
    /// Presses the Tab key.
    /// </summary>
    Task PressTabAsync();

    /// <summary>
    /// Presses the Tab key synchronously.
    /// </summary>
    void PressTab();

    /// <summary>
    /// Presses the Enter key.
    /// </summary>
    Task PressEnterAsync();

    /// <summary>
    /// Presses the Enter key synchronously.
    /// </summary>
    void PressEnter();

    /// <summary>
    /// Presses the Escape key.
    /// </summary>
    Task PressEscapeAsync();

    /// <summary>
    /// Presses the Escape key synchronously.
    /// </summary>
    void PressEscape();

    /// <summary>
    /// Presses the Backspace key.
    /// </summary>
    Task PressBackspaceAsync();

    /// <summary>
    /// Presses the Backspace key synchronously.
    /// </summary>
    void PressBackspace();

    /// <summary>
    /// Presses the Down Arrow key.
    /// </summary>
    Task PressDownArrowAsync();

    /// <summary>
    /// Presses the Down Arrow key synchronously.
    /// </summary>
    void PressDownArrow();

    /// <summary>
    /// Presses the Up Arrow key.
    /// </summary>
    Task PressUpArrowAsync();

    /// <summary>
    /// Presses the Up Arrow key synchronously.
    /// </summary>
    void PressUpArrow();

    /// <summary>
    /// Presses the Right Arrow key.
    /// </summary>
    Task PressRightArrowAsync();

    /// <summary>
    /// Presses the Right Arrow key synchronously.
    /// </summary>
    void PressRightArrow();

    /// <summary>
    /// Presses the Left Arrow key.
    /// </summary>
    Task PressLeftArrowAsync();

    /// <summary>
    /// Presses the Left Arrow key synchronously.
    /// </summary>
    void PressLeftArrow();
}
