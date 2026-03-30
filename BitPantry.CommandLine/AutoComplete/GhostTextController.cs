using System;
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
        private readonly Theme _theme;
        private int _promptLength;
        private string _text;
        private int _startPosition;
        private bool _isRendered;

        /// <summary>
        /// Creates a new GhostTextController.
        /// </summary>
        /// <param name="console">The console to render ghost text to.</param>
        /// <param name="theme">The theme providing the ghost text style.</param>
        /// <param name="promptLength">The length of the prompt in characters (for wrap-aware cursor movement).</param>
        public GhostTextController(IAnsiConsole console, Theme theme, int promptLength = 0)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));
            _promptLength = promptLength;
        }

        /// <summary>
        /// Whether ghost text is currently being shown.
        /// </summary>
        public bool IsShowing => !string.IsNullOrEmpty(_text);

        /// <summary>
        /// Sets the prompt length for wrap-aware cursor positioning.
        /// </summary>
        public void SetPromptLength(int promptLength)
        {
            _promptLength = promptLength;
        }

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

            // Write ghost text using theme's GhostText style (centralized dim style)
            _console.Write(new Text(_text, _theme.GhostText));

            // Move cursor back to original position (wrap-aware)
            MoveCursorBack(_text.Length);

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

            // Erase ghost text from cursor to end of display (handles multi-row ghost text)
            _console.Write("\x1B[0J");

            _console.Cursor.Show();

            _isRendered = false;
        }

        /// <summary>
        /// Moves the cursor back by the specified number of characters,
        /// handling row wrapping via CUU + CHA when the text crosses row boundaries.
        /// </summary>
        private void MoveCursorBack(int charCount)
        {
            int width = _console.Profile.Width;
            if (width <= 0) width = 80;

            int startOffset = _promptLength + _startPosition;
            int endOffset = startOffset + charCount;

            int fromRow = endOffset / width;
            int fromCol = endOffset % width;

            // Handle delayed wrap
            if (endOffset > 0 && endOffset % width == 0)
            {
                fromRow--;
                fromCol = width - 1;
            }

            int toRow = startOffset / width;
            int toCol = startOffset % width;

            int deltaRow = toRow - fromRow;
            if (deltaRow < 0)
                _console.Cursor.MoveUp(-deltaRow);
            else if (deltaRow > 0)
                _console.Cursor.MoveDown(deltaRow);

            if (fromCol != toCol || deltaRow != 0)
                _console.Write($"\x1B[{toCol + 1}G");
        }
    }
}
