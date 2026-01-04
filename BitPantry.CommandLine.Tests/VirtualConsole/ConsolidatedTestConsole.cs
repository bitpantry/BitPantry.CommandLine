using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;
using System;
using System.Collections.Generic;
using System.IO;

namespace BitPantry.CommandLine.Tests.VirtualConsole;

/// <summary>
/// Consolidated test console that wraps Spectre's TestConsole and adds cursor position tracking.
/// This combines the benefits of Spectre's IAnsiConsole implementation with our need for
/// cursor position tracking for visual tests.
/// </summary>
public class ConsolidatedTestConsole : IAnsiConsole, IDisposable
{
    private readonly TestConsole _inner;
    private readonly CursorTracker _cursorTracker;
    private bool _emitAnsiSequences;

    /// <summary>
    /// Gets the raw output written to the console.
    /// </summary>
    public string Output => _inner.Output;

    /// <summary>
    /// Gets the raw output buffer (alias for Output).
    /// </summary>
    public string Buffer => _inner.Output;

    /// <summary>
    /// Gets the output split into lines.
    /// </summary>
    public IReadOnlyList<string> Lines => _inner.Lines;

    /// <summary>
    /// Gets the console input for queueing keystrokes.
    /// </summary>
    public TestConsoleInput Input => _inner.Input;

    /// <summary>
    /// Gets the current cursor position as tracked through ANSI sequences.
    /// </summary>
    public (int Column, int Line) CursorPosition => _cursorTracker.Position;

    /// <inheritdoc/>
    public Profile Profile => _inner.Profile;

    /// <inheritdoc/>
    public IExclusivityMode ExclusivityMode => _inner.ExclusivityMode;

    /// <inheritdoc/>
    public RenderPipeline Pipeline => _inner.Pipeline;

    /// <inheritdoc/>
    public IAnsiConsoleCursor Cursor => _inner.Cursor;

    /// <inheritdoc/>
    IAnsiConsoleInput IAnsiConsole.Input => _inner.Input;

    /// <summary>
    /// Creates a new ConsolidatedTestConsole with default settings.
    /// </summary>
    public ConsolidatedTestConsole()
    {
        _inner = new TestConsole();
        _cursorTracker = new CursorTracker();
        _emitAnsiSequences = false;
    }

    /// <summary>
    /// Configures the console width.
    /// </summary>
    public ConsolidatedTestConsole Width(int width)
    {
        _inner.Profile.Width = width;
        return this;
    }

    /// <summary>
    /// Configures the console height.
    /// </summary>
    public ConsolidatedTestConsole Height(int height)
    {
        _inner.Profile.Height = height;
        return this;
    }

    /// <summary>
    /// Enables ANSI sequence emission for testing control codes.
    /// </summary>
    public ConsolidatedTestConsole EmitAnsiSequences()
    {
        _emitAnsiSequences = true;
        _inner.EmitAnsiSequences = true;
        return this;
    }

    /// <summary>
    /// Marks the console as interactive. Required for tests that simulate user input.
    /// </summary>
    public ConsolidatedTestConsole Interactive()
    {
        _inner.Profile.Capabilities.Interactive = true;
        return this;
    }

    /// <summary>
    /// Sets ANSI support capability for the console.
    /// </summary>
    public ConsolidatedTestConsole SupportsAnsi(bool supportsAnsi = true)
    {
        _inner.Profile.Capabilities.Ansi = supportsAnsi;
        return this;
    }

    /// <summary>
    /// Sets both width and height of the console.
    /// </summary>
    public ConsolidatedTestConsole Size(int width, int height)
    {
        _inner.Profile.Width = width;
        _inner.Profile.Height = height;
        return this;
    }

    /// <summary>
    /// Configures the console to use true color.
    /// </summary>
    public ConsolidatedTestConsole Colors(ColorSystem colorSystem)
    {
        _inner.Profile.Capabilities.ColorSystem = colorSystem;
        return this;
    }

    /// <inheritdoc/>
    public void Clear(bool home)
    {
        _inner.Clear(home);
        if (home)
        {
            _cursorTracker.Reset();
        }
    }

    /// <inheritdoc/>
    public void Write(IRenderable renderable)
    {
        // First, delegate to Spectre's TestConsole
        _inner.Write(renderable);

        // Then track cursor position by processing the segments
        foreach (var segment in renderable.GetSegments(this))
        {
            if (segment.IsControlCode)
            {
                _cursorTracker.ProcessText(segment.Text);
            }
            else
            {
                _cursorTracker.ProcessText(segment.Text);
            }
        }
    }

    /// <summary>
    /// Writes a line break to the console.
    /// </summary>
    public void WriteLine()
    {
        _inner.WriteLine();
        _cursorTracker.ProcessText("\n");
    }

    /// <summary>
    /// Writes text followed by a line break to the console.
    /// </summary>
    public void WriteLine(string text)
    {
        _inner.WriteLine(text);
        _cursorTracker.ProcessText(text);
        _cursorTracker.ProcessText("\n");
    }

    /// <summary>
    /// Gets the current cursor position.
    /// </summary>
    public (int Column, int Line) GetCursorPosition()
    {
        return _cursorTracker.Position;
    }

    /// <summary>
    /// Sets the cursor position directly.
    /// </summary>
    public void SetCursorPosition(int column, int line)
    {
        _cursorTracker.SetPosition(column, line);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _inner.Dispose();
    }
}
