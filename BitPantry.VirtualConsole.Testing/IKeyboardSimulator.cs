namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// Interface for simulating keyboard input in CLI tests.
/// Provides async methods for typing text and pressing special keys.
/// 
/// <para>
/// All methods wait until the keys have been fully processed by the input loop
/// before returning, eliminating the need for artificial delays in tests.
/// </para>
/// </summary>
public interface IKeyboardSimulator
{
    /// <summary>
    /// Types a string of text and waits until all characters have been fully processed.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <returns>A Task that completes when all characters have been processed.</returns>
    Task TypeTextAsync(string text);

    /// <summary>
    /// Presses a specific console key and waits until it has been fully processed.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="shift">Whether shift is held.</param>
    /// <param name="alt">Whether alt is held.</param>
    /// <param name="control">Whether control is held.</param>
    /// <returns>A Task that completes when the key has been processed.</returns>
    Task PressKeyAsync(ConsoleKey key, bool shift = false, bool alt = false, bool control = false);

    /// <summary>
    /// Presses the Tab key and waits until it has been fully processed.
    /// </summary>
    Task PressTabAsync();

    /// <summary>
    /// Presses the Enter key and waits until it has been fully processed.
    /// </summary>
    Task PressEnterAsync();

    /// <summary>
    /// Presses the Escape key and waits until it has been fully processed.
    /// </summary>
    Task PressEscapeAsync();

    /// <summary>
    /// Presses the Backspace key and waits until it has been fully processed.
    /// </summary>
    Task PressBackspaceAsync();

    /// <summary>
    /// Presses the Down Arrow key and waits until it has been fully processed.
    /// </summary>
    Task PressDownArrowAsync();

    /// <summary>
    /// Presses the Up Arrow key and waits until it has been fully processed.
    /// </summary>
    Task PressUpArrowAsync();

    /// <summary>
    /// Presses the Right Arrow key and waits until it has been fully processed.
    /// </summary>
    Task PressRightArrowAsync();

    /// <summary>
    /// Presses the Left Arrow key and waits until it has been fully processed.
    /// </summary>
    Task PressLeftArrowAsync();
}
