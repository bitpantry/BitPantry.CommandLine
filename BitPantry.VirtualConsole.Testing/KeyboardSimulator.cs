using System;
using System.Threading.Tasks;

namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// Implementation of IKeyboardSimulator that wraps a TestConsoleInput.
/// Provides a convenient high-level API for simulating keyboard input in CLI tests.
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
        TypeText(text);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void TypeText(string text)
    {
        _input.PushText(text);
    }

    /// <inheritdoc/>
    public Task PressKeyAsync(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
    {
        PressKey(key, shift, alt, control);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void PressKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
    {
        _input.PushKey(key, shift, alt, control);
    }

    /// <inheritdoc/>
    public Task PressTabAsync()
    {
        PressTab();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void PressTab()
    {
        _input.PushKey(ConsoleKey.Tab);
    }

    /// <inheritdoc/>
    public Task PressEnterAsync()
    {
        PressEnter();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void PressEnter()
    {
        _input.PushKey(ConsoleKey.Enter);
    }

    /// <inheritdoc/>
    public Task PressEscapeAsync()
    {
        PressEscape();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void PressEscape()
    {
        _input.PushKey(ConsoleKey.Escape);
    }

    /// <inheritdoc/>
    public Task PressBackspaceAsync()
    {
        PressBackspace();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void PressBackspace()
    {
        _input.PushKey(ConsoleKey.Backspace);
    }

    /// <inheritdoc/>
    public Task PressDownArrowAsync()
    {
        PressDownArrow();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void PressDownArrow()
    {
        _input.PushKey(ConsoleKey.DownArrow);
    }

    /// <inheritdoc/>
    public Task PressUpArrowAsync()
    {
        PressUpArrow();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void PressUpArrow()
    {
        _input.PushKey(ConsoleKey.UpArrow);
    }

    /// <inheritdoc/>
    public Task PressRightArrowAsync()
    {
        PressRightArrow();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void PressRightArrow()
    {
        _input.PushKey(ConsoleKey.RightArrow);
    }

    /// <inheritdoc/>
    public Task PressLeftArrowAsync()
    {
        PressLeftArrow();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void PressLeftArrow()
    {
        _input.PushKey(ConsoleKey.LeftArrow);
    }
}
