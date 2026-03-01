# File Path Autocomplete — Implementation Plan

## Overview

Add a `FilePathAutoCompleteHandler` available via a `[FilePathAutoComplete]` attribute binding. The handler uses `IFileSystem` (from `System.IO.Abstractions`) so it works transparently for both local and remote (sandboxed) file systems.

## Prerequisites / Refactoring

### 0. `SpectreStyleJsonConverter` — Enable `Style` serialization

Spectre's `Style` class can't round-trip through `System.Text.Json` natively (its constructor parameter types don't match its property types). However, `Style.ToMarkup()` and `Style.Parse()` round-trip perfectly (e.g., `"bold cyan1"` → `Style` → `"bold cyan1"`). A custom `JsonConverter<Style>` bridges this gap:

```csharp
public class SpectreStyleJsonConverter : JsonConverter<Style>
{
    public override Style Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Style.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, Style value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToMarkup());
}
```

Register on the shared `JsonSerializerOptions` used by SignalR / RPC serialization. This enables `Style` as a first-class property type on any serialized object (including `AutoCompleteOption`), and also unblocks sending `Theme` data from client to server for RPC operations.

**Files:** New `SpectreStyleJsonConverter.cs` (in Serialization or shared infrastructure), SignalR `JsonSerializerOptions` registration

### 1. `AutoCompleteOption` — Redesign properties

- Remove `IsGroup` property and constructor parameter
- Rename `Format` to `MenuFormat` — `string.Format` template for **menu display** (e.g., `"{0} (default)"`). When null, displays `Value` as-is.
- Add `AcceptFormat` — `string.Format` template for **writing to the input line** on acceptance (e.g., `"{0} "`). When null, writes `Value` as-is.
- Add `MenuStyle` — `Style?` property (Spectre `Style`). When non-null, the renderer applies it directly. Serializes transparently via `SpectreStyleJsonConverter` (e.g., `"cyan"` in JSON).
- `GetFormattedValue()` updated to use `MenuFormat` instead of `Format`
- Quoting logic remains in the controller — it's a universal parsing rule (values with spaces need quotes) that applies across all handlers

**Files:** `AutoCompleteOption.cs`

### 2. Remove dead `Format` usage and update syntax handlers with new format properties

The second constructor argument was misused to store descriptions. Remove these and set proper `AcceptFormat` values:

- `CommandSyntaxHandler` — stop passing descriptions as format; set `acceptFormat: "{0} "` on both commands and groups (trailing space so user types next token)
- `ArgumentNameHandler` — stop passing `arg.Description`; set `acceptFormat: "{0} "`
- `ArgumentAliasHandler` — stop passing `arg.Description`; set `acceptFormat: "{0} "`
- `AutoCompleteSuggestionProviderTests` — remove description string from `"captured"` option

**Files:** `CommandSyntaxHandler.cs`, `ArgumentNameHandler.cs`, `ArgumentAliasHandler.cs`, `AutoCompleteSuggestionProviderTests.cs`

### 3. `AutoCompleteMenuRenderer` — Replace `IsGroup` branch with `MenuStyle`

Replace the `else if (option.IsGroup) → _theme.MenuGroup` branch: when `option.MenuStyle` is non-null, apply it directly (it's already a Spectre `Style`). Use `MenuFormat` for display text when set.

**Files:** `AutoCompleteMenuRenderer.cs`

### 4. `AutoCompleteController` — Use `AcceptFormat`, remove `ShouldAddTrailingSpace`

- `ApplySelection`: apply `AcceptFormat` via `string.Format` (or write `Value` when null), then apply quoting if needed
- `AcceptGhostText`: same pattern
- Remove `ShouldAddTrailingSpace` from `AutoCompleteSuggestionProvider` — trailing space is now handler-controlled via `AcceptFormat`

**Files:** `AutoCompleteController.cs`, `AutoCompleteSuggestionProvider.cs`

### 4. `Theme` — Keep `MenuGroup` as a reusable style constant

`MenuGroup` stays as a well-known theme style (default: `new Style(Color.Cyan)`). Handlers that want the "group/container" look use `Theme.MenuGroup` when constructing options — e.g., `new AutoCompleteOption("myDir/") { MenuStyle = theme.MenuGroup }`. Since `Style` is now a first-class serializable type, theme properties can be `Style` instead of raw `Color`, and they serialize cleanly if the theme is ever sent over the wire.

**Files:** `Theme.cs`

### 5. `CommandRegistryBuilder.Build()` — Auto-register attribute handler types

Scan command argument properties for `[AutoComplete<T>]` attributes during `Build(IServiceCollection)` and register discovered handler types with DI. This eliminates the manual `services.AddTransient<MyHandler>()` boilerplate that every attribute handler consumer currently repeats.

**Files:** `CommandRegistryBuilder.cs`

## New Feature

### 6. `FilePathAutoCompleteHandler`

Implements `IAutoCompleteHandler` (attribute handler, not type handler — `string` is too broad for auto-matching). Constructor-injected `IFileSystem` provides transparent local/remote file system access.

Behavior:
- Splits `QueryString` into directory prefix + filename fragment
- Enumerates matching entries via `IFileSystem.Directory`
- Directories: appended separator, styled with the "group/container" menu style
- Files: plain value, no special styling
- Sort order: directories first, then files, alphabetical within each
- Empty query: lists current directory contents
- Non-existent directory: returns empty list (graceful)
- Case-insensitive matching

**Files:** `Handlers/FilePathAutoCompleteHandler.cs` (new)

### 7. `FilePathAutoCompleteAttribute`

Syntactic sugar inheriting `AutoCompleteAttribute<FilePathAutoCompleteHandler>`. Enables `[FilePathAutoComplete]` on command properties.

**Files:** `Handlers/FilePathAutoCompleteAttribute.cs` (new)

## Tests

### 8. `FilePathAutoCompleteHandlerTests`

Unit tests using `MockFileSystem` from `TestableIO.System.IO.Abstractions.TestingHelpers` (already a test project dependency). Follows the direct-handler-instantiation pattern from `BooleanAutoCompleteHandlerTests`.

Test cases:
- `GetOptionsAsync_EmptyQuery_ReturnsAllEntriesInCurrentDir`
- `GetOptionsAsync_PartialFilename_ReturnsMatchingEntries`
- `GetOptionsAsync_DirectoryPrefix_ReturnsEntriesInSubdir`
- `GetOptionsAsync_DirectoryPrefixWithFragment_FiltersWithinSubdir`
- `GetOptionsAsync_NonExistentDirectory_ReturnsEmptyList`
- `GetOptionsAsync_DirectoryEntries_AppendSeparator`
- `GetOptionsAsync_SortsDirectoriesBeforeFiles`
- `GetOptionsAsync_CaseInsensitiveMatching`
- `GetOptionsAsync_TrailingSlashQuery_ListsDirectoryContents`
- `GetOptionsAsync_RelativeDotDot_ReturnsParentEntries`
- `Attribute_HandlerType_ReturnsFilePathAutoCompleteHandler`

Also verify no regressions from `IsGroup` removal and `Format` cleanup in existing tests.

**Files:** `Tests/AutoComplete/Handlers/FilePathAutoCompleteHandlerTests.cs` (new)

## Documentation

### 9. Update built-in handlers docs

Add `FilePathAutoCompleteHandler` section to `docs/autocomplete/built-in-handlers.md` following the existing pattern (description, code example, console interaction, registration note).

**Files:** `docs/autocomplete/built-in-handlers.md`

---

## Handler-vs-Controller Tension Points

The move from `IsGroup` to `MenuStyle` revealed a broader pattern: the controller/provider makes formatting and behavior decisions that only the handler has enough context to make. These are grouped here as a set of related design problems.

### ~~Tension 1: Menu option styling (`MenuStyle` on `AutoCompleteOption`)~~ — Resolved

Solved by `SpectreStyleJsonConverter`. `MenuStyle` is `Style?` — a native Spectre `Style` object everywhere in code. The custom `JsonConverter<Style>` serializes it as a markup string (e.g., `"bold cyan"`) and deserializes via `Style.Parse()`. Handlers set `MenuStyle` directly (e.g., from `Theme.MenuGroup`), the renderer applies it as-is, and serialization is transparent. This also unblocks sending `Theme` properties (which can now be `Style` instead of raw `Color`) from client to server if needed.

**Status:** No remaining design questions.

### ~~Tension 2: Trailing space after acceptance~~ — Resolved

Solved by `AcceptFormat`. Handlers set `acceptFormat: "{0} "` when a trailing space is needed (commands, groups, argument names) or leave it null / `"{0}"` when not (argument values, directory paths). `ShouldAddTrailingSpace` is removed from the controller. Quoting logic stays in the controller as a universal parsing rule.

### Tension 3: Ghost text styling (minor, client-only)

Ghost text uses `_theme.GhostText` directly in `GhostTextController`. This is entirely client-side and not serialized, so there's **no tension** — it works as-is. Listed here only for completeness.

**Status:** No change needed.
