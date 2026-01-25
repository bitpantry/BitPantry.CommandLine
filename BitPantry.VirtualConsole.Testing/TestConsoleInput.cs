using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// A queue-based implementation of IAnsiConsoleInput for testing CLI applications.
/// Allows tests to simulate keyboard input by queuing keystrokes that will be
/// consumed by code calling ReadKey/ReadKeyAsync.
/// 
/// When the queue is empty, ReadKeyAsync blocks (like a real console) until:
/// - A key is pushed to the queue, OR
/// - The cancellation token fires
/// </summary>
public class TestConsoleInput : IAnsiConsoleInput
{
    private readonly Queue<ConsoleKeyInfo> _queue = new();
    private readonly object _lock = new();
    private readonly SemaphoreSlim _keyAvailable = new(0);

    /// <summary>
    /// Gets the number of keys currently in the queue.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    /// <summary>
    /// Pushes a single key to the input queue.
    /// </summary>
    /// <param name="key">The console key to push.</param>
    /// <param name="shift">Whether Shift is held.</param>
    /// <param name="alt">Whether Alt is held.</param>
    /// <param name="control">Whether Control is held.</param>
    public void PushKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
    {
        var keyChar = GetKeyChar(key, shift);
        PushKey(new ConsoleKeyInfo(keyChar, key, shift, alt, control));
    }

    /// <summary>
    /// Pushes a ConsoleKeyInfo directly to the input queue.
    /// </summary>
    /// <param name="keyInfo">The key info to push.</param>
    public void PushKey(ConsoleKeyInfo keyInfo)
    {
        lock (_lock)
        {
            _queue.Enqueue(keyInfo);
        }
        _keyAvailable.Release();
    }

    /// <summary>
    /// Pushes a string of text to the input queue, converting each character to a key.
    /// </summary>
    /// <param name="text">The text to push.</param>
    public void PushText(string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));

        foreach (var ch in text)
        {
            PushCharacter(ch);
        }
    }

    /// <summary>
    /// Pushes a string of text followed by Enter to the input queue.
    /// </summary>
    /// <param name="text">The text to push.</param>
    public void PushTextWithEnter(string text)
    {
        PushText(text);
        PushKey(ConsoleKey.Enter);
    }

    /// <summary>
    /// Pushes a single character to the input queue.
    /// </summary>
    /// <param name="ch">The character to push.</param>
    public void PushCharacter(char ch)
    {
        var key = CharToConsoleKey(ch, out var shift);
        PushKey(new ConsoleKeyInfo(ch, key, shift, false, false));
    }

    /// <summary>
    /// Clears all pending keys from the queue.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
        }
    }

    /// <inheritdoc/>
    public bool IsKeyAvailable()
    {
        lock (_lock)
        {
            return _queue.Count > 0;
        }
    }

    /// <inheritdoc/>
    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        // Block until a key is available (no timeout - matches real console behavior)
        _keyAvailable.Wait();
        
        lock (_lock)
        {
            return _queue.Dequeue();
        }
    }

    /// <inheritdoc/>
    public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        // Block until a key is available or cancellation is requested
        await _keyAvailable.WaitAsync(cancellationToken);
        
        lock (_lock)
        {
            return _queue.Dequeue();
        }
    }

    /// <summary>
    /// Gets the character representation for a console key.
    /// </summary>
    private static char GetKeyChar(ConsoleKey key, bool shift)
    {
        // Handle common special keys
        return key switch
        {
            ConsoleKey.Enter => '\r',
            ConsoleKey.Tab => '\t',
            ConsoleKey.Escape => '\x1b',
            ConsoleKey.Backspace => '\b',
            ConsoleKey.Spacebar => ' ',
            // Letter keys
            >= ConsoleKey.A and <= ConsoleKey.Z => shift 
                ? (char)('A' + (key - ConsoleKey.A)) 
                : (char)('a' + (key - ConsoleKey.A)),
            // Number keys
            >= ConsoleKey.D0 and <= ConsoleKey.D9 => (char)('0' + (key - ConsoleKey.D0)),
            // Arrow keys and other non-character keys
            _ => '\0'
        };
    }

    /// <summary>
    /// Converts a character to its ConsoleKey equivalent.
    /// </summary>
    private static ConsoleKey CharToConsoleKey(char ch, out bool shift)
    {
        shift = false;

        // Uppercase letters
        if (ch >= 'A' && ch <= 'Z')
        {
            shift = true;
            return (ConsoleKey)ch;
        }

        // Lowercase letters
        if (ch >= 'a' && ch <= 'z')
        {
            return (ConsoleKey)(ch - 32); // Convert to uppercase for ConsoleKey
        }

        // Numbers
        if (ch >= '0' && ch <= '9')
        {
            return (ConsoleKey)(ch - '0' + (int)ConsoleKey.D0);
        }

        // Special characters
        return ch switch
        {
            ' ' => ConsoleKey.Spacebar,
            '\r' or '\n' => ConsoleKey.Enter,
            '\t' => ConsoleKey.Tab,
            '\b' => ConsoleKey.Backspace,
            '\x1b' => ConsoleKey.Escape,
            '-' => ConsoleKey.OemMinus,
            '/' => ConsoleKey.Oem2,
            '\\' => ConsoleKey.Oem5,
            '.' => ConsoleKey.OemPeriod,
            ',' => ConsoleKey.OemComma,
            _ => ConsoleKey.NoName
        };
    }
}
