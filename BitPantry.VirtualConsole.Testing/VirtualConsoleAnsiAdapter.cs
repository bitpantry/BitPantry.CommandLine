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

    /// <summary>
    /// Gets the underlying VirtualConsole for assertions and inspection.
    /// </summary>
    public VirtualConsole VirtualConsole => _virtualConsole;

    /// <summary>
    /// Creates a new adapter wrapping the specified VirtualConsole.
    /// </summary>
    /// <param name="virtualConsole">The VirtualConsole to write to.</param>
    public VirtualConsoleAnsiAdapter(VirtualConsole virtualConsole)
    {
        _virtualConsole = virtualConsole ?? throw new ArgumentNullException(nameof(virtualConsole));
        _writer = new VirtualConsoleTextWriter(_virtualConsole);
        
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

    /// <inheritdoc/>
    public IAnsiConsoleInput Input => _internalConsole.Input;

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

    /// <summary>
    /// TextWriter that writes directly to VirtualConsole.
    /// </summary>
    private class VirtualConsoleTextWriter : TextWriter
    {
        private readonly VirtualConsole _console;

        public VirtualConsoleTextWriter(VirtualConsole console)
        {
            _console = console;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            _console.Write(value.ToString());
        }

        public override void Write(string? value)
        {
            if (value != null)
            {
                _console.Write(value);
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
