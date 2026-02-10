using System;
using System.Threading.Tasks;

namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// Implementation of IKeyboardSimulator that wraps a TestConsoleInput.
/// Provides a convenient high-level API for simulating keyboard input in CLI tests.
/// 
/// All methods wait until the keys have been fully processed by the input loop
/// before returning, eliminating the need for artificial delays in tests.
/// </summary>
public class KeyboardSimulator : IKeyboardSimulator
{
    private readonly TestConsoleInput _input;

    /// <summary>
    /// Creates a new KeyboardSimulator that pushes keys to the specified input queue.
    /// </summary>
    /// <param name="input">The testable console input to push keys to.</param>
    public KeyboardSimulator(TestConsoleInput input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    /// <inheritdoc/>
    public Task TypeTextAsync(string text)
    {
        return _input.PushTextAsync(text);
    }

    /// <inheritdoc/>
    public Task PressKeyAsync(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
    {
        return _input.PushKeyAsync(key, shift, alt, control);
    }

    /// <inheritdoc/>
    public Task PressTabAsync()
    {
        return _input.PushKeyAsync(ConsoleKey.Tab);
    }

    /// <inheritdoc/>
    public Task PressEnterAsync()
    {
        return _input.PushKeyAsync(ConsoleKey.Enter);
    }

    /// <inheritdoc/>
    public Task PressEscapeAsync()
    {
        return _input.PushKeyAsync(ConsoleKey.Escape);
    }

    /// <inheritdoc/>
    public Task PressBackspaceAsync()
    {
        return _input.PushKeyAsync(ConsoleKey.Backspace);
    }

    /// <inheritdoc/>
    public Task PressDownArrowAsync()
    {
        return _input.PushKeyAsync(ConsoleKey.DownArrow);
    }

    /// <inheritdoc/>
    public Task PressUpArrowAsync()
    {
        return _input.PushKeyAsync(ConsoleKey.UpArrow);
    }

    /// <inheritdoc/>
    public Task PressRightArrowAsync()
    {
        return _input.PushKeyAsync(ConsoleKey.RightArrow);
    }

    /// <inheritdoc/>
    public Task PressLeftArrowAsync()
    {
        return _input.PushKeyAsync(ConsoleKey.LeftArrow);
    }

    /// <inheritdoc/>
    public async Task SubmitAsync(string command)
    {
        await _input.PushTextAsync(command);
        await _input.PushKeyAsync(ConsoleKey.Enter);
    }
}
