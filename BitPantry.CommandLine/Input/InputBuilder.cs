using BitPantry.CommandLine.AutoComplete;
using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Input
{
    public class InputBuilder : IDisposable
    {
        private IAnsiConsole _console;
        private IPrompt _prompt;
        private AutoCompleteController _acCtrl;
        private InputLog _inputLog;

        public InputBuilder(IAnsiConsole console, IPrompt prompt, AutoCompleteController acCtrl, InputLog inputLog)
        {
            _console = console;
            _prompt = prompt;
            _acCtrl = acCtrl;
            _inputLog = inputLog ?? new InputLog();
        }

        // Constructor for backwards compatibility
        public InputBuilder(IAnsiConsole console, IPrompt prompt, AutoCompleteController acCtrl)
            : this(console, prompt, acCtrl, new InputLog())
        {
        }

        public async Task<string> GetInput(CancellationToken token = default)
        {
            _prompt.Write(_console);

            try
            {

                var input = await new ConsoleInputInterceptor(_console)
                    .AddHandler(ConsoleKey.Tab, async ctx =>
                    {
                        // Clear ghost when opening menu
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
                    })
                    .AddHandler(ConsoleKey.Escape, async ctx =>
                    {
                        if (!_acCtrl.IsEngaged)
                            return await Task.FromResult(false);

                        _acCtrl.Cancel(ctx.InputLine);
                        return await Task.FromResult(true);
                    })
                    .AddHandler(ConsoleKey.Enter, async ctx =>
                    {
                        // Clear ghost on Enter
                        _acCtrl.ClearGhost();
                        
                        if (!_acCtrl.IsEngaged)
                            return await Task.FromResult(false);

                        _acCtrl.Accept(ctx.InputLine);
                        return await Task.FromResult(true);
                    })
                    .AddHandler(ConsoleKey.RightArrow, async ctx =>
                    {
                        // Right Arrow accepts ghost text (T039)
                        if (_acCtrl.HasGhostText && ctx.InputLine.BufferPosition == ctx.InputLine.Buffer.Length)
                        {
                            _acCtrl.AcceptGhost(ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        return await Task.FromResult(false);
                    })
                    .AddHandler(ConsoleKey.End, async ctx =>
                    {
                        // End key also accepts ghost text when at end of line (T039)
                        if (_acCtrl.HasGhostText && ctx.InputLine.BufferPosition == ctx.InputLine.Buffer.Length)
                        {
                            _acCtrl.AcceptGhost(ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        return await Task.FromResult(false);
                    })
                    .AddHandler(ConsoleKey.UpArrow, async ctx =>
                    {
                        // When menu is engaged, navigate menu; otherwise navigate history
                        if (_acCtrl.IsEngaged)
                        {
                            _acCtrl.PreviousOption(ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        
                        // Clear ghost when navigating history
                        _acCtrl.ClearGhost();
                        if (_inputLog.Previous())
                        {
                            ctx.InputLine.HideCursor();
                            _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);
                            ctx.InputLine.ShowCursor();
                            return await Task.FromResult(true);
                        }

                        return await Task.FromResult(false);
                    })
                    .AddHandler(ConsoleKey.DownArrow, async ctx =>
                    {
                        // When menu is engaged, navigate menu; otherwise navigate history
                        if (_acCtrl.IsEngaged)
                        {
                            _acCtrl.NextOption(ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        
                        // Clear ghost when navigating history
                        _acCtrl.ClearGhost();
                        
                        if (_inputLog.Next())
                        {
                            ctx.InputLine.HideCursor();
                            _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);
                            ctx.InputLine.ShowCursor();
                            return await Task.FromResult(true);
                        }

                        return await Task.FromResult(false);
                    })
                    .AddHandler(ConsoleKey.Backspace, async ctx =>
                    {
                        // Handle backspace and update ghost (GS-006)
                        if (_acCtrl.IsEngaged)
                            _acCtrl.End(ctx.InputLine);
                        
                        // IMPORTANT: Clear ghost BEFORE backspace changes cursor position
                        // Ghost is rendered at current cursor position, so we must clear from here
                        _acCtrl.ClearGhost();
                        
                        ctx.InputLine.Backspace();
                        
                        // Update ghost text after backspace with the NEW buffer state
                        await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
                        
                        return true;
                    })
                    .AddHandler(ConsoleKey.Delete, async ctx =>
                    {
                        // Handle delete and update ghost
                        if (_acCtrl.IsEngaged)
                            _acCtrl.End(ctx.InputLine);
                        
                        // Clear ghost BEFORE delete (for consistency, though delete doesn't move cursor left)
                        _acCtrl.ClearGhost();
                        
                        ctx.InputLine.Delete();
                        
                        // Update ghost text after delete with the NEW buffer state
                        await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
                        
                        return true;
                    })
                    .AddDefaultHandler(async ctx =>
                    {
                        if (_acCtrl.IsEngaged)
                            _acCtrl.End(ctx.InputLine);
                        
                        // For regular character input, write the character first, then update ghost
                        // This ensures ghost calculation uses the updated buffer
                        if (!char.IsControl(ctx.KeyInfo.KeyChar))
                        {
                            ctx.InputLine.Write(ctx.KeyInfo.KeyChar);
                            
                            // Update ghost text after keystroke with the NEW buffer state
                            await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
                            
                            return true; // Mark as handled so default switch doesn't re-write the character
                        }
                        
                        // For non-character keys (backspace, delete, etc.), update ghost after default handling
                        // Return false to let default handling occur, ghost will be slightly stale but acceptable
                        return false;
                    })
                    .ReadLine(token);

                _inputLog.Add(input);

                return input;
            }
            catch
            {
                _console.WriteLine();
                throw;
            }
        }

        public void Dispose()
        {
            _acCtrl.Dispose();
        }
    }
}
