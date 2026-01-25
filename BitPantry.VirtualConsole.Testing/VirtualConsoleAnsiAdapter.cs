using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// Adapter that implements Spectre.Console's IAnsiConsole interface and routes all output 
/// to a VirtualConsole instance. This enables testing of code that uses IAnsiConsole
/// by capturing the ANSI output in the VirtualConsole's screen buffer.
/// 
/// Uses delegation pattern: creates an internal AnsiConsole that writes to a TextWriter
/// which forwards output to VirtualConsole.
/// </summary>
public class VirtualConsoleAnsiAdapter : IAnsiConsole
{
    private readonly VirtualConsole _virtualConsole;
    private readonly VirtualConsoleTextWriter _writer;
    private readonly IAnsiConsole _internalConsole;
    private readonly TestConsoleInput _testInput;
    private readonly IKeyboardSimulator _keyboard;

    /// <summary>
    /// Gets the underlying VirtualConsole for low-level access and inspection.
    /// Prefer using the convenience methods on this adapter when possible.
    /// </summary>
    public VirtualConsole VirtualConsole => _virtualConsole;

    /// <summary>
    /// Gets the testable console input for simulating keyboard input.
    /// Use this to queue keystrokes that will be consumed by code calling ReadKey/ReadKeyAsync.
    /// </summary>
    public TestConsoleInput Input => _testInput;

    /// <summary>
    /// Gets the high-level keyboard simulator for convenient test input.
    /// Provides methods like TypeText(), PressTab(), PressEnter(), etc.
    /// </summary>
    public IKeyboardSimulator Keyboard => _keyboard;

    /// <summary>
    /// When true, all writes are logged to <see cref="WriteLog"/>.
    /// This captures transient content (like progress bars with AutoClear) that would
    /// otherwise be erased from the screen buffer before assertions can inspect it.
    /// Default is false for performance.
    /// </summary>
    public bool WriteLogEnabled { get; set; } = false;

    /// <summary>
    /// Gets all text written to the console when <see cref="WriteLogEnabled"/> is true.
    /// This includes raw ANSI sequences and content that was later cleared.
    /// Useful for verifying that transient UI elements (progress bars, spinners) were displayed.
    /// </summary>
    public VirtualConsoleWriteLog WriteLog { get; } = new VirtualConsoleWriteLog();

    /// <summary>
    /// Creates a new adapter wrapping the specified VirtualConsole.
    /// </summary>
    /// <param name="virtualConsole">The VirtualConsole to write to.</param>
    public VirtualConsoleAnsiAdapter(VirtualConsole virtualConsole)
        : this(virtualConsole, new TestConsoleInput())
    {
    }

