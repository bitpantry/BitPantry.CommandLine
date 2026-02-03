namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// Interface for simulating keyboard input in autocomplete tests.
/// Provides methods for typing text and pressing special keys.
/// 
/// <para>
/// <b>Async vs Sync methods:</b>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Async methods (e.g., <see cref="TypeTextAsync"/>, <see cref="PressTabAsync"/>) wait until 
/// all keys have been fully processed by the input loop before returning. Use these in tests to avoid 
/// artificial delays.</description>
/// </item>
/// <item>
/// <description>Sync methods (e.g., <see cref="TypeText"/>, <see cref="PressTab"/>) queue keys and return 
/// immediately without waiting for processing. Use only for fire-and-forget scenarios.</description>
/// </item>
/// </list>
/// </summary>
public interface IKeyboardSimulator
{
    /// <summary>
    /// Types a string of text and waits until all characters have been fully processed.
    /// This is the preferred method for tests as it eliminates the need for artificial delays.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <returns>A Task that completes when all characters have been processed.</returns>
    Task TypeTextAsync(string text);

    /// <summary>
    /// Types a string of text synchronously (fire-and-forget).
    /// Does not wait for processing to complete. Consider using <see cref="TypeTextAsync"/> instead.
    /// </summary>
    /// <param name="text">The text to type.</param>
    void TypeText(string text);

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
    /// Presses a specific console key synchronously (fire-and-forget).
    /// </summary>
    void PressKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false);

    /// <summary>
    /// Presses the Tab key and waits until it has been fully processed.
    /// </summary>
    Task PressTabAsync();

    /// <summary>
    /// Presses the Tab key synchronously (fire-and-forget).
    /// </summary>
    void PressTab();

    /// <summary>
    /// Presses the Enter key and waits until it has been fully processed.
    /// </summary>
    Task PressEnterAsync();

    /// <summary>
    /// Presses the Enter key synchronously (fire-and-forget).
    /// </summary>
    void PressEnter();

    /// <summary>
    /// Presses the Escape key and waits until it has been fully processed.
    /// </summary>
    Task PressEscapeAsync();

    /// <summary>
    /// Presses the Escape key synchronously (fire-and-forget).
    /// </summary>
    void PressEscape();

    /// <summary>
    /// Presses the Backspace key and waits until it has been fully processed.
    /// </summary>
    Task PressBackspaceAsync();

    /// <summary>
    /// Presses the Backspace key synchronously (fire-and-forget).
    /// </summary>
    void PressBackspace();

    /// <summary>
    /// Presses the Down Arrow key and waits until it has been fully processed.
    /// </summary>
    Task PressDownArrowAsync();

    /// <summary>
    /// Presses the Down Arrow key synchronously (fire-and-forget).
    /// </summary>
    void PressDownArrow();

    /// <summary>
    /// Presses the Up Arrow key and waits until it has been fully processed.
    /// </summary>
    Task PressUpArrowAsync();

    /// <summary>
    /// Presses the Up Arrow key synchronously (fire-and-forget).
    /// </summary>
    void PressUpArrow();

    /// <summary>
    /// Presses the Right Arrow key and waits until it has been fully processed.
    /// </summary>
    Task PressRightArrowAsync();

    /// <summary>
    /// Presses the Right Arrow key synchronously (fire-and-forget).
    /// </summary>
    void PressRightArrow();

    /// <summary>
    /// Presses the Left Arrow key and waits until it has been fully processed.
    /// </summary>
    Task PressLeftArrowAsync();

    /// <summary>
    /// Presses the Left Arrow key synchronously (fire-and-forget).
    /// </summary>
    void PressLeftArrow();
}
