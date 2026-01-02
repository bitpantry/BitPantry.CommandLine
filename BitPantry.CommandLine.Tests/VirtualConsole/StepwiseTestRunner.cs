using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Input;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.VirtualConsole
{
    /// <summary>
    /// Allows step-by-step execution of InputBuilder to enable visual state assertions
    /// between keystrokes. This is essential for testing that the console actually LOOKS
    /// correct at each step, not just that the final result is correct.
    /// </summary>
    public class StepwiseTestRunner : IDisposable
    {
        private readonly VirtualAnsiConsole _console;
        private readonly IPrompt _prompt;
        private readonly AutoCompleteController _acCtrl;
        private readonly InputLog _inputLog;
        private readonly ConsoleLineMirror _inputLine;
        private readonly Dictionary<ConsoleKey, Func<ReadKeyHandlerContext, Task<bool>>> _handlers;
        private Func<ReadKeyHandlerContext, Task<bool>> _defaultHandler;
        private bool _isInitialized;
        private bool _isSubmitted;

        /// <summary>
        /// The underlying virtual console for visual state inspection.
        /// </summary>
        public VirtualAnsiConsole Console => _console;

        /// <summary>
        /// The AutoComplete controller for inspecting menu state.
        /// </summary>
        public AutoCompleteController Controller => _acCtrl;

        /// <summary>
        /// The input line mirror for inspecting buffer state.
        /// </summary>
        public ConsoleLineMirror InputLine => _inputLine;

        /// <summary>
        /// Gets the current buffer content (what the user has typed).
        /// </summary>
        public string Buffer => _inputLine.Buffer;

        /// <summary>
        /// Gets the current cursor position in the buffer.
        /// </summary>
        public int BufferPosition => _inputLine.BufferPosition;

        /// <summary>
        /// Gets the current cursor column on the console (including prompt).
        /// </summary>
        public int CursorColumn => _console.GetCursorPosition().Column;

        /// <summary>
        /// Gets the current cursor line on the console.
        /// </summary>
        public int CursorLine => _console.GetCursorPosition().Line;

        /// <summary>
        /// Gets the prompt length for calculating expected cursor positions.
        /// </summary>
        public int PromptLength => _prompt.GetPromptLength();

        /// <summary>
        /// Gets whether the menu is currently engaged/visible.
        /// </summary>
        public bool IsMenuVisible => _acCtrl.IsEngaged;

        /// <summary>
        /// Gets the currently selected menu item index.
        /// </summary>
        public int MenuSelectedIndex => _acCtrl.MenuSelectedIndex;

        /// <summary>
        /// Gets the currently selected menu item text.
        /// </summary>
        public string SelectedMenuItem => _acCtrl.SelectedItemText;

        /// <summary>
        /// Gets the menu item count.
        /// </summary>
        public int MenuItemCount => _acCtrl.MenuItemCount;

        /// <summary>
        /// Gets all menu items as a list of strings (insert text).
        /// Returns empty list if menu is not visible.
        /// </summary>
        public List<string> GetMenuItems()
        {
            var items = _acCtrl.MenuItems;
            if (items == null)
                return new List<string>();
            return items.Select(i => i.InsertText).ToList();
        }

        /// <summary>
        /// Gets the current ghost text, if any.
        /// </summary>
        public string GhostText => _acCtrl.CurrentGhostText;

        /// <summary>
        /// Gets whether ghost text is currently showing.
        /// </summary>
        public bool HasGhostText => _acCtrl.HasGhostText;

        /// <summary>
        /// Gets whether input has been submitted (Enter pressed).
        /// </summary>
        public bool IsSubmitted => _isSubmitted;

        /// <summary>
        /// Gets the first line of the console buffer (the input line).
        /// </summary>
        public string DisplayedLine
        {
            get
            {
                var lines = _console.Lines;
                return lines.Count > 0 ? lines[0] : string.Empty;
            }
        }

        /// <summary>
        /// Gets just the input portion of the displayed line (after prompt).
        /// </summary>
        public string DisplayedInput
        {
            get
            {
                var line = DisplayedLine;
                var promptLen = PromptLength;
                if (line.Length <= promptLen) return string.Empty;
                return line.Substring(promptLen);
            }
        }

        public StepwiseTestRunner(
            VirtualAnsiConsole console,
            IPrompt prompt,
            AutoCompleteController acCtrl,
            InputLog inputLog = null)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
            _acCtrl = acCtrl ?? throw new ArgumentNullException(nameof(acCtrl));
            _inputLog = inputLog ?? new InputLog();
            _inputLine = new ConsoleLineMirror(console);
            _handlers = new Dictionary<ConsoleKey, Func<ReadKeyHandlerContext, Task<bool>>>();
            
            // Set up standard handlers matching InputBuilder behavior
            SetupHandlers();
        }

        /// <summary>
        /// Initialize the runner by writing the prompt.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _prompt.Write(_console);
            _isInitialized = true;
        }

        /// <summary>
        /// Add an entry to history (for testing history navigation).
        /// </summary>
        public void AddHistoryEntry(string entry)
        {
            _inputLog.Add(entry);
        }

        /// <summary>
        /// Process a single keystroke and return control for assertions.
        /// </summary>
        public async Task PressKey(ConsoleKey key, ConsoleModifiers modifiers = 0)
        {
            EnsureInitialized();
            if (_isSubmitted) throw new InvalidOperationException("Input already submitted");

            char keyChar = key switch
            {
                ConsoleKey.Tab => '\t',
                ConsoleKey.Enter => '\r',
                ConsoleKey.Escape => '\x1b',
                ConsoleKey.Backspace => '\b',
                ConsoleKey.Delete => '\x7f',
                >= ConsoleKey.A and <= ConsoleKey.Z => (char)(key - ConsoleKey.A + 'a'),
                >= ConsoleKey.D0 and <= ConsoleKey.D9 => (char)(key - ConsoleKey.D0 + '0'),
                ConsoleKey.Spacebar => ' ',
                ConsoleKey.OemMinus => '-',
                _ => '\0'
            };

            bool shift = modifiers.HasFlag(ConsoleModifiers.Shift);
            bool alt = modifiers.HasFlag(ConsoleModifiers.Alt);
            bool ctrl = modifiers.HasFlag(ConsoleModifiers.Control);

            var keyInfo = new ConsoleKeyInfo(keyChar, key, shift, alt, ctrl);
            await HandleKeyPress(keyInfo);
        }

        /// <summary>
        /// Process Shift+Tab keystroke.
        /// </summary>
        public async Task PressShiftTab()
        {
            await PressKey(ConsoleKey.Tab, ConsoleModifiers.Shift);
        }

        /// <summary>
        /// Process Ctrl+C keystroke.
        /// </summary>
        public async Task PressCtrlC()
        {
            await PressKey(ConsoleKey.C, ConsoleModifiers.Control);
        }

        /// <summary>
        /// Type a character.
        /// </summary>
        public async Task TypeChar(char ch)
        {
            EnsureInitialized();
            if (_isSubmitted) throw new InvalidOperationException("Input already submitted");

            var key = ch switch
            {
                >= 'a' and <= 'z' => (ConsoleKey)(ch - 'a' + ConsoleKey.A),
                >= 'A' and <= 'Z' => (ConsoleKey)(ch - 'A' + ConsoleKey.A),
                >= '0' and <= '9' => (ConsoleKey)(ch - '0' + ConsoleKey.D0),
                ' ' => ConsoleKey.Spacebar,
                '-' => ConsoleKey.OemMinus,
                _ => ConsoleKey.NoName
            };

            bool shift = char.IsUpper(ch);
            var keyInfo = new ConsoleKeyInfo(ch, key, shift, false, false);
            await HandleKeyPress(keyInfo);
        }

        /// <summary>
        /// Type a string of characters.
        /// </summary>
        public async Task TypeText(string text)
        {
            foreach (var ch in text)
            {
                await TypeChar(ch);
            }
        }

        /// <summary>
        /// Submit the input (press Enter and finalize).
        /// </summary>
        public async Task<string> Submit()
        {
            EnsureInitialized();
            if (_isSubmitted) throw new InvalidOperationException("Input already submitted");

            // First handle Enter key normally (in case menu is open)
            await PressKey(ConsoleKey.Enter);

            // If still not submitted, force submission
            if (!_isSubmitted)
            {
                _console.WriteLine();
                _inputLog.Add(_inputLine.Buffer);
                _isSubmitted = true;
            }

            return _inputLine.Buffer;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized) Initialize();
        }

        private async Task HandleKeyPress(ConsoleKeyInfo keyInfo)
        {
            var isHandled = false;
            var ctx = new ReadKeyHandlerContext(_inputLine, keyInfo);

            // Check registered handlers first
            if (_handlers.ContainsKey(keyInfo.Key))
            {
                isHandled = await _handlers[keyInfo.Key].Invoke(ctx);
            }

            // Check default handler
            if (!isHandled && _defaultHandler != null)
            {
                isHandled = await _defaultHandler.Invoke(ctx);
            }

            // Default handling if not handled
            if (!isHandled)
            {
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        _console.WriteLine();
                        _inputLog.Add(_inputLine.Buffer);
                        _isSubmitted = true;
                        break;
                    case ConsoleKey.LeftArrow:
                        _inputLine.MovePositionLeft();
                        break;
                    case ConsoleKey.RightArrow:
                        _inputLine.MovePositionRight();
                        break;
                    case ConsoleKey.Backspace:
                        _inputLine.Backspace();
                        break;
                    case ConsoleKey.Delete:
                        _inputLine.Delete();
                        break;
                    default:
                        if (!char.IsControl(keyInfo.KeyChar))
                            _inputLine.Write(keyInfo.KeyChar);
                        break;
                }
            }
        }

        private void SetupHandlers()
        {
            // Tab handler
            _handlers[ConsoleKey.Tab] = async ctx =>
            {
                _acCtrl.ClearGhost();
                
                if (_acCtrl.IsEngaged)
                {
                    if (ctx.KeyInfo.Modifiers == ConsoleModifiers.Shift)
                        _acCtrl.PreviousOption(ctx.InputLine);
                    else
                        _acCtrl.NextOption(ctx.InputLine);
                }
                else
                {
                    await _acCtrl.Begin(ctx.InputLine);
                }

                return true;
            };

            // Escape handler
            _handlers[ConsoleKey.Escape] = async ctx =>
            {
                if (!_acCtrl.IsEngaged)
                    return await Task.FromResult(false);

                _acCtrl.Cancel(ctx.InputLine);
                return await Task.FromResult(true);
            };

            // Enter handler
            _handlers[ConsoleKey.Enter] = async ctx =>
            {
                _acCtrl.ClearGhost();
                
                if (!_acCtrl.IsEngaged)
                    return await Task.FromResult(false);

                _acCtrl.Accept(ctx.InputLine);
                return await Task.FromResult(true);
            };

            // Right Arrow handler (ghost acceptance or cursor movement)
            _handlers[ConsoleKey.RightArrow] = async ctx =>
            {
                if (_acCtrl.HasGhostText && ctx.InputLine.BufferPosition == ctx.InputLine.Buffer.Length)
                {
                    _acCtrl.AcceptGhost(ctx.InputLine);
                    return await Task.FromResult(true);
                }
                // Clear ghost and move cursor if not at end
                _acCtrl.ClearGhost();
                ctx.InputLine.MovePositionRight();
                return await Task.FromResult(true);
            };

            // Left Arrow handler (cursor movement, clears ghost)
            _handlers[ConsoleKey.LeftArrow] = async ctx =>
            {
                _acCtrl.ClearGhost();
                ctx.InputLine.MovePositionLeft();
                return await Task.FromResult(true);
            };

            // Home key handler
            _handlers[ConsoleKey.Home] = async ctx =>
            {
                if (_acCtrl.IsEngaged)
                    _acCtrl.End(ctx.InputLine);
                _acCtrl.ClearGhost();
                ctx.InputLine.MoveToPosition(0);
                return await Task.FromResult(true);
            };

            // End key handler
            _handlers[ConsoleKey.End] = async ctx =>
            {
                // If menu is open, close it and move to end
                if (_acCtrl.IsEngaged)
                {
                    _acCtrl.End(ctx.InputLine);
                    ctx.InputLine.MoveToPosition(ctx.InputLine.Buffer.Length);
                    return await Task.FromResult(true);
                }
                
                if (_acCtrl.HasGhostText && ctx.InputLine.BufferPosition == ctx.InputLine.Buffer.Length)
                {
                    _acCtrl.AcceptGhost(ctx.InputLine);
                    return await Task.FromResult(true);
                }
                
                // Move cursor to end
                ctx.InputLine.MoveToPosition(ctx.InputLine.Buffer.Length);
                return await Task.FromResult(true);
            };

            // Up Arrow handler
            _handlers[ConsoleKey.UpArrow] = async ctx =>
            {
                if (_acCtrl.IsEngaged)
                {
                    _acCtrl.PreviousOption(ctx.InputLine);
                    return await Task.FromResult(true);
                }
                
                _acCtrl.ClearGhost();
                if (_inputLog.Previous())
                {
                    ctx.InputLine.HideCursor();
                    _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);
                    ctx.InputLine.ShowCursor();
                    return await Task.FromResult(true);
                }

                return await Task.FromResult(false);
            };

            // Down Arrow handler
            _handlers[ConsoleKey.DownArrow] = async ctx =>
            {
                if (_acCtrl.IsEngaged)
                {
                    _acCtrl.NextOption(ctx.InputLine);
                    return await Task.FromResult(true);
                }
                
                _acCtrl.ClearGhost();
                
                if (_inputLog.Next())
                {
                    ctx.InputLine.HideCursor();
                    _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);
                    ctx.InputLine.ShowCursor();
                    return await Task.FromResult(true);
                }

                return await Task.FromResult(false);
            };

            // Backspace handler
            _handlers[ConsoleKey.Backspace] = async ctx =>
            {
                if (_acCtrl.IsEngaged)
                    _acCtrl.End(ctx.InputLine);
                
                _acCtrl.ClearGhost();
                
                ctx.InputLine.Backspace();
                
                await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
                
                return true;
            };

            // Delete handler
            _handlers[ConsoleKey.Delete] = async ctx =>
            {
                if (_acCtrl.IsEngaged)
                    _acCtrl.End(ctx.InputLine);
                
                _acCtrl.ClearGhost();
                
                ctx.InputLine.Delete();
                
                await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
                
                return true;
            };

            // Default handler for character input
            _defaultHandler = async ctx =>
            {
                if (!char.IsControl(ctx.KeyInfo.KeyChar))
                {
                    ctx.InputLine.Write(ctx.KeyInfo.KeyChar);
                    
                    if (_acCtrl.IsEngaged)
                    {
                        // Filter menu while typing
                        await _acCtrl.HandleCharacterWhileMenuOpenAsync(ctx.InputLine, ctx.KeyInfo.KeyChar);
                    }
                    else
                    {
                        // Update ghost text when menu not open
                        await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
                    }
                    return true;
                }
                
                return false;
            };
        }

        public void Dispose()
        {
            _acCtrl?.Dispose();
            _console?.Dispose();
        }
    }
}
