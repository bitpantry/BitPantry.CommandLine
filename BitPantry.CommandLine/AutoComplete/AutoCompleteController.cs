using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Context;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.AutoComplete.Rendering;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete
{
    /// <summary>
    /// The current mode of the autocomplete controller.
    /// </summary>
    public enum AutoCompleteMode
    {
        /// <summary>No autocomplete is active.</summary>
        Idle,
        /// <summary>Inline ghost text suggestion is shown.</summary>
        GhostText,
        /// <summary>Dropdown menu with multiple options is shown.</summary>
        Menu
    }

    /// <summary>
    /// Orchestrates autocomplete functionality by composing sub-controllers for
    /// ghost text and menu display. Acts as the facade for the autocomplete system.
    /// </summary>
    public class AutoCompleteController
    {
        private readonly CursorContextResolver _contextResolver;
        private readonly AutoCompleteSuggestionProvider _suggestionProvider;
        private readonly GhostTextController _ghostTextController;
        private readonly AutoCompleteMenuController _menuController;
        private readonly IAnsiConsole _console;

        private CursorContext _lastContext;
        private List<AutoCompleteOption> _lastOptions;
        private int? _suppressedElementStart;

        #region State Properties

        /// <summary>
        /// Gets the current autocomplete mode.
        /// </summary>
        public AutoCompleteMode Mode
        {
            get
            {
                if (_menuController.IsVisible)
                    return AutoCompleteMode.Menu;
                if (_ghostTextController.IsShowing)
                    return AutoCompleteMode.GhostText;
                return AutoCompleteMode.Idle;
            }
        }

        /// <summary>
        /// Gets the ghost text controller for testing purposes.
        /// </summary>
        internal GhostTextController GhostTextController => _ghostTextController;

        /// <summary>
        /// Gets the menu controller for testing purposes.
        /// </summary>
        internal AutoCompleteMenuController MenuController => _menuController;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new AutoCompleteController.
        /// </summary>
        /// <param name="registry">The command registry.</param>
        /// <param name="console">The console for rendering.</param>
        /// <param name="handlerRegistry">The autocomplete handler registry.</param>
        /// <param name="handlerActivator">The handler activator.</param>
        /// <param name="serverProxy">The server proxy for remote command autocomplete (NoopServerProxy if not connected).</param>
        public AutoCompleteController(
            ICommandRegistry registry,
            IAnsiConsole console,
            IAutoCompleteHandlerRegistry handlerRegistry,
            AutoCompleteHandlerActivator handlerActivator,
            Client.IServerProxy serverProxy,
            ILogger<AutoCompleteSuggestionProvider> logger)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (console == null) throw new ArgumentNullException(nameof(console));
            if (handlerRegistry == null) throw new ArgumentNullException(nameof(handlerRegistry));
            if (handlerActivator == null) throw new ArgumentNullException(nameof(handlerActivator));
            if (serverProxy == null) throw new ArgumentNullException(nameof(serverProxy));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _console = console;
            _contextResolver = new CursorContextResolver(registry);
            _suggestionProvider = new AutoCompleteSuggestionProvider(registry, handlerRegistry, handlerActivator, serverProxy, logger);
            _ghostTextController = new GhostTextController(console);
            _menuController = new AutoCompleteMenuController(console);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Resets the autocomplete controller state for a new input session.
        /// </summary>
        /// <param name="promptLength">The length of the prompt in characters, used for cursor positioning.</param>
        public void Reset(int promptLength)
        {
            _menuController.SetPromptLength(promptLength);
            _suppressedElementStart = null;
            _lastContext = null;
            _lastOptions = null;
            _ghostTextController.Clear();
            _menuController.Reset();
        }

        #endregion

        #region Core Operations

        /// <summary>
        /// Updates the autocomplete display based on the current line content.
        /// Call this after each keystroke when not in menu mode.
        /// </summary>
        /// <remarks>
        /// This synchronous method is provided for unit testing convenience.
        /// Production code should use UpdateAsync to avoid blocking.
        /// This method calls UpdateAsync internally - there is only one implementation.
        /// </remarks>
        public void Update(ConsoleLineMirror line)
        {
            // Delegate to async implementation to avoid duplicate code.
            // Safe for unit tests with NoopServerProxy; production code uses UpdateAsync.
            UpdateAsync(line, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the autocomplete display based on the current line content asynchronously.
        /// This async version should be used when remote autocomplete is active to avoid blocking.
        /// Call this after each keystroke when not in menu mode.
        /// </summary>
        /// <param name="line">The current input line mirror.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public async Task UpdateAsync(ConsoleLineMirror line, CancellationToken cancellationToken = default)
        {
            var input = line.Buffer;

            // Don't show ghost text on empty input
            if (string.IsNullOrEmpty(input))
            {
                _lastContext = null;
                _lastOptions = null;
                _ghostTextController.Clear();
                return;
            }

            _lastContext = _contextResolver.ResolveContext(input, line.BufferPosition);

            // Check if we've moved to a new element (clears suppression)
            if (_suppressedElementStart.HasValue && 
                _lastContext != null && 
                _lastContext.ReplacementStart != _suppressedElementStart.Value)
            {
                _suppressedElementStart = null;
            }

            // If suppressed in current element, don't show ghost text
            if (_suppressedElementStart.HasValue)
            {
                _lastOptions = null;
                _ghostTextController.Clear();
                return;
            }

            _lastOptions = await _suggestionProvider.GetOptionsAsync(_lastContext, input, cancellationToken).ConfigureAwait(false);

            // Show ghost text for first option
            var ghostText = _suggestionProvider.GetGhostText(_lastOptions, _lastContext);
            if (ghostText != null)
            {
                _ghostTextController.Show(ghostText, line);
            }
            else
            {
                _ghostTextController.Clear();
            }
        }

        /// <summary>
        /// Handles a key press, performing the appropriate autocomplete action.
        /// This is the main entry point for key handling from InputBuilder.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        /// <param name="line">The current input line.</param>
        /// <returns>True if the key was handled by autocomplete; false to let default handling proceed.</returns>
        public bool HandleKey(ConsoleKey key, ConsoleLineMirror line)
        {
            // Handle menu mode keys
            if (Mode == AutoCompleteMode.Menu)
            {
                return HandleMenuModeKey(key, line);
            }

            // Handle ghost text mode keys
            if (Mode == AutoCompleteMode.GhostText)
            {
                return HandleGhostTextModeKey(key, line);
            }

            // Handle idle mode keys
            return HandleIdleModeKey(key, line);
        }

        #endregion

        #region Mode-Specific Key Handlers

        private bool HandleMenuModeKey(ConsoleKey key, ConsoleLineMirror line)
        {
            switch (key)
            {
                case ConsoleKey.Tab:
                case ConsoleKey.Enter:
                    AcceptMenuSelection(line);
                    return true;

                case ConsoleKey.Spacebar:
                    // UX-026b: In quoted context, space filters instead of accepting
                    if (_suggestionProvider.IsInQuoteContext(_lastContext))
                    {
                        // Return false to let the character be added normally, 
                        // then UpdateMenuFilter will re-filter the menu
                        return false;
                    }
                    AcceptMenuSelection(line);
                    return true;

                case ConsoleKey.Escape:
                    _menuController.Hide(_menuController.GetCursorColumn(line));
                    return true;

                case ConsoleKey.UpArrow:
                    _menuController.HandleKey(ConsoleKey.UpArrow);
                    return true;

                case ConsoleKey.DownArrow:
                    _menuController.HandleKey(ConsoleKey.DownArrow);
                    return true;

                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                    _menuController.Hide(_menuController.GetCursorColumn(line));
                    return false; // Let default handler move cursor

                case ConsoleKey.Backspace:
                    // Backspace is handled specially - need to update after default handling
                    return false;

                default:
                    // For printable characters, handled via UpdateMenuFilter after char is added
                    return false;
            }
        }

        private bool HandleGhostTextModeKey(ConsoleKey key, ConsoleLineMirror line)
        {
            switch (key)
            {
                case ConsoleKey.Tab:
                    if (_lastOptions?.Count == 1)
                    {
                        AcceptGhostText(line);
                        return true;
                    }
                    else if (_lastOptions?.Count > 1)
                    {
                        ShowMenu(line);
                        return true;
                    }
                    return false;

                case ConsoleKey.RightArrow:
                    AcceptGhostText(line);
                    return true;

                case ConsoleKey.Escape:
                    SuppressGhostText();
                    return true;

                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                    DismissGhostText();
                    return false; // Let history navigation handle

                case ConsoleKey.Backspace:
                    DismissGhostText();
                    return false; // Let default handler perform the backspace

                case ConsoleKey.Enter:
                    DismissGhostText();
                    return false; // Let default handler submit the line

                default:
                    return false;
            }
        }

        private bool HandleIdleModeKey(ConsoleKey key, ConsoleLineMirror line)
        {
            switch (key)
            {
                case ConsoleKey.Tab:
                    // Tab in idle mode - check if there are options
                    if (_lastOptions?.Count == 1)
                    {
                        AcceptGhostText(line);
                        return true;
                    }
                    else if (_lastOptions?.Count > 1)
                    {
                        ShowMenu(line);
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        #endregion

        #region Ghost Text Operations

        /// <summary>
        /// Accepts the current ghost text suggestion, applying quoting if needed.
        /// </summary>
        private void AcceptGhostText(ConsoleLineMirror line)
        {
            if (Mode != AutoCompleteMode.GhostText || _lastOptions == null || _lastOptions.Count == 0)
            {
                return;
            }

            var shouldAddSpace = _suggestionProvider.ShouldAddTrailingSpace(_lastContext);
            var isValueContext = _lastContext?.ContextType == CursorContextType.ArgumentValue
                               || _lastContext?.ContextType == CursorContextType.PositionalValue;

            if (isValueContext && _lastOptions.Count > 0)
            {
                var optionValue = _lastOptions[0].Value;
                var valueHasSpaces = optionValue.Contains(' ');
                var isInQuoteContext = _suggestionProvider.IsInQuoteContext(_lastContext);

                if (valueHasSpaces && !isInQuoteContext)
                {
                    AcceptGhostTextWithQuoting(line, optionValue, shouldAddSpace);
                    return;
                }
            }

            _ghostTextController.Accept(line);

            if (shouldAddSpace)
            {
                line.Write(" ");
            }
        }

        private void AcceptGhostTextWithQuoting(ConsoleLineMirror line, string value, bool addTrailingSpace)
        {
            var query = _lastContext?.QueryText ?? "";
            _ghostTextController.Clear();

            for (int i = 0; i < query.Length; i++)
            {
                line.Backspace();
            }

            line.Write($"\"{value}\"");

            if (addTrailingSpace)
            {
                line.Write(" ");
            }
        }

        /// <summary>
        /// Dismisses ghost text (it may reappear on next Update).
        /// </summary>
        private void DismissGhostText()
        {
            _ghostTextController.Clear();
            _lastOptions = null;
        }

        /// <summary>
        /// Suppresses ghost text for the current element (won't reappear until element changes).
        /// </summary>
        private void SuppressGhostText()
        {
            if (_lastContext != null)
            {
                _suppressedElementStart = _lastContext.ReplacementStart;
            }
            DismissGhostText();
        }

        #endregion

        #region Menu Operations

        /// <summary>
        /// Shows the autocomplete menu if multiple options are available.
        /// </summary>
        internal void ShowMenu(ConsoleLineMirror line)
        {
            if (_lastOptions == null || _lastOptions.Count < 2)
            {
                return;
            }

            _ghostTextController.Clear();
            _menuController.Show(_lastOptions, line);
        }

        /// <summary>
        /// Hides the autocomplete menu.
        /// </summary>
        internal void HideMenu()
        {
            _menuController.Hide();
        }

        /// <summary>
        /// Hides the menu with specific cursor column.
        /// </summary>
        internal void HideMenu(int cursorColumn)
        {
            _menuController.Hide(cursorColumn);
        }

        /// <summary>
        /// Accepts the currently selected menu option.
        /// </summary>
        internal void AcceptMenuSelection(ConsoleLineMirror line)
        {
            if (!_menuController.IsVisible)
            {
                return;
            }

            var selectedOption = _menuController.SelectedOption;
            if (selectedOption == null)
            {
                _menuController.Hide();
                return;
            }

            _menuController.Hide();
            ApplySelection(line, selectedOption);
        }

        /// <summary>
        /// Updates the menu filter after user types a character.
        /// Call this after the character has been added to the line.
        /// </summary>
        /// <remarks>
        /// This synchronous method is provided for unit testing convenience.
        /// Production code should use UpdateMenuFilterAsync to avoid blocking.
        /// This method calls UpdateMenuFilterAsync internally - there is only one implementation.
        /// </remarks>
        public void UpdateMenuFilter(ConsoleLineMirror line)
        {
            // Delegate to async implementation to avoid duplicate code.
            // Safe for unit tests with NoopServerProxy; production code uses UpdateMenuFilterAsync.
            UpdateMenuFilterAsync(line, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the menu filter after user types a character asynchronously.
        /// Call this after the character has been added to the line.
        /// </summary>
        /// <param name="line">The current input line mirror.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public async Task UpdateMenuFilterAsync(ConsoleLineMirror line, CancellationToken cancellationToken = default)
        {
            if (!_menuController.IsVisible)
            {
                return;
            }

            var input = line.Buffer;
            var cursorColumn = _menuController.GetCursorColumn(line);

            if (string.IsNullOrEmpty(input))
            {
                _menuController.Hide(cursorColumn);
                return;
            }

            _lastContext = _contextResolver.ResolveContext(input, line.BufferPosition);
            _lastOptions = await _suggestionProvider.GetOptionsAsync(_lastContext, input, cancellationToken).ConfigureAwait(false);

            if (_lastOptions == null || _lastOptions.Count == 0)
            {
                _menuController.Hide(cursorColumn);
                return;
            }

            if (_lastOptions.Count == 1)
            {
                _menuController.Hide(cursorColumn);
                await UpdateAsync(line, cancellationToken).ConfigureAwait(false); // Switch to ghost text mode
                return;
            }

            _menuController.UpdateFilter(_lastOptions, line);
        }

        /// <summary>
        /// Handles a menu navigation key.
        /// </summary>
        internal AutoCompleteMenuResult HandleMenuKey(ConsoleKey key)
        {
            return _menuController.HandleKey(key);
        }

        /// <summary>
        /// Updates the menu display.
        /// </summary>
        internal void UpdateMenuDisplay()
        {
            _menuController.UpdateDisplay();
        }

        private void ApplySelection(ConsoleLineMirror line, AutoCompleteOption option)
        {
            if (_lastContext == null)
            {
                return;
            }

            var query = _lastContext.QueryText ?? "";
            var prefix = _suggestionProvider.GetContextPrefix(_lastContext);
            var fullQuery = prefix + query;
            var optionValue = option.Value;

            // Erase what user typed
            for (int i = 0; i < fullQuery.Length; i++)
            {
                line.Backspace();
            }

            // Check if quoting is needed
            var isValueContext = _lastContext.ContextType == CursorContextType.ArgumentValue
                               || _lastContext.ContextType == CursorContextType.PositionalValue;

            if (isValueContext && optionValue.Contains(' ') && !_suggestionProvider.IsInQuoteContext(_lastContext))
            {
                line.Write($"\"{optionValue}\"");
            }
            else
            {
                line.Write(optionValue);
            }

            if (_suggestionProvider.ShouldAddTrailingSpace(_lastContext))
            {
                line.Write(" ");
            }
        }

        #endregion
    }
}
