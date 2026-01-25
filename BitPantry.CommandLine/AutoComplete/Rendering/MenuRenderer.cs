using Spectre.Console;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete.Rendering
{
    /// <summary>
    /// Renders the autocomplete menu below the input line.
    /// Supports in-place updates to avoid flickering.
    /// </summary>
    public class MenuRenderer
    {
        private readonly IAnsiConsole _console;
        private SegmentShape? _currentShape;
        private bool _isVisible;

        /// <summary>
        /// Gets whether the menu is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Style for the selected (highlighted) item.
        /// </summary>
        private static readonly Style HighlightStyle = Style.Parse("invert");

        /// <summary>
        /// Style for scroll indicators.
        /// </summary>
        private static readonly Style DimStyle = new Style(Color.Grey, decoration: Decoration.Dim);

        /// <summary>
        /// Creates a new MenuRenderer.
        /// </summary>
        /// <param name="console">The console to render to.</param>
        public MenuRenderer(IAnsiConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        /// <summary>
        /// Shows the menu with the specified options.
        /// </summary>
        /// <param name="options">The options to display.</param>
        /// <param name="selectedIndex">Index of the currently selected option.</param>
        /// <param name="viewportStart">First visible option index (for scrolling).</param>
        /// <param name="viewportSize">Maximum number of visible options.</param>
        public void Show(IReadOnlyList<AutoCompleteOption> options, int selectedIndex, int viewportStart, int viewportSize)
        {
            if (options == null || options.Count == 0)
            {
                Hide();
                return;
            }

            _isVisible = true;
            RenderMenu(options, selectedIndex, viewportStart, viewportSize, isUpdate: false);
        }

        /// <summary>
        /// Updates the menu in-place without flickering.
        /// </summary>
        /// <param name="options">The options to display.</param>
        /// <param name="selectedIndex">Index of the currently selected option.</param>
        /// <param name="viewportStart">First visible option index (for scrolling).</param>
        /// <param name="viewportSize">Maximum number of visible options.</param>
        public void Update(IReadOnlyList<AutoCompleteOption> options, int selectedIndex, int viewportStart, int viewportSize)
        {
            if (!_isVisible)
            {
                Show(options, selectedIndex, viewportStart, viewportSize);
                return;
            }

            // Move cursor back to start of menu (first menu line)
            // After rendering, cursor is at the last menu line, so we move up height-1 lines
            if (_currentShape.HasValue && _currentShape.Value.Height > 1)
            {
                _console.Write(new ControlCode(AnsiCodes.CursorUp(_currentShape.Value.Height - 1)));
            }
            _console.Write(new ControlCode(AnsiCodes.CarriageReturn));

            RenderMenu(options, selectedIndex, viewportStart, viewportSize, isUpdate: true);
        }

        /// <summary>
        /// Hides and clears the menu.
        /// </summary>
        public void Hide()
        {
            if (!_isVisible)
            {
                return;
            }

            _isVisible = false;

            // Clear all lines occupied by the menu
            // After rendering, cursor is at the last menu line.
            // We need to clear from the first menu line down to current position.
            if (_currentShape.HasValue && _currentShape.Value.Height > 0)
            {
                var height = _currentShape.Value.Height;
                
                // Move up to first menu line (height - 1 because we're at the last line of menu)
                if (height > 1)
                {
                    _console.Write(new ControlCode(AnsiCodes.CursorUp(height - 1)));
                }
                
                // Clear each menu line
                for (int i = 0; i < height; i++)
                {
                    _console.Write(new ControlCode(AnsiCodes.CarriageReturn + AnsiCodes.EraseLine(2)));
                    if (i < height - 1)
                    {
                        _console.Write(new ControlCode(AnsiCodes.CursorDown(1)));
                    }
                }
                
                // Move cursor back to input line (one row above first menu line)
                _console.Write(new ControlCode(AnsiCodes.CursorUp(height)));
                _console.Write(new ControlCode(AnsiCodes.CarriageReturn));
            }

            _currentShape = null;
        }

        /// <summary>
        /// Renders the menu content.
        /// </summary>
        private void RenderMenu(IReadOnlyList<AutoCompleteOption> options, int selectedIndex, int viewportStart, int viewportSize, bool isUpdate)
        {
            var viewportEnd = Math.Min(viewportStart + viewportSize, options.Count);
            var hasScrollUp = viewportStart > 0;
            var hasScrollDown = viewportEnd < options.Count;

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
                var indicator = $"  ▲ {viewportStart} more...";
                maxWidth = Math.Max(maxWidth, indicator.Length);
                _console.Write(new ControlCode(AnsiCodes.CarriageReturn + AnsiCodes.EraseLine(2)));
                _console.Markup($"[grey dim]{indicator.EscapeMarkup()}[/]");
                _console.WriteLine();
                lineCount++;
            }

            // Render visible items
            for (int i = viewportStart; i < viewportEnd; i++)
            {
                var option = options[i];
                var displayText = $"  {option.Value}";
                maxWidth = Math.Max(maxWidth, displayText.Length);

                _console.Write(new ControlCode(AnsiCodes.CarriageReturn + AnsiCodes.EraseLine(2)));

                if (i == selectedIndex)
                {
                    _console.Write(displayText, HighlightStyle);
                }
                else
                {
                    _console.Write(displayText);
                }

                if (i < viewportEnd - 1 || hasScrollDown)
                {
                    _console.WriteLine();
                }
                lineCount++;
            }

            // Scroll down indicator
            if (hasScrollDown)
            {
                var remaining = options.Count - viewportEnd;
                var indicator = $"  ▼ {remaining} more...";
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
        }
    }
}