    /// <summary>
    /// Creates a new adapter wrapping the specified VirtualConsole with custom input.
    /// </summary>
    /// <param name="virtualConsole">The VirtualConsole to write to.</param>
    /// <param name="testInput">The testable input source for keyboard simulation.</param>
    public VirtualConsoleAnsiAdapter(VirtualConsole virtualConsole, TestConsoleInput testInput)
    {
        _virtualConsole = virtualConsole ?? throw new ArgumentNullException(nameof(virtualConsole));
        _testInput = testInput ?? throw new ArgumentNullException(nameof(testInput));
        _keyboard = new KeyboardSimulator(_testInput);
        _writer = new VirtualConsoleTextWriter(this);
        
        // Create a custom output that properly reports VirtualConsole dimensions.
        // This is critical - using AnsiConsoleOutput would fall back to Console.BufferWidth
        // which doesn't reflect our virtual terminal size.
        var output = new VirtualConsoleOutput(_writer, _virtualConsole);
        
        // Create an internal AnsiConsole that writes to our VirtualConsole-backed TextWriter
        _internalConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Interactive = InteractionSupport.Yes,
            Out = output
        });
        
        // Also explicitly set Profile dimensions as a safeguard
        _internalConsole.Profile.Width = _virtualConsole.Width;
        _internalConsole.Profile.Height = _virtualConsole.Height;
    }

    // Delegate all IAnsiConsole members to the internal console
    
    /// <inheritdoc/>
    public Profile Profile => _internalConsole.Profile;

    /// <inheritdoc/>
    public IAnsiConsoleCursor Cursor => _internalConsole.Cursor;

    /// <summary>
    /// Gets the input for Spectre.Console's IAnsiConsole interface.
    /// Returns the TestConsoleInput cast to IAnsiConsoleInput.
    /// </summary>
    IAnsiConsoleInput IAnsiConsole.Input => _testInput;

    /// <inheritdoc/>
    public IExclusivityMode ExclusivityMode => _internalConsole.ExclusivityMode;

    /// <inheritdoc/>
    public RenderPipeline Pipeline => _internalConsole.Pipeline;

    /// <inheritdoc/>
    public void Clear(bool home)
    {
        _virtualConsole.Clear();
    }

    /// <inheritdoc/>
    public void Write(IRenderable renderable)
    {
        _internalConsole.Write(renderable);
        _writer.Flush();
    }

    #region Screen Inspection

    /// <summary>
    /// Gets the width of the virtual console in columns.
    /// </summary>
    public int Width => _virtualConsole.Width;

    /// <summary>
    /// Gets the height of the virtual console in rows.
    /// </summary>
    public int Height => _virtualConsole.Height;

    /// <summary>
    /// Gets the current cursor row (0-based).
    /// </summary>
    public int CursorRow => _virtualConsole.CursorRow;

    /// <summary>
    /// Gets the current cursor column (0-based).
    /// </summary>
    public int CursorColumn => _virtualConsole.CursorColumn;

    /// <summary>
    /// Gets the cell at the specified position.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="column">Column (0-based).</param>
    /// <returns>The cell at the position.</returns>
    public ScreenCell GetCell(int row, int column) => _virtualConsole.GetCell(row, column);

    /// <summary>
    /// Gets a row wrapper for the specified row.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <returns>A ScreenRow wrapper.</returns>
    public ScreenRow GetRow(int row) => _virtualConsole.GetRow(row);

    /// <summary>
    /// Gets the text content of a specific row.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <returns>The text content of the row.</returns>
    public string GetRowText(int row) => _virtualConsole.GetRow(row).GetText();

    /// <summary>
    /// Gets the trimmed text content of a specific row.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <returns>The trimmed text content of the row.</returns>
    public string GetRowTextTrimmed(int row) => GetRowText(row).TrimEnd();

    /// <summary>
    /// Gets the screen content as a string for debugging.
    /// </summary>
    public string GetScreenContent() => _virtualConsole.GetScreenContent();

    /// <summary>
    /// Gets the screen text without line breaks.
    /// </summary>
    public string GetScreenText() => _virtualConsole.GetScreenText();

    /// <summary>
    /// Gets the screen content as individual lines (for compatibility with VirtualAnsiConsole.Lines).
    /// </summary>
    public IReadOnlyList<string> Lines
    {
        get
        {
            var lines = new List<string>();
            for (int i = 0; i < _virtualConsole.Height; i++)
            {
                lines.Add(_virtualConsole.GetRow(i).GetText());
            }
            return lines;
        }
    }

    #endregion

    /// <summary>
    /// TextWriter that writes directly to VirtualConsole and optionally logs all writes.
    /// </summary>
    private class VirtualConsoleTextWriter : TextWriter
    {
        private readonly VirtualConsoleAnsiAdapter _adapter;

        public VirtualConsoleTextWriter(VirtualConsoleAnsiAdapter adapter)
        {
            _adapter = adapter;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            var text = value.ToString();
            if (_adapter.WriteLogEnabled)
            {
                _adapter.WriteLog.Append(text);
            }
            _adapter._virtualConsole.Write(text);
        }

        public override void Write(string? value)
        {
            if (value != null)
            {
                if (_adapter.WriteLogEnabled)
                {
                    _adapter.WriteLog.Append(value);
                }
                _adapter._virtualConsole.Write(value);
            }
        }
    }
    
    /// <summary>
    /// Custom IAnsiConsoleOutput that correctly reports VirtualConsole dimensions.
    /// This ensures Spectre.Console renders content to fit within the virtual terminal.
    /// </summary>
    private class VirtualConsoleOutput : IAnsiConsoleOutput
    {
        private readonly VirtualConsoleTextWriter _writer;
        private readonly VirtualConsole _console;
        
        public VirtualConsoleOutput(VirtualConsoleTextWriter writer, VirtualConsole console)
        {
            _writer = writer;
            _console = console;
        }
        
        public TextWriter Writer => _writer;
        
        public bool IsTerminal => true;
        
        public int Width => _console.Width;
        
        public int Height => _console.Height;
        
        public void SetEncoding(Encoding encoding)
        {
            // VirtualConsole always uses UTF-8
        }
    }
}
