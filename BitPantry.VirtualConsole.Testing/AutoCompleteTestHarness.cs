using BitPantry.CommandLine;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Input;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// Test harness for autocomplete testing using VirtualConsole.
/// Provides a complete testing environment with keyboard simulation,
/// autocomplete controller, and VirtualConsole for assertions.
/// </summary>
public class AutoCompleteTestHarness : IDisposable
{
    private readonly VirtualConsole _virtualConsole;
    private readonly VirtualConsoleAnsiAdapter _adapter;
    private readonly ConsoleLineMirror _inputLine;
    private readonly AutoCompleteController _autoComplete;
    private readonly KeyboardSimulator _keyboard;
    private readonly CommandLineApplication _app;
    private readonly ICompletionOrchestrator _orchestrator;
    private readonly TestPrompt _prompt;
    private bool _disposed;

    /// <summary>
    /// Default console width for testing.
    /// </summary>
    public const int DefaultWidth = 80;

    /// <summary>
    /// Default console height for testing.
    /// </summary>
    public const int DefaultHeight = 24;

    /// <summary>
    /// Default prompt text.
    /// </summary>
    public const string DefaultPromptText = "> ";

    /// <summary>
    /// Gets the underlying VirtualConsole for direct assertions.
    /// </summary>
    public VirtualConsole Console => _virtualConsole;

    /// <summary>
    /// Gets the IAnsiConsole adapter for Spectre.Console compatibility.
    /// </summary>
    public IAnsiConsole AnsiConsole => _adapter;

    /// <summary>
    /// Gets the current input buffer content.
    /// </summary>
    public string Buffer => _inputLine.Buffer;

    /// <summary>
    /// Gets the current buffer position (cursor position within buffer).
    /// </summary>
    public int BufferPosition => _inputLine.BufferPosition;

    /// <summary>
    /// Gets whether the autocomplete menu is currently visible.
    /// </summary>
    public bool IsMenuVisible => _autoComplete.IsEngaged;

    /// <summary>
    /// Gets the currently selected menu item text, or null if no menu is open.
    /// </summary>
    public string? SelectedItem => _autoComplete.SelectedItemText;

    /// <summary>
    /// Gets the current selected menu index.
    /// </summary>
    public int SelectedIndex => _autoComplete.MenuSelectedIndex;

    /// <summary>
    /// Gets the total number of menu items.
    /// </summary>
    public int MenuItemCount => _autoComplete.MenuItemCount;

    /// <summary>
    /// Gets the menu items (for testing).
    /// </summary>
    public IReadOnlyList<CompletionItem>? MenuItems => _autoComplete.MenuItems;

    /// <summary>
    /// Gets the current ghost text, if any.
    /// </summary>
    public string? GhostText => _autoComplete.CurrentGhostText;

    /// <summary>
    /// Gets whether ghost text is currently displayed.
    /// </summary>
    public bool HasGhostText => _autoComplete.HasGhostText;

    /// <summary>
    /// Gets the prompt length for position calculations.
    /// </summary>
    public int PromptLength => _prompt.GetPromptLength();

    /// <summary>
    /// Gets the keyboard simulator for advanced input scenarios.
    /// </summary>
    public IKeyboardSimulator Keyboard => _keyboard;

    /// <summary>
    /// Gets the underlying application for advanced scenarios.
    /// </summary>
    public CommandLineApplication Application => _app;

    /// <summary>
    /// Creates a new test harness with the specified configuration.
    /// </summary>
    /// <param name="width">Console width in columns.</param>
    /// <param name="height">Console height in rows.</param>
    /// <param name="promptText">The prompt text to display.</param>
    /// <param name="configureApp">Optional action to configure the CommandLineApplicationBuilder.</param>
    public AutoCompleteTestHarness(
        int width = DefaultWidth,
        int height = DefaultHeight,
        string promptText = DefaultPromptText,
        Action<CommandLineApplicationBuilder>? configureApp = null)
    {
        // Create VirtualConsole and adapter
        _virtualConsole = new VirtualConsole(width, height);
        _adapter = new VirtualConsoleAnsiAdapter(_virtualConsole);
        _prompt = new TestPrompt(promptText);

        // Build the CommandLineApplication
        var builder = new CommandLineApplicationBuilder()
            .UsingConsole(_adapter);

        // Apply user configuration
        configureApp?.Invoke(builder);

        _app = builder.Build();

        // Get orchestrator from services
        _orchestrator = _app.Services.GetRequiredService<ICompletionOrchestrator>();

        // Create input line and autocomplete controller
        _inputLine = new ConsoleLineMirror(_adapter);
        _autoComplete = new AutoCompleteController(_orchestrator, _adapter, _prompt);

        // Create keyboard simulator
        _keyboard = new KeyboardSimulator(_inputLine, _autoComplete);

        // Write initial prompt
        _adapter.Write(new Text(promptText));
    }

