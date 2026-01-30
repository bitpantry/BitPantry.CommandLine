using BitPantry.CommandLine.Input;
using Spectre.Console;
using System;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete.Rendering
{
    /// <summary>
    /// Renders the autocomplete menu below the input line.
    /// Supports in-place updates to avoid flickering.
    /// </summary>
    public class AutoCompleteMenuRenderer
    {
        private readonly IAnsiConsole _console;
        private SegmentShape? _currentShape;
        private bool _isVisible;
        private int _renderedLineCount;
        private int _savedCursorColumn; // 1-indexed screen column for ANSI CHA

        /// <summary>
        /// The indicator shown before the selected item.
        /// </summary>
        public const string SelectionIndicator = ">";

        /// <summary>
        /// Gets whether the menu is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Style for the selected (highlighted) item.
        /// </summary>
        private static readonly Style HighlightStyle = Style.Parse("invert");

        /// <summary>
        /// Creates a new AutoCompleteMenuRenderer.
        /// </summary>
        /// <param name="console">The console to render to.</param>
        public AutoCompleteMenuRenderer(IAnsiConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        /// <summary>
        /// Renders the menu for the first time.
        /// </summary>
        /// <param name="menu">The menu state to render.</param>
        /// <param name="line">The console line (provides cursor position context).</param>
        /// <param name="promptLength">Length of the prompt prefix (for cursor column calculation).</param>
        public void Render(AutoCompleteMenu menu, ConsoleLineMirror line, int promptLength = 0)
        {
            if (menu == null)
                throw new ArgumentNullException(nameof(menu));
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            if (menu.FilteredOptions.Count == 0)
            {
                Clear();
                return;
            }

            // Save cursor column (1-indexed for ANSI CHA)
            // Column = promptLength + bufferPosition + 1 (convert to 1-indexed)
            _savedCursorColumn = promptLength + line.BufferPosition + 1;

            _isVisible = true;
            _renderedLineCount = RenderMenu(menu, isUpdate: false);

            // Move cursor back up to input line and to saved position
            RestoreCursorPosition();
        }

        /// <summary>
        /// Updates the menu in-place without flickering.
        /// </summary>
        /// <param name="menu">The menu state to render.</param>
        public void Update(AutoCompleteMenu menu)
        {
            if (menu == null)
                throw new ArgumentNullException(nameof(menu));

            if (!_isVisible)
            {
                // Can't update if not rendered - need a line context for first render
                return;
            }

            if (menu.FilteredOptions.Count == 0)
            {
                Clear();
                return;
            }

            // Move cursor back to start of menu (we're on input line)
            // Move down one line to first menu line
            _console.Write(new ControlCode(AnsiCodes.CursorDown(1)));
            _console.Write(new ControlCode(AnsiCodes.CarriageReturn));

            _renderedLineCount = RenderMenu(menu, isUpdate: true);

            // Move cursor back up to input line
            RestoreCursorPosition();
        }

        /// <summary>
        /// Clears the menu and restores cursor position.
        /// </summary>
        public void Clear()
        {
            Clear(-1); // Use saved column
        }

        /// <summary>
        /// Clears the menu and restores cursor to the specified column.
        /// </summary>
        /// <param name="cursorColumn">The 1-indexed cursor column to restore to, or -1 to use the saved column.</param>
        public void Clear(int cursorColumn)
        {
            if (!_isVisible)
            {
                return;
            }
            
            // Update saved column if specified
            if (cursorColumn > 0)
            {
                _savedCursorColumn = cursorColumn;
            }

            _isVisible = false;

            // Clear all lines occupied by the menu
            if (_renderedLineCount > 0)
            {
                // Move down to first menu line
                _console.Write(new ControlCode(AnsiCodes.CursorDown(1)));

                // Clear each menu line
                for (int i = 0; i < _renderedLineCount; i++)
                {
                    _console.Write(new ControlCode(AnsiCodes.CarriageReturn + AnsiCodes.EraseLine(2)));
                    if (i < _renderedLineCount - 1)
                    {
                        _console.Write(new ControlCode(AnsiCodes.CursorDown(1)));
                    }
                }

                // Move back up to input line
                _console.Write(new ControlCode(AnsiCodes.CursorUp(_renderedLineCount)));
            }

            // Move to saved column using CHA (Cursor Horizontal Absolute)
            _console.Write(new ControlCode(AnsiCodes.CursorToColumn(_savedCursorColumn)));

            _currentShape = null;
            _renderedLineCount = 0;
        }

        /// <summary>
        /// Restores cursor to the saved position on the input line.
        /// </summary>
        private void RestoreCursorPosition()
        {
            // After rendering, cursor is at the last menu line
            // Move up to input line (which is _renderedLineCount lines above)
            if (_renderedLineCount > 0)
            {
                _console.Write(new ControlCode(AnsiCodes.CursorUp(_renderedLineCount)));
            }

            // Move to saved column using CHA (Cursor Horizontal Absolute)
            _console.Write(new ControlCode(AnsiCodes.CursorToColumn(_savedCursorColumn)));
        }

        /// <summary>
        /// Renders the menu content.
        /// </summary>
        /// <returns>The number of lines rendered.</returns>
        private int RenderMenu(AutoCompleteMenu menu, bool isUpdate)
        {
            var visibleOptions = menu.VisibleOptions;
            var hasScrollUp = menu.HasMoreAbove;
            var hasScrollDown = menu.HasMoreBelow;

            int lineCount = 0;
            int maxWidth = 0;

            // Move to new line for menu (below input)
            if (!isUpdate)
            {
                _console.WriteLine();
            }

            // Scroll up indicator
            if (hasScrollUp)
            {
                var indicator = $"  ▲ {menu.MoreAboveCount} more...";
                maxWidth = Math.Max(maxWidth, indicator.Length);
                _console.Write(new ControlCode(AnsiCodes.CarriageReturn + AnsiCodes.EraseLine(2)));
                _console.Markup($"[grey dim]{indicator.EscapeMarkup()}[/]");
                _console.WriteLine();
                lineCount++;
            }

            // Render visible items
            for (int i = 0; i < visibleOptions.Count; i++)
            {
                var option = visibleOptions[i];
                var isSelected = option == menu.SelectedOption;
                var prefix = isSelected ? SelectionIndicator : " ";
                var displayText = $"{prefix} {option.Value}";
                maxWidth = Math.Max(maxWidth, displayText.Length);

                _console.Write(new ControlCode(AnsiCodes.CarriageReturn + AnsiCodes.EraseLine(2)));

                if (isSelected)
                {
                    _console.Write(displayText, HighlightStyle);
                }
                else
                {
                    _console.Write(displayText);
                }

                if (i < visibleOptions.Count - 1 || hasScrollDown)
                {
                    _console.WriteLine();
                }
                lineCount++;
            }

            // Scroll down indicator
            if (hasScrollDown)
            {
                var indicator = $"  ▼ {menu.MoreBelowCount} more...";
                maxWidth = Math.Max(maxWidth, indicator.Length);
                _console.Write(new ControlCode(AnsiCodes.CarriageReturn + AnsiCodes.EraseLine(2)));
                _console.Markup($"[grey dim]{indicator.EscapeMarkup()}[/]");
                lineCount++;
            }

            // Track shape for future updates/clearing
            var newShape = new SegmentShape(maxWidth, lineCount);
            _currentShape = _currentShape.HasValue
                ? _currentShape.Value.Inflate(newShape)
                : newShape;

            return lineCount;
        }
    }
}
