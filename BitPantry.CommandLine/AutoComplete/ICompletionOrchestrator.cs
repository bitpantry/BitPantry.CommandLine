using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Orchestrates the completion system, coordinating between providers,
/// cache, matching, and UI rendering.
/// </summary>
public interface ICompletionOrchestrator
{
    /// <summary>
    /// Handles a Tab key press, showing or navigating the completion menu.
    /// </summary>
    /// <param name="inputBuffer">The current input buffer.</param>
    /// <param name="cursorPosition">The cursor position in the buffer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion action result.</returns>
    Task<CompletionAction> HandleTabAsync(
        string inputBuffer,
        int cursorPosition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a Shift+Tab key press, navigating up in the completion menu.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion action result.</returns>
    Task<CompletionAction> HandleShiftTabAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles an Escape key press, closing the completion menu.
    /// </summary>
    /// <returns>The completion action result.</returns>
    CompletionAction HandleEscape();

    /// <summary>
    /// Handles an Enter key press while the menu is open.
    /// </summary>
    /// <returns>The completion action result with selected item.</returns>
    CompletionAction HandleEnter();

    /// <summary>
    /// Handles an Up Arrow key press for menu navigation.
    /// </summary>
    /// <returns>The completion action result.</returns>
    CompletionAction HandleUpArrow();

    /// <summary>
    /// Handles a Down Arrow key press for menu navigation.
    /// </summary>
    /// <returns>The completion action result.</returns>
    CompletionAction HandleDownArrow();

    /// <summary>
    /// Handles a character key press while the menu is open (filtering).
    /// </summary>
    /// <param name="character">The character typed.</param>
    /// <param name="inputBuffer">The current input buffer.</param>
    /// <param name="cursorPosition">The cursor position.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion action result.</returns>
    Task<CompletionAction> HandleCharacterAsync(
        char character,
        string inputBuffer,
        int cursorPosition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the ghost text suggestion for the current input.
    /// </summary>
    /// <param name="inputBuffer">The current input buffer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ghost text to display, or null.</returns>
    Task<string?> UpdateGhostTextAsync(
        string inputBuffer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the completion menu is currently open.
    /// </summary>
    bool IsMenuOpen { get; }

    /// <summary>
    /// Gets the current menu state for rendering.
    /// </summary>
    MenuState? MenuState { get; }

    /// <summary>
    /// Invalidates the cache for a specific command after execution.
    /// </summary>
    /// <param name="commandName">The command name.</param>
    void InvalidateCacheForCommand(string commandName);
}
