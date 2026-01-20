# Test Cases: Extension-Based Autocomplete System

**Feature**: 008-autocomplete-extensions  
**Date**: January 18, 2026

---

## Test Categories

1. [AutoCompleteHandlerRegistry](#1-autocompletehandlerregistry)
2. [Built-in Type Handlers](#2-built-in-type-handlers)
3. [AutoCompleteAttribute](#3-autocompleteattribute)
4. [Integration Tests](#4-integration-tests)
5. [User Experience (Ghost Text & Menu)](#5-user-experience-ghost-text--menu)
6. [Command Syntax Autocomplete](#6-command-syntax-autocomplete)
7. [Remote Autocomplete](#7-remote-autocomplete)

---

## 1. AutoCompleteHandlerRegistry

### 008:TC-1.1: Register Adds Handler to List
**Given**: An empty registry and a Type Handler  
**When**: Calling `Register<THandler>()`  
**Then**: Handler is in the registry

### 008:TC-1.2: GetHandler Returns Null When No Handler Matches
**Given**: `string` argument without attribute  
**And**: No handler where `CanHandle(typeof(string))` returns true  
**When**: Calling `GetHandler(stringArgumentInfo)`  
**Then**: Returns null

### 008:TC-1.3: GetHandler Returns Last Registered Matching Handler
**Given**: Registry with HandlerA then HandlerB, both matching `LogLevel`  
**When**: `GetHandler(logLevelArgumentInfo)`  
**Then**: Returns HandlerB (last registered wins)

### 008:TC-1.4: Attribute Handler Takes Precedence Over Type Handler
**Given**: Argument with `[AutoComplete<CustomHandler>]`  
**And**: Registry has Type Handler that matches the argument type  
**When**: `GetHandler(argumentInfo)`  
**Then**: Returns `CustomHandler` (not Type Handler)

---

## 2. Built-in Type Handlers

### EnumAutoCompleteHandler

#### 008:TC-2.1: CanHandle Returns True For Enum Types
**Given**: `new EnumAutoCompleteHandler()`  
**When**: `CanHandle(typeof(LogLevel))`  
**Then**: Returns true

#### 008:TC-2.2: CanHandle Returns False For Non-Enum Types
**Given**: `new EnumAutoCompleteHandler()`  
**When**: `CanHandle(typeof(string))`  
**Then**: Returns false

#### 008:TC-2.3: CanHandle Returns False For Enum Base Type
**Given**: `new EnumAutoCompleteHandler()`  
**When**: `CanHandle(typeof(Enum))`  
**Then**: Returns false (Enum itself is not an enum)

#### 008:TC-2.4: Returns All Enum Values When Query Empty
**Given**: Handler and `LogLevel` argument with values [Debug, Info, Warning, Error]  
**When**: `GetOptionsAsync` with `QueryString = ""`  
**Then**: Returns all 4 values

#### 008:TC-2.5: Filters by Prefix Case-Insensitive
**Given**: Handler and `LogLevel` argument  
**When**: `GetOptionsAsync` with `QueryString = "war"`  
**Then**: Returns ["Warning"]

#### 008:TC-2.6: Returns Sorted Results
**Given**: Handler and enum with values [Zebra, Apple, Mango]  
**When**: `GetOptionsAsync` with `QueryString = ""`  
**Then**: Returns ["Apple", "Mango", "Zebra"] (alphabetical)

#### 008:TC-2.7: Handles Nullable Enum In Context
**Given**: Handler and `LogLevel?` argument  
**When**: `GetOptionsAsync`  
**Then**: Returns values from `LogLevel` (unwraps nullable)

### BooleanAutoCompleteHandler

#### 008:TC-2.8: CanHandle Returns True For Bool
**Given**: `new BooleanAutoCompleteHandler()`  
**When**: `CanHandle(typeof(bool))`  
**Then**: Returns true

#### 008:TC-2.9: CanHandle Returns False For Non-Bool
**Given**: `new BooleanAutoCompleteHandler()`  
**When**: `CanHandle(typeof(string))`  
**Then**: Returns false

#### 008:TC-2.10: Returns True and False When Query Empty
**Given**: Boolean handler  
**When**: `GetOptionsAsync` with `QueryString = ""`  
**Then**: Returns ["false", "true"]

#### 008:TC-2.11: Filters by Prefix
**Given**: Boolean handler  
**When**: `GetOptionsAsync` with `QueryString = "t"`  
**Then**: Returns ["true"]

---

## 3. AutoCompleteAttribute<THandler>

### 008:TC-3.1: Generic Constraint Prevents Invalid Types
**Given**: A type that doesn't implement `IAutoCompleteHandler`  
**When**: Attempting `[AutoComplete<string>]`  
**Then**: Compile error (generic constraint violation)

> **Note**: This is a compile-time guarantee enforced by the generic constraint `where THandler : IAutoCompleteHandler`. It cannot be tested at runtime because invalid code would not compile. The constraint is verified by code review of `AutoCompleteAttribute<THandler>` definition.

### 008:TC-3.2: HandlerType Returns Correct Type
**Given**: `[AutoComplete<MyHandler>]` on a property  
**When**: Accessing `HandlerType` via `IAutoCompleteAttribute`  
**Then**: Returns `typeof(MyHandler)`

### 008:TC-3.3: Works With Type Handler Types
**Given**: A type implementing `ITypeAutoCompleteHandler`  
**When**: Using `[AutoComplete<MyTypeHandler>]`  
**Then**: Compiles and `HandlerType` equals `typeof(MyTypeHandler)`

> **Design Note**: This behavior is intentional. C# generics cannot enforce exclusion of derivative interfaces (e.g., "allow IAutoCompleteHandler but not ITypeAutoCompleteHandler"). The practical implication is minimal: if a Type Handler is used via explicit attribute on a mismatched type, the handler's `CanHandle` simply won't be called (attribute handlers bypass that check). This flexibility also allows future interface extensions without breaking the attribute constraint.

### 008:TC-3.4: Attribute Inheritance Works
**Given**: Custom attribute inheriting `AutoCompleteAttribute<T>`  
**When**: Checking for `IAutoCompleteAttribute` on property  
**Then**: Custom attribute is found via marker interface

---

## 4. Integration Tests

### 008:TC-4.1: End-to-End Enum Autocomplete
**Given**: Command with `LogLevel` argument  
**And**: Application built with default handlers  
**When**: User types "err" and triggers autocomplete  
**Then**: Returns ["Error"]

### 008:TC-4.2: Custom Type Handler Overrides Built-In
**Given**: Custom `LogLevelHandler` registered after built-in  
**When**: User triggers autocomplete for `LogLevel` argument  
**Then**: Custom handler behavior is used

### 008:TC-4.3: Attribute Handler Used Despite Type Handler
**Given**: `LogLevel` argument with `[AutoComplete<CustomHandler>]`  
**And**: EnumAutoCompleteHandler registered  
**When**: User triggers autocomplete  
**Then**: Custom handler is invoked

### 008:TC-4.4: Handler Can Access Other Argument Values
**Given**: Handler needing context from other arguments  
**When**: `GetOptionsAsync` is invoked  
**Then**: `context.ProvidedValues` contains already-entered values

### 008:TC-4.5: Boolean Autocomplete Works
**Given**: Command with `bool Verbose` argument  
**When**: User types "f" and triggers autocomplete  
**Then**: Returns ["false"]

### 008:TC-4.6: Nullable Enum Autocomplete Works
**Given**: Command with `LogLevel?` argument  
**When**: User triggers autocomplete  
**Then**: Returns enum values (nullable unwrapped)

### 008:TC-4.7: Handler Exception Gracefully Degrades
**Given**: Argument with `[AutoComplete<ThrowingHandler>]` where handler throws exception  
**When**: User triggers autocomplete  
**Then**: No suggestions displayed (graceful degradation), exception logged to ILogger, UX not disrupted

### 008:TC-4.8: Handler Returning Empty Continues Resolution
**Given**: Multiple Type Handlers registered  
**And**: First matching handler returns empty list  
**When**: User triggers autocomplete  
**Then**: System does NOT continue to next handler (first match wins, empty is valid result)

### 008:TC-4.9: New Input Cancels Pending Request
**Given**: Slow handler that takes 500ms to respond  
**When**: User types a character, then types another character before first request completes  
**Then**: First request is cancelled via CancellationToken, only second request result is used

---

## Test Data

### Sample Enums

```csharp
public enum LogLevel { Debug, Info, Warning, Error }
public enum Environment { Development, Staging, Production }
public enum Priority { Low, Medium, High, Critical }
```

### Sample Commands

```csharp
[Command]
public class TestCommand : CommandBase
{
    [Argument]
    public LogLevel Level { get; set; }
    
    [Argument]
    public bool Verbose { get; set; }
    
    [Argument]
    public LogLevel? OptionalLevel { get; set; }
    
    [Argument]
    [AutoComplete(typeof(CustomEnvironmentHandler))]
    public string Target { get; set; }
}
```

### Sample Handlers

```csharp
// Attribute Handler - just implements core interface
public class CustomEnvironmentHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken ct = default)
    {
        var query = context.QueryString ?? "";
        return Task.FromResult(new[] { "dev", "staging", "prod" }
            .Where(e => e.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(e => new AutoCompleteOption(e))
            .ToList());
    }
}

// Type Handler - implements extended interface
public class LogLevelHandler : ITypeAutoCompleteHandler
{
    public bool CanHandle(Type t) => t == typeof(LogLevel);
    
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken ct = default)
    {
        var query = context.QueryString ?? "";
        // Custom logic with filtering
        return Task.FromResult(new[] 
        {
            new AutoCompleteOption("Debug", "Detailed debugging information"),
            new AutoCompleteOption("Info", "General information"),
            new AutoCompleteOption("Warning", "Warning messages"),
            new AutoCompleteOption("Error", "Error messages")
        }
        .Where(o => o.Value.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        .ToList());
    }
}
```

---

## 5. User Experience (Ghost Text & Menu)

*Source: [experience.md](experience.md) acceptance criteria*

### Ghost Text Behavior

#### 008:UX-001: Ghost Text Auto-Appears
**Given**: Cursor enters an autocomplete-applicable position  
**When**: Position has available suggestions  
**Then**: Ghost text appears automatically with the first alphabetical match (no keypress required)

#### 008:UX-002: Tab Accepts Single Option
**Given**: Ghost text visible with only one matching option  
**When**: User presses Tab  
**Then**: Ghost text is accepted, cursor moves to end of inserted text

#### 008:UX-003: Tab Opens Menu for Multiple Options
**Given**: Ghost text visible with multiple matching options  
**When**: User presses Tab  
**Then**: Ghost text clears, menu opens with first item selected

#### 008:UX-004: Right Arrow Accepts Ghost Text
**Given**: Ghost text visible  
**When**: User presses Right Arrow  
**Then**: Ghost text is accepted (same behavior as Tab with single option)

#### 008:UX-008: Escape Dismisses Ghost Text
**Given**: Ghost text visible  
**When**: User presses Escape  
**Then**: Ghost text clears, cursor stays at current position

#### 008:UX-010: Typing Updates Ghost Text
**Given**: Ghost text visible  
**When**: User types a character  
**Then**: Character is inserted, ghost text updates to new first match

#### 008:UX-012: Up Arrow Dismisses Ghost Text for History
**Given**: Ghost text visible  
**When**: User presses Up Arrow  
**Then**: Ghost text dismissed, command history is shown

#### 008:UX-013: No Ghost Text When No Matches
**Given**: Cursor at autocomplete position  
**When**: No suggestions match the current input  
**Then**: No ghost text appears, Tab does nothing

### Menu Behavior

#### 008:UX-005: Down Arrow Navigates Menu
**Given**: Menu is open  
**When**: User presses Down Arrow  
**Then**: Selection moves to next item (wraps from last to first)

#### 008:UX-006: Up Arrow Navigates Menu
**Given**: Menu is open  
**When**: User presses Up Arrow  
**Then**: Selection moves to previous item (wraps from first to last)

#### 008:UX-007: Enter Accepts Menu Selection
**Given**: Menu is open with an item selected  
**When**: User presses Enter  
**Then**: Selected option is inserted, menu closes

#### 008:UX-009: Escape Closes Menu
**Given**: Menu is open  
**When**: User presses Escape  
**Then**: Menu closes, original text is preserved (no insertion)

#### 008:UX-011: Type-to-Filter in Menu
**Given**: Menu is open  
**When**: User types a character  
**Then**: Character is added to input, menu filters options in real-time

#### 008:UX-025: Backspace Re-filters Menu
**Given**: Menu is open with filter text  
**When**: User presses Backspace  
**Then**: Last character removed, menu re-filters to show more options

#### 008:UX-026: Space Accepts and Continues (Unquoted Context)
**Given**: Menu is open and cursor is NOT within quotes  
**When**: User presses Space  
**Then**: Current selection accepted, space inserted, menu closes

#### 008:UX-026b: Space Filters Within Quoted Context
**Given**: Menu is open and cursor is within an open quote (`"`)
**When**: User presses Space  
**Then**: Space is added to filter text, menu re-filters to show values containing the space

#### 008:UX-027: Filter Removes All Matches
**Given**: Menu is open  
**When**: User types characters that filter out all options  
**Then**: Menu closes, no ghost text (no matches available)

#### 008:UX-027b: Backspace Restores Filtered Options
**Given**: Menu was closed due to filter removing all matches (UX-027)  
**When**: User presses Backspace to remove filter characters  
**Then**: When filter matches options again, ghost text/menu reappears automatically

### Menu Scrolling

#### 008:UX-020: Scroll Indicator at Bottom
**Given**: Menu has more than 5 options  
**When**: Menu opens (scrolled to top)  
**Then**: Menu shows `▼ N more...` indicator at bottom with count of hidden items

#### 008:UX-021: Scroll Indicators at Both Ends
**Given**: Menu is scrolled to middle  
**When**: Items exist above and below visible area  
**Then**: Menu shows both `▲ N more...` and `▼ N more...` indicators

#### 008:UX-022: Scroll Indicator at Top
**Given**: Menu is scrolled to bottom  
**When**: All bottom items visible  
**Then**: Menu shows `▲ N more...` indicator at top only

#### 008:UX-023: Wrap Navigation Bottom to Top
**Given**: Menu is scrolled to bottom, last item selected  
**When**: User presses Down Arrow  
**Then**: Selection jumps to first item, menu scrolls to top

#### 008:UX-024: Wrap Navigation Top to Bottom
**Given**: Menu is at top, first item selected  
**When**: User presses Up Arrow  
**Then**: Selection jumps to last item, menu scrolls to bottom

### Handler Integration

#### 008:UX-014: Enum Autocomplete Works
**Given**: Cursor at value position for enum-typed argument  
**When**: No explicit attribute present  
**Then**: Enum values appear as autocomplete options (implicit handler)

#### 008:UX-015: Boolean Autocomplete Works
**Given**: Cursor at value position for bool-typed argument  
**When**: No explicit attribute present  
**Then**: "true" and "false" appear as autocomplete options

#### 008:UX-016: Attribute Handler Overrides Type Handler
**Given**: Argument has `[AutoComplete<THandler>]` attribute  
**And**: A registered Type Handler also matches the argument type  
**When**: User triggers autocomplete  
**Then**: Attribute Handler is used (explicit takes precedence over implicit)

### Positional Parameter Handling

#### 008:UX-017: Positional Enum Shows Ghost Text
**Given**: Command with positional enum parameter  
**When**: Cursor at that positional position  
**Then**: Ghost text shows first enum value for that position

#### 008:UX-018: Multiple Positionals Track Independently
**Given**: Command with multiple positional parameters  
**When**: User fills first positional, moves to second  
**Then**: Each position tracks independently, correct handler used for each

#### 008:UX-019: Positional Without Handler
**Given**: Positional parameter with no implicit or explicit handler (e.g., string)  
**When**: Cursor at that position  
**Then**: No ghost text appears, Tab does nothing

#### 008:UX-031: Positional Set Positionally Excludes from Named
**Given**: Positional value provided positionally (e.g., `myapp Debug`)  
**When**: User types `--` and triggers autocomplete  
**Then**: That argument name is excluded from `--` suggestions

#### 008:UX-032: Positional Set by Name Has No Positional Autocomplete
**Given**: Positional parameter set by name (e.g., `--level Debug`)  
**When**: Cursor at that positional position  
**Then**: No autocomplete offered (position is satisfied)

#### 008:UX-033: Named Arg Set But Positional Unsatisfied
**Given**: Named argument set but positional parameter NOT satisfied by it  
**When**: Cursor at unsatisfied positional position  
**Then**: Positional autocomplete still works

#### 008:UX-034: After Named Arg Only Named Args Available
**Given**: Cursor is after first named argument (`--name`)  
**When**: User is at a space position  
**Then**: Only named arguments available, no positional autocomplete

#### 008:UX-035: Unsatisfied Positional in Named Suggestions
**Given**: After first `--arg`, a positional is still unsatisfied  
**When**: User types `--` and triggers autocomplete  
**Then**: Unsatisfied positional appears in `--` suggestions as named arg option

### Value Formatting

#### 008:UX-028: Values with Spaces Auto-Quoted
**Given**: Handler returns value containing spaces (e.g., "My Documents")  
**When**: Value is inserted via autocomplete  
**Then**: Value is wrapped in double quotes: `"My Documents"`

#### 008:UX-029: Values Without Spaces Not Quoted
**Given**: Handler returns value without spaces  
**When**: Value is inserted via autocomplete  
**Then**: Value is inserted without quotes

#### 008:UX-030: Completion Within Existing Quotes
**Given**: User has already typed opening quote (`"`)  
**When**: Autocomplete inserts a value  
**Then**: Completion continues within quote context, adds closing quote

---

## 6. Command Syntax Autocomplete

*Source: spec.md FR-018 through FR-023, User Story 6*

### Group and Command Suggestions

#### 008:SYN-001: Groups Suggested at Command Position
**Given**: Registered command groups exist  
**When**: Cursor at command position with partial group name typed  
**Then**: Matching group names are suggested

#### 008:SYN-002: Commands Suggested Within Group
**Given**: User has typed a group name  
**When**: Cursor at command position within that group  
**Then**: Commands within that group are suggested

#### 008:SYN-003: Root Commands Suggested
**Given**: Commands registered at root level (no group)  
**When**: Cursor at command position  
**Then**: Root-level commands are suggested

#### 008:SYN-004: Command Aliases Suggested
**Given**: Command with defined alias  
**When**: User types part of the alias  
**Then**: Alias is suggested alongside full command name

### Argument Name Suggestions

#### 008:SYN-005: Argument Names Suggested After Double-Dash
**Given**: Command with named arguments  
**When**: User types `--` and triggers autocomplete  
**Then**: All available argument names (prefixed with `--`) are suggested

#### 008:SYN-006: Argument Aliases Suggested After Single-Dash
**Given**: Command with arguments that have aliases  
**When**: User types `-` and triggers autocomplete  
**Then**: All available argument aliases (prefixed with `-`) are suggested

#### 008:SYN-007: Used Arguments Filtered from Suggestions
**Given**: Some arguments already provided in the input  
**When**: User triggers argument name autocomplete  
**Then**: Only unused argument names/aliases are suggested

---

## Coverage Requirements

| Component | Target Coverage |
|-----------|-----------------|
| IAutoCompleteHandler implementations | 90% |
| ITypeAutoCompleteHandler implementations | 90% |
| AutoCompleteHandlerRegistry | 95% |
| AutoCompleteAttribute | 100% |
| Resolution logic | 95% |
| User Experience (Ghost Text/Menu) | 85% |
| Command Syntax Handlers | 90% || Remote Autocomplete | 85% |

---

## 7. Remote Autocomplete

*Source: plan.md Remote section and SignalR architecture*

### Unit Tests

#### 008:RMT-001: Autocomplete Request Serialization
**Given**: AutocompleteRequest with FullInput and CursorPosition  
**When**: Serialized to JSON  
**Then**: All properties are correctly serialized

#### 008:RMT-002: Autocomplete Response Serialization
**Given**: AutocompleteResponse with list of AutoCompleteOption  
**When**: Serialized to JSON  
**Then**: Options with Value and Description are correctly serialized

#### 008:RMT-003: AutoCompleteOption Serialization
**Given**: AutoCompleteOption with Value and optional Description  
**When**: Serialized/deserialized round-trip  
**Then**: Properties are preserved

### Integration Tests

#### 008:RMT-004: Remote Autocomplete Invocation
**Given**: Connected SignalR client  
**And**: Server with registered command having enum argument  
**When**: Client sends autocomplete request with partial value  
**Then**: Server returns filtered options via SignalR

#### 008:RMT-005: Remote Autocomplete Full Parity
**Given**: Same command registered locally and remotely  
**When**: Autocomplete triggered at identical cursor positions  
**Then**: Local and remote return identical results

#### 008:RMT-006: Remote Handler Resolution
**Given**: Server with custom Type Handler registered  
**When**: Client sends autocomplete request for matching type  
**Then**: Custom handler is used on server, results returned to client

#### 008:RMT-007: Remote Attribute Handler
**Given**: Server command with `[AutoComplete<CustomHandler>]` attribute  
**When**: Client triggers autocomplete for that argument  
**Then**: Attribute handler results returned to client

#### 008:RMT-008: Remote CursorPosition Accuracy
**Given**: Complex input with multiple arguments  
**When**: Autocomplete triggered at specific CursorPosition  
**Then**: Server correctly identifies position context

#### 008:RMT-009: Remote Empty Results
**Given**: Connected client  
**When**: Autocomplete request matches no options  
**Then**: Server returns empty list (not null or error)

### User Experience

#### 008:RMT-UX-001: Ghost Text Over Remote Connection
**Given**: Remote connection established  
**When**: User enters autocomplete-applicable position  
**Then**: Ghost text appears with minimal latency

#### 008:RMT-UX-002: Menu Over Remote Connection
**Given**: Remote connection with multiple options  
**When**: User presses Tab with ghost text visible  
**Then**: Menu opens with server-provided options

#### 008:RMT-UX-003: Type-to-Filter Over Remote
**Given**: Remote menu open  
**When**: User types filter characters  
**Then**: Filter applied locally (no round-trip per keystroke)

#### 008:RMT-UX-004: Remote Connection Failure Graceful
**Given**: Autocomplete request in progress  
**When**: Connection drops  
**Then**: No ghost text appears, no error displayed (silent degradation)

#### 008:RMT-UX-005: Slow Connection Handling
**Given**: High latency remote connection  
**When**: User triggers autocomplete  
**Then**: Ghost text appears when response arrives (no blocking)