    /// <summary>
    /// Creates a new test harness using an existing CommandLineApplication.
    /// This is useful for integration testing with pre-configured applications,
    /// such as those connected to remote servers.
    /// </summary>
    /// <param name="app">The existing CommandLineApplication to use.</param>
    /// <param name="width">Console width in columns.</param>
    /// <param name="height">Console height in rows.</param>
    /// <param name="promptText">The prompt text to display.</param>
    public AutoCompleteTestHarness(
        CommandLineApplication app,
        int width = DefaultWidth,
        int height = DefaultHeight,
        string promptText = DefaultPromptText)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));

        // Create VirtualConsole and adapter
        _virtualConsole = new VirtualConsole(width, height);
        _adapter = new VirtualConsoleAnsiAdapter(_virtualConsole);
        _prompt = new TestPrompt(promptText);

        // Get orchestrator from services
        _orchestrator = _app.Services.GetRequiredService<ICompletionOrchestrator>();

        // Create input line and autocomplete controller
        _inputLine = new ConsoleLineMirror(_adapter);
        _autoComplete = new AutoCompleteController(_orchestrator, _adapter, _prompt);

        // Create keyboard simulator
        _keyboard = new KeyboardSimulator(_inputLine, _autoComplete);

        // Write initial prompt
        _adapter.Write(new Text(promptText));
    }

    /// <summary>
    /// Registers a command type with the harness.
    /// </summary>
    /// <typeparam name="TCommand">The command type to register.</typeparam>
    /// <returns>A new harness with the command registered.</returns>
    public static AutoCompleteTestHarness WithCommand<TCommand>(
        int width = DefaultWidth,
        int height = DefaultHeight,
        string promptText = DefaultPromptText) where TCommand : CommandBase
    {
        return new AutoCompleteTestHarness(width, height, promptText, builder =>
        {
            builder.RegisterCommand<TCommand>();
        });
    }

    /// <summary>
    /// Registers multiple command types with the harness.
    /// </summary>
    /// <param name="commandTypes">The command types to register.</param>
    /// <returns>A new harness with the commands registered.</returns>
    public static AutoCompleteTestHarness WithCommands(
        params Type[] commandTypes)
    {
        return WithCommands(DefaultWidth, DefaultHeight, DefaultPromptText, commandTypes);
    }

    /// <summary>
    /// Registers multiple command types with the harness.
    /// </summary>
    public static AutoCompleteTestHarness WithCommands(
        int width,
        int height,
        string promptText,
        params Type[] commandTypes)
    {
        return new AutoCompleteTestHarness(width, height, promptText, builder =>
        {
            foreach (var type in commandTypes)
            {
                builder.RegisterCommand(type);
            }
        });
    }

    /// <summary>
    /// Types text into the input line.
    /// </summary>
    /// <param name="text">The text to type.</param>
    public async Task TypeTextAsync(string text)
    {
        await _keyboard.TypeTextAsync(text);
    }

    /// <summary>
    /// Types text into the input line synchronously.
    /// </summary>
    /// <param name="text">The text to type.</param>
    public void TypeText(string text)
    {
        _keyboard.TypeText(text);
    }

    /// <summary>
    /// Presses a specific key.
    /// </summary>
    public async Task PressKeyAsync(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
    {
        await _keyboard.PressKeyAsync(key, shift, alt, control);
    }

    /// <summary>
    /// Presses a specific key synchronously.
    /// </summary>
    public void PressKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
    {
        _keyboard.PressKey(key, shift, alt, control);
    }

    /// <summary>
    /// Presses the Tab key.
    /// </summary>
    public async Task PressTabAsync() => await _keyboard.PressTabAsync();

    /// <summary>
    /// Presses the Tab key synchronously.
    /// </summary>
    public void PressTab() => _keyboard.PressTab();

    /// <summary>
    /// Presses the Enter key.
    /// </summary>
    public async Task PressEnterAsync() => await _keyboard.PressEnterAsync();

    /// <summary>
    /// Presses the Enter key synchronously.
    /// </summary>
    public void PressEnter() => _keyboard.PressEnter();

    /// <summary>
    /// Presses the Escape key.
    /// </summary>
    public async Task PressEscapeAsync() => await _keyboard.PressEscapeAsync();

    /// <summary>
    /// Presses the Escape key synchronously.
    /// </summary>
    public void PressEscape() => _keyboard.PressEscape();

    /// <summary>
    /// Presses the Backspace key.
    /// </summary>
    public async Task PressBackspaceAsync() => await _keyboard.PressBackspaceAsync();

    /// <summary>
    /// Presses the Backspace key synchronously.
    /// </summary>
    public void PressBackspace() => _keyboard.PressBackspace();

    /// <summary>
    /// Presses the Down Arrow key.
    /// </summary>
    public async Task PressDownArrowAsync() => await _keyboard.PressDownArrowAsync();

    /// <summary>
    /// Presses the Down Arrow key synchronously.
    /// </summary>
    public void PressDownArrow() => _keyboard.PressDownArrow();

    /// <summary>
    /// Presses the Up Arrow key.
    /// </summary>
    public async Task PressUpArrowAsync() => await _keyboard.PressUpArrowAsync();

    /// <summary>
    /// Presses the Up Arrow key synchronously.
    /// </summary>
    public void PressUpArrow() => _keyboard.PressUpArrow();

    /// <summary>
    /// Presses the Right Arrow key.
    /// </summary>
    public async Task PressRightArrowAsync() => await _keyboard.PressRightArrowAsync();

    /// <summary>
    /// Presses the Right Arrow key synchronously.
    /// </summary>
    public void PressRightArrow() => _keyboard.PressRightArrow();

    /// <summary>
    /// Presses the Left Arrow key.
    /// </summary>
    public async Task PressLeftArrowAsync() => await _keyboard.PressLeftArrowAsync();

    /// <summary>
    /// Presses the Left Arrow key synchronously.
    /// </summary>
    public void PressLeftArrow() => _keyboard.PressLeftArrow();

    /// <summary>
    /// Gets the screen content for debugging.
    /// </summary>
    public string GetScreenContent() => _virtualConsole.GetScreenContent();

    /// <summary>
    /// Gets the screen text without line breaks.
    /// </summary>
    public string GetScreenText() => _virtualConsole.GetScreenText();

    /// <summary>
    /// Gets a cell at the specified position.
    /// </summary>
    public ScreenCell GetCell(int row, int column) => _virtualConsole.GetCell(row, column);

    /// <summary>
    /// Gets a row at the specified index.
    /// </summary>
    public ScreenRow GetRow(int row) => _virtualConsole.GetRow(row);

    /// <summary>
    /// Gets diagnostic information for test failure messages.
    /// </summary>
    public string GetDiagnostics()
    {
        return $@"
VirtualConsole Buffer:
{GetScreenContent()}

Harness State:
  Buffer: ""{Buffer}""
  BufferPosition: {BufferPosition}
  IsMenuVisible: {IsMenuVisible}
  SelectedItem: ""{SelectedItem ?? "(none)"}""
  SelectedIndex: {SelectedIndex}
  MenuItemCount: {MenuItemCount}
  GhostText: ""{GhostText ?? "(none)"}""
  HasGhostText: {HasGhostText}
";
    }

    /// <summary>
    /// Disposes of the harness resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _autoComplete.Dispose();
        _app.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Simple prompt implementation for testing.
    /// </summary>
    private class TestPrompt : IPrompt
    {
        private readonly string _text;

        public TestPrompt(string text)
        {
            _text = text;
        }

        public int GetPromptLength() => _text.Length;

        public string Render()
        {
            return _text;
        }

        public void Write(IAnsiConsole console)
        {
            console.Write(new Text(_text));
        }
    }
}
