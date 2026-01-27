using BitPantry.CommandLine.Input;
using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// Manages ghost text autocomplete suggestions.
    /// Ghost text is displayed inline after the cursor in a dim style,
    /// and can be accepted (committed to buffer) or hidden.
    /// </summary>
    public class GhostTextController
    {
        private readonly IAnsiConsole _console;
        private string _text;
        private int _startPosition;
        private bool _isRendered;

        /// <summary>
        /// Creates a new GhostTextController.
        /// </summary>
        /// <param name="console">The console to render ghost text to.</param>
        public GhostTextController(IAnsiConsole console)
        {
            _console = console;
        }

        /// <summary>
        /// Whether ghost text is currently being shown.
        /// </summary>
        public bool IsShowing => !string.IsNullOrEmpty(_text);

        /// <summary>
        /// The current ghost text, or null if none is shown.
        /// </summary>
        public string Text => _text;

        /// <summary>
        /// Shows ghost text at the current cursor position.
        /// If ghost text is already showing, it is replaced.
        /// </summary>
        /// <param name="text">The ghost text to display.</param>
        /// <param name="line">The console line mirror for position tracking.</param>
        public void Show(string text, ConsoleLineMirror line)
        {
            // Clear any existing ghost text first
            Clear();

            if (string.IsNullOrEmpty(text))
            {
                _text = null;
                _startPosition = 0;
                _isRendered = false;
                return;
            }

            _text = text;
            _startPosition = line.BufferPosition;

            // Render immediately
            RenderToDisplay();
        }

        /// <summary>
        /// Clears the ghost text from the display and resets state.
        /// </summary>
        public void Clear()
        {
            if (_isRendered)
            {
                ClearFromDisplay();
            }

            _text = null;
            _startPosition = 0;
            _isRendered = false;
        }

        /// <summary>
        /// Accepts the current ghost text, committing it to the buffer and hiding.
        /// </summary>
        /// <param name="line">The console line mirror to write to.</param>
        public void Accept(ConsoleLineMirror line)
        {
            if (!IsShowing)
                return;

            var textToWrite = _text;

            // Clear ghost text from display and reset state
            Clear();

            // Write the text as normal text through ConsoleLineMirror (updates buffer)
            line.Write(textToWrite);
        }

        /// <summary>
        /// Renders the ghost text to the console in dim style.
        /// </summary>
        private void RenderToDisplay()
        {
            if (string.IsNullOrEmpty(_text))
                return;

            // Hide cursor during rendering to prevent flickering
            _console.Cursor.Hide();

            // Write ghost text directly to console in dim style (bypasses buffer)
            _console.Markup($"[dim]{_text.EscapeMarkup()}[/]");

            // Move cursor back to original position
            _console.Cursor.MoveLeft(_text.Length);

            _console.Cursor.Show();

            _isRendered = true;
        }

        /// <summary>
        /// Clears the rendered ghost text from the display.
        /// </summary>
        private void ClearFromDisplay()
        {
            if (string.IsNullOrEmpty(_text))
                return;

            // Hide cursor during clearing to prevent flickering
            _console.Cursor.Hide();

            // Write spaces directly to console (bypasses buffer)
            _console.Write(new string(' ', _text.Length));

            // Move cursor back
            _console.Cursor.MoveLeft(_text.Length);

            _console.Cursor.Show();

            _isRendered = false;
        }
    }
}
