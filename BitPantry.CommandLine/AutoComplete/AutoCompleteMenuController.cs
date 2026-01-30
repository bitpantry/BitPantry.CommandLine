using System;
using System.Collections.Generic;
using BitPantry.CommandLine.AutoComplete.Rendering;
using BitPantry.CommandLine.Input;
using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// Manages the autocomplete dropdown menu, consolidating state management,
    /// keyboard navigation, and visual rendering into a single cohesive component.
    /// </summary>
    public class AutoCompleteMenuController
    {
        private readonly IAnsiConsole _console;
        private readonly AutoCompleteMenuRenderer _menuRenderer;
        private AutoCompleteMenu _menu;
        private int _promptLength;

        /// <summary>
        /// Gets whether the menu is currently visible.
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Gets the currently selected option, or null if menu is not visible.
        /// </summary>
        public AutoCompleteOption SelectedOption => IsVisible ? _menu?.SelectedOption : null;

        /// <summary>
        /// Gets the current menu state, or null if not visible.
        /// </summary>
        public AutoCompleteMenu Menu => IsVisible ? _menu : null;

        /// <summary>
        /// Gets the number of options in the current menu.
        /// </summary>
        public int OptionCount => IsVisible ? (_menu?.FilteredOptions.Count ?? 0) : 0;

        /// <summary>
        /// Creates a new AutoCompleteMenuController.
        /// </summary>
        /// <param name="console">The console to render to.</param>
        public AutoCompleteMenuController(IAnsiConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _menuRenderer = new AutoCompleteMenuRenderer(console);
        }

        /// <summary>
        /// Sets the prompt length for cursor position calculations.
        /// </summary>
        /// <param name="promptLength">The length of the prompt in characters.</param>
        public void SetPromptLength(int promptLength)
        {
            _promptLength = promptLength;
        }

        /// <summary>
        /// Shows the menu with the specified options.
        /// </summary>
        /// <param name="options">The options to display (must have at least 2).</param>
        /// <param name="line">The current input line for positioning.</param>
        public void Show(List<AutoCompleteOption> options, ConsoleLineMirror line)
        {
            if (options == null || options.Count < 2)
            {
                return;
            }

            // Create menu state
            _menu = new AutoCompleteMenu(options);
            IsVisible = true;

            // Render the menu
            _menuRenderer.Render(_menu, line, _promptLength);
        }

        /// <summary>
        /// Hides the menu using the saved cursor position.
        /// </summary>
        public void Hide()
        {
            Hide(-1);
        }

        /// <summary>
        /// Hides the menu and restores cursor to the specified column.
        /// </summary>
        /// <param name="cursorColumn">The 1-indexed cursor column, or -1 to use saved position.</param>
        public void Hide(int cursorColumn)
        {
            if (!IsVisible)
            {
                return;
            }

            _menuRenderer.Clear(cursorColumn);
            _menu = null;
            IsVisible = false;
        }

        /// <summary>
        /// Handles a key press for menu navigation.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        /// <returns>Result indicating what action occurred.</returns>
        public AutoCompleteMenuResult HandleKey(ConsoleKey key)
        {
            if (!IsVisible || _menu == null)
            {
                return AutoCompleteMenuResult.NotHandled;
            }

            switch (key)
            {
                case ConsoleKey.DownArrow:
                    _menu.MoveDown();
                    _menuRenderer.Update(_menu);
                    return AutoCompleteMenuResult.Handled;

                case ConsoleKey.UpArrow:
                    _menu.MoveUp();
                    _menuRenderer.Update(_menu);
                    return AutoCompleteMenuResult.Handled;

                case ConsoleKey.Tab:
                case ConsoleKey.Enter:
                    return AutoCompleteMenuResult.Selected;

                case ConsoleKey.Escape:
                    Hide();
                    return AutoCompleteMenuResult.Dismissed;

                default:
                    return AutoCompleteMenuResult.NotHandled;
            }
        }

        /// <summary>
        /// Updates the menu with new filtered options while keeping it visible.
        /// </summary>
        /// <param name="newOptions">The new filtered options.</param>
        /// <param name="line">The current input line for positioning.</param>
        /// <returns>True if menu remains visible, false if it was closed.</returns>
        public bool UpdateFilter(List<AutoCompleteOption> newOptions, ConsoleLineMirror line)
        {
            if (!IsVisible)
            {
                return false;
            }

            var cursorColumn = _promptLength + line.BufferPosition + 1;

            // If no options or single option, close menu
            if (newOptions == null || newOptions.Count < 2)
            {
                Hide(cursorColumn);
                return false;
            }

            // Update menu with new options
            _menu = new AutoCompleteMenu(newOptions);

            // Re-render the menu
            _menuRenderer.Clear(cursorColumn);
            _menuRenderer.Render(_menu, line, _promptLength);

            return true;
        }

        /// <summary>
        /// Forces a display update (e.g., after selection change).
        /// </summary>
        public void UpdateDisplay()
        {
            if (!IsVisible || _menu == null)
            {
                return;
            }

            _menuRenderer.Update(_menu);
        }

        /// <summary>
        /// Resets the menu controller state.
        /// </summary>
        public void Reset()
        {
            if (IsVisible)
            {
                Hide();
            }
        }

        /// <summary>
        /// Calculates the cursor column for the current line position.
        /// </summary>
        public int GetCursorColumn(ConsoleLineMirror line)
        {
            return _promptLength + line.BufferPosition + 1;
        }
    }
}
