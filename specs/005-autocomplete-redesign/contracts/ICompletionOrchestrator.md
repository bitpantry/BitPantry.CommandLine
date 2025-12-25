# ICompletionOrchestrator Contract

**Location**: `BitPantry.CommandLine/AutoComplete/ICompletionOrchestrator.cs`

## Interface Definition

```csharp
namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Orchestrates the completion system, coordinating between providers,
/// cache, matching, and UI rendering.
/// </summary>
/// <remarks>
/// <para>
/// The orchestrator is the main entry point for the autocomplete system.
/// It is invoked by the input controller when Tab is pressed or when
/// ghost text needs to be updated.
/// </para>
/// </remarks>
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
    /// Handles a character key press while the menu is open (filtering).
    /// </summary>
    /// <param name="character">The character typed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion action result.</returns>
    Task<CompletionAction> HandleCharacterAsync(
        char character,
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
}
```

## CompletionAction Result

```csharp
/// <summary>
/// The result of a completion action, indicating what the caller should do.
/// </summary>
public sealed class CompletionAction
{
    /// <summary>
    /// The type of action to take.
    /// </summary>
    public CompletionActionType Type { get; init; }
    
    /// <summary>
    /// Text to insert into the input buffer, if any.
    /// </summary>
    public string? InsertText { get; init; }
    
    /// <summary>
    /// Whether the menu state changed and needs re-rendering.
    /// </summary>
    public bool RequiresMenuRedraw { get; init; }
    
    /// <summary>
    /// Whether the input line needs re-rendering.
    /// </summary>
    public bool RequiresInputRedraw { get; init; }
    
    /// <summary>
    /// No action needed.
    /// </summary>
    public static CompletionAction None { get; } = new() { Type = CompletionActionType.None };
    
    /// <summary>
    /// Close the menu without inserting.
    /// </summary>
    public static CompletionAction CloseMenu { get; } = new() 
    { 
        Type = CompletionActionType.CloseMenu, 
        RequiresMenuRedraw = true 
    };
}

/// <summary>
/// The type of completion action.
/// </summary>
public enum CompletionActionType
{
    /// <summary>No action needed.</summary>
    None,
    
    /// <summary>Open/show the completion menu.</summary>
    OpenMenu,
    
    /// <summary>Close the completion menu.</summary>
    CloseMenu,
    
    /// <summary>Insert text from selected completion.</summary>
    InsertText,
    
    /// <summary>Menu selection changed.</summary>
    SelectionChanged,
    
    /// <summary>Menu items filtered.</summary>
    FilterChanged,
    
    /// <summary>Show loading indicator.</summary>
    Loading,
    
    /// <summary>Show error message.</summary>
    Error
}
```

## Implementation Requirements

### State Management
- Track menu open/closed state
- Maintain current selection index
- Handle viewport scrolling for large lists

### Provider Coordination
- Resolve providers from DI
- Query in priority order
- Merge results from multiple providers if needed

### Caching
- Check cache before calling providers
- Cache results with appropriate TTL
- Invalidate cache when appropriate

### Remote Support
- Detect remote commands
- Apply debouncing (150ms)
- Handle timeouts (3s)
- Show loading indicators

## Usage Flow

```
1. User presses Tab
   ↓
2. InputController calls orchestrator.HandleTabAsync()
   ↓
3. Orchestrator builds CompletionContext from input
   ↓
4. Orchestrator checks cache
   ↓
5. If not cached, query providers
   ↓
6. Apply matching/filtering
   ↓
7. Update MenuState
   ↓
8. Return CompletionAction.OpenMenu
   ↓
9. InputController renders menu via CompletionMenu
```
