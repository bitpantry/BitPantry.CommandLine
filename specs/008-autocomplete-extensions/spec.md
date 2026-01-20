# Feature Specification: Extension-Based Autocomplete System

**Feature Branch**: `008-autocomplete-extensions`  
**Created**: January 17, 2026  
**Status**: Draft  
**Input**: User description: "Build an extension-based autocomplete system that fits within the existing architecture and is easy to extend and use by command implementers and package users"

**Related Documents**:
- [experience.md](experience.md) - User experience specification with console mockups and interaction flows

## Clarifications

### Session 2026-01-18

- Q: How many menu items should be visible before scrolling activates? → A: Fixed default of 5 visible items, defined as a central constant for easy updates later.
- Q: How should autocomplete suggestions be ordered within a category? → A: Alphabetical (A-Z) for all suggestions.
- Q: Can extensions receive concurrent autocomplete requests? → A: Single-threaded; one request at a time, cancel previous on new input.
- Q: When a provider throws an exception, should error details be logged? → A: Always log to ILogger (require logging DI registration).
- Q: Should ghost text and menu filtering match case-insensitively or case-sensitively? → A: Case-insensitive (typing "de" matches "Debug").

## Overview

### Complete System Replacement

This specification defines a **complete replacement** of the existing autocomplete system. The current implementation—including convention-based `AutoCompleteFunctionName` methods, the existing command syntax completion logic, and all associated infrastructure—will be **entirely removed and rewritten**.

The legacy system uses a convention-based function naming pattern (`AutoCompleteFunctionName`) where autocomplete logic is defined as methods within command classes. This pattern will be deleted. The new provider-based architecture replaces **all** autocomplete functionality with a unified, extensible design.

**Scope of Removal**:
- `AutoCompleteFunctionName` property on `ArgumentAttribute` → Removed
- `AutoCompleteFunctionName` property on `ArgumentInfo` → Removed
- `IsAutoCompleteFunctionAsync` property on `ArgumentInfo` → Removed
- Legacy autocomplete function invocation code in `AutoCompleteOptionSetBuilder` → Removed
- Existing command syntax completion logic → Removed and reimplemented

### New Architecture

The replacement system provides:

#### Three Handler Categories

All handlers implement `IAutoCompleteHandler` with a single `GetOptionsAsync` method. Handlers are categorized by how they are bound to arguments:

1. **Attribute Handlers (Explicit)**: Command implementers decorate arguments with `[AutoComplete<THandler>]`. Only one explicit attribute is allowed per argument. Resolved from DI when invoked.

2. **Type Handlers (Implicit)**: The system automatically applies autocomplete based on argument type (e.g., enums, booleans). These implement `ITypeAutoCompleteHandler` which adds `CanHandle(Type)` for runtime matching. Extensible for additional types.

3. **Syntax Handlers (Internal)**: Handle command structure completion (groups, commands, argument names/aliases). Implemented internally and not exposed for extension.

**Precedence Rule**: Explicit (Attribute) always overrides Implicit (Type). Syntax handlers are invoked based on position context, not argument type.

#### Key Capabilities

- **Single explicit attribute per argument**: Compile-time enforcement prevents ambiguity
- **Unified handler interface**: All handlers implement `IAutoCompleteHandler` with `GetOptionsAsync`
- **Extensible Type Handlers**: Package users can register new type-based handlers
- **Filtering in handlers**: Handlers receive `QueryString` in context and filter their own results
- **Built-in command syntax autocomplete**: Groups, commands, command aliases, argument names, and argument aliases are all reimplemented using internal syntax handlers

**Breaking Change**: All commands using the `AutoCompleteFunctionName` pattern must migrate to the new attribute-based approach.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Built-in Type Autocomplete (Priority: P1)

As a command implementer, I want autocomplete to work automatically for common types (enums, booleans) without any configuration, so that my commands have good UX out of the box.

**Why this priority**: This delivers immediate value with zero effort from command implementers. Most commands have enum or boolean arguments, and providing autocomplete for these types automatically dramatically improves user experience.

**Independent Test**: Create a command with an enum argument and a boolean argument. Press Tab while typing values and verify suggestions appear automatically.

**Acceptance Scenarios**:

1. **Given** a command with an enum-typed argument and no autocomplete attribute, **When** the user presses Tab while typing the argument value, **Then** the system displays all enum values as suggestions filtered by the partial input.
2. **Given** a command with a boolean-typed argument and no autocomplete attribute, **When** the user presses Tab while typing the argument value, **Then** the system displays "true" and "false" as suggestions.
3. **Given** a command with a nullable enum argument, **When** the user presses Tab, **Then** the system displays enum values as suggestions.
4. **Given** an enum-typed argument with an explicit autocomplete attribute, **When** the user triggers autocomplete, **Then** the explicit attribute takes precedence over the automatic enum autocomplete.

---

### User Story 2 - Custom Extension Registration (Priority: P1)

As a package user, I want to create and register custom autocomplete extensions for specific types or scenarios, so that I can provide tailored autocomplete experiences for my domain.

**Why this priority**: This enables the extensibility that is the core value proposition of the new architecture.

**Independent Test**: Create a custom extension for file paths, register it, create a command with a file path argument, and verify the extension provides completions.

**Acceptance Scenarios**:

1. **Given** a custom extension registered for a specific type, **When** a command has an argument of that type and the user triggers autocomplete, **Then** the custom extension provides suggestions.
2. **Given** multiple extensions that could handle an argument, **When** the user triggers autocomplete, **Then** extensions are evaluated in priority order and the first matching one provides suggestions.
3. **Given** a custom extension with dependencies, **When** the extension is resolved, **Then** dependencies are injected from the service provider.

---

### User Story 3 - Attribute-Based Extension Assignment (Priority: P2)

As a command implementer, I want to use an attribute to specify which extension an argument should use for autocomplete, so that I can control behavior precisely while maintaining a clean, declarative API.

**Why this priority**: Provides fine-grained control when automatic type-based detection isn't sufficient. Attributes are the standard .NET pattern for declarative metadata.

**Independent Test**: Create an argument with an explicit autocomplete attribute and verify that specific extension is used.

**Acceptance Scenarios**:

1. **Given** an argument decorated with an autocomplete attribute specifying an extension, **When** the user triggers autocomplete, **Then** that extension provides suggestions regardless of the argument type.
2. **Given** an explicit autocomplete attribute that references a non-existent extension, **When** the user triggers autocomplete, **Then** the system falls back to type-based extensions or provides no suggestions gracefully.
3. **Given** an argument with both a type that has automatic autocomplete (e.g., enum) and an explicit attribute, **When** the user triggers autocomplete, **Then** the explicit attribute overrides the automatic behavior.

---

### User Story 4 - Context-Aware Suggestions (Priority: P2)

As a command implementer, I want extensions to receive context about other argument values already provided, so that suggestions can be dynamic based on user input.

**Why this priority**: This replaces the context-passing capability of the old `AutoCompleteContext` and is essential for sophisticated autocomplete scenarios.

**Independent Test**: Create an extension that filters suggestions based on another argument's value and verify the filtering works.

**Acceptance Scenarios**:

1. **Given** an extension that reads other argument values from context, **When** the user has already provided a value for another argument, **Then** the extension receives that value in its context and can use it for filtering.
2. **Given** a positional argument with an extension, **When** prior positional arguments have been provided, **Then** those values are available in the context.

---

### User Story 5 - Remote Command Support (Priority: P3)

As a user of remote commands (client-server), I want autocomplete to work for remote commands, so that I have a consistent experience regardless of where commands execute.

**Why this priority**: Important for complete feature parity but depends on existing remote infrastructure.

**Independent Test**: Connect to a remote server, type a remote command, and verify autocomplete works by delegating to the server.

**Acceptance Scenarios**:

1. **Given** a remote command with autocomplete extensions configured, **When** the user triggers autocomplete, **Then** the request is sent to the server and results are displayed locally.
2. **Given** a disconnected remote server, **When** the user triggers autocomplete for a remote command, **Then** the system handles the error gracefully without crashing.

---

### User Story 6 - Command Syntax Autocomplete (Priority: P1)

As a user, I want autocomplete to work for all parts of command syntax (groups, commands, aliases, argument names, argument aliases), so that I can discover and use commands efficiently.

**Why this priority**: This is fundamental to the command-line experience and must work for all users, regardless of which commands are registered.

**Independent Test**: Type partial group names, command names, and argument names with Tab and verify suggestions appear for each.

**Acceptance Scenarios**:

1. **Given** registered command groups, **When** the user types a partial group name and presses Tab, **Then** matching group names are suggested.
2. **Given** commands within a group, **When** the user has typed the group name and presses Tab, **Then** commands within that group are suggested.
3. **Given** a command with a defined alias, **When** the user types part of the alias, **Then** the alias is suggested alongside the full command name.
4. **Given** a command with named arguments, **When** the user types "--" and presses Tab, **Then** all available argument names are suggested.
5. **Given** arguments with defined aliases, **When** the user types "-" and presses Tab, **Then** all available argument aliases are suggested.
6. **Given** some arguments have already been provided, **When** the user triggers argument name autocomplete, **Then** only unused argument names/aliases are suggested.

---

### Edge Cases

- What happens when a command implementer tries to add multiple autocomplete attributes to one argument?
  - Compile-time error prevents this. Only one explicit autocomplete attribute is allowed per argument.
- What happens when an extension throws an exception during `GetOptionsAsync`?
  - The system catches the exception, logs it to ILogger, and gracefully degrades to no autocomplete (no ghost text, no menu). The UX is not disrupted.
- What happens when multiple extensions return overlapping suggestions?
  - N/A - only one handler is selected per argument (first match via CanHandle, or explicit attribute).
- What happens when no extensions match an argument?
  - No suggestions are displayed; the user continues typing normally.
- What happens when an extension takes too long to respond?
  - The system respects the cancellation token; if the user continues typing, pending requests are cancelled. There is no hard timeout — a truly hanging handler will block the CLI until the user types again (which triggers cancellation).
- What happens when multiple Type Providers could handle the same type?
  - The last registered provider wins (LIFO order). This allows package users to override built-in providers.
- What happens when the user types characters that filter out all menu options?
  - The menu closes and ghost text clears; no suggestions are available for the current input.
- What happens when a positional parameter is set by name (e.g., `--level Debug`) instead of positionally?
  - The system recognizes it as satisfied; no autocomplete is offered at that positional position, and `--level` is excluded from `--` suggestions.
- What happens when the user presses Tab at a position with no autocomplete provider?
  - Nothing happens; cursor remains unchanged, no ghost text or menu appears.

## Requirements *(mandatory)*

### Functional Requirements

#### Core Handler System

- **FR-001**: System MUST provide a core handler interface (`IAutoCompleteHandler`) that all handlers implement, with a single `GetOptionsAsync` method.
- **FR-002**: System MUST provide an extended interface (`ITypeAutoCompleteHandler`) for type-based handlers that adds a `CanHandle(Type)` method for runtime matching.
- **FR-003**: System MUST provide a handler registry (`AutoCompleteHandlerRegistry`) that manages registration, discovery, and retrieval of handlers.
- **FR-004**: System MUST allow handlers to be registered via dependency injection extension methods.
- **FR-005**: For Type Handlers, system MUST use last-registered-wins ordering: the most recently registered handler that returns true from `CanHandle` is used.

#### Attribute Architecture (Attribute Handlers)

- **FR-006**: System MUST provide a base autocomplete attribute that all specific autocomplete attributes inherit from.
- **FR-007**: All autocomplete attributes MUST share common foundational behavior defined in the base attribute.
- **FR-008**: Command implementers MUST be able to decorate arguments with autocomplete attributes to specify behavior.
- **FR-009**: Only one explicit autocomplete attribute is allowed per argument; attempting to add multiple MUST result in a compile-time error.
- **FR-010**: When an argument has an explicit autocomplete attribute, that attribute MUST take precedence over implicit type-based handlers.

#### Implicit Handler Architecture (Type-based)

- **FR-011**: System MUST provide the `ITypeAutoCompleteHandler` interface with `CanHandle(Type)` method for runtime type matching.
- **FR-012**: Implicit handlers MUST be evaluated only when no explicit attribute is present on the argument.
- **FR-013**: Implicit handlers MUST be extensible, allowing package users to register new type-based handlers via `AutoCompleteHandlerRegistry.Register<T>()`.
- **FR-014**: When multiple Type Handlers could handle the same type, the last registered handler wins.

#### Built-in Implicit Handlers

- **FR-015**: System MUST include a built-in implicit handler for enum-typed arguments.
- **FR-016**: System MUST include a built-in implicit handler for boolean-typed arguments.
- **FR-017**: Built-in implicit handlers MUST be overridable by adding an explicit autocomplete attribute to the argument.

#### Command Syntax Autocomplete

- **FR-018**: System MUST provide autocomplete for command group names.
- **FR-019**: System MUST provide autocomplete for command names within groups and at root level.
- **FR-020**: System MUST provide autocomplete for command aliases.
- **FR-021**: System MUST provide autocomplete for argument names (prefixed with "--").
- **FR-022**: System MUST provide autocomplete for argument aliases (prefixed with "-").
- **FR-023**: System MUST filter out already-used argument names/aliases from suggestions.

#### Legacy Removal

- **FR-024**: System MUST remove the `AutoCompleteFunctionName` property from `ArgumentAttribute`.
- **FR-025**: System MUST remove the `IsAutoCompleteFunctionAsync` property from `ArgumentInfo`.
- **FR-026**: System MUST remove all legacy autocomplete function invocation code from `AutoCompleteOptionSetBuilder`.

#### Extension Context

- **FR-027**: System MUST provide handlers with a context object containing: query string, full input, cursor position, argument metadata, command info, and already-provided argument values.
- **FR-028**: Handlers MUST receive dependencies via constructor injection (resolved from DI at registration time), NOT via ServiceProvider in context.
- **FR-029**: System MUST support cancellation tokens for async handler operations.
- **FR-058**: Handlers MUST filter their results based on QueryString provided in context.

#### Registration & Configuration

- **FR-030**: Package users MUST be able to register custom Type Handlers via dependency injection.
- **FR-031**: Package users MUST be able to register custom Attribute Handlers via `[AutoComplete<THandler>]` attribute.
- **FR-032**: System MUST provide extension methods on `CommandLineApplicationBuilder` for registering custom Type Handlers.

#### Error Handling

- **FR-033**: System MUST catch and suppress exceptions from individual handlers without affecting other handlers or crashing the application. Exceptions MUST be logged to ILogger (logging DI registration required). The UX MUST gracefully degrade to no autocomplete (no ghost text, no menu).
- **FR-034**: When a handler returns null or empty results, the system MUST treat this as a valid response (no suggestions available). The system does NOT fall back to the next handler — first match via `CanHandle` is authoritative.

#### User Interaction Model

- **FR-035**: System MUST display ghost text automatically when the cursor enters an autocomplete-applicable position (no keypress required).
- **FR-036**: Ghost text MUST show the first available suggestion and update dynamically as the user types.
- **FR-037**: When Tab is pressed and only one option is available, the system MUST accept the ghost text immediately (no menu).
- **FR-038**: When Tab is pressed and multiple options are available, the system MUST clear ghost text and display a selection menu.
- **FR-039**: Right Arrow key MUST accept ghost text (same as Tab with single option).
- **FR-040**: Escape key MUST dismiss ghost text without accepting it.
- **FR-041**: Up/Down arrow keys with ghost text visible MUST dismiss ghost text and access command history.

#### Positional Parameter Handling

- **FR-042**: System MUST track positional parameter satisfaction state (whether set positionally or by name).
- **FR-043**: When a positional parameter is satisfied (by position or by name), its argument name MUST be excluded from `--` autocomplete suggestions.
- **FR-044**: When a positional parameter is satisfied (by position or by name), no autocomplete MUST be offered at that positional position.
- **FR-045**: Once a named argument (`--name`) appears in the input, the system MUST NOT offer positional autocomplete after it; only named arguments are available.
- **FR-046**: Unsatisfied positional parameters MUST appear in `--` suggestions as named argument options.

#### Menu Behavior

- **FR-047**: Menu MUST support type-to-filter: characters typed while menu is open filter visible options in real-time.
- **FR-048**: Menu navigation MUST wrap around (Down on last item goes to first, Up on first item goes to last).
- **FR-049**: When more options exist than fit in the visible menu, scroll indicators MUST display with counts of hidden items (`▲ N more...` / `▼ N more...`). Default visible item count is 5, defined as a central constant.
- **FR-050**: Enter key in menu MUST accept the selected option and close the menu.
- **FR-051**: Escape key in menu MUST close the menu and restore original text.
- **FR-052**: Space key in menu MUST accept current selection, insert a space, and close the menu.

#### Value Formatting

- **FR-053**: System MUST automatically wrap completion values containing spaces in double quotes when inserting.
- **FR-054**: If the user has already typed an opening quote, completion MUST continue within the quote context.

#### Suggestion Ordering

- **FR-055**: All autocomplete suggestions MUST be ordered alphabetically (A-Z) within their category.

#### Concurrency Model

- **FR-056**: Only one autocomplete request MUST be active at a time; new input MUST cancel any pending request before starting a new one.

#### Matching Behavior

- **FR-057**: Ghost text and menu filtering MUST use case-insensitive matching (e.g., typing "de" matches "Debug").

### Key Entities

- **`IAutoCompleteHandler`**: Core interface with single `GetOptionsAsync` method. All handlers implement this interface (Attribute, Type, and Syntax handlers).
- **`ITypeAutoCompleteHandler`**: Extends `IAutoCompleteHandler` with `CanHandle(Type)` method for runtime type matching. Used by Type Handlers (implicit binding).
- **`AutoCompleteAttribute<THandler>`**: Generic attribute that command implementers place on arguments to specify explicit binding. Only one allowed per argument. Takes precedence over Type Handlers.
- **`IAutoCompleteAttribute`**: Marker interface for reflection-based discovery of generic attributes.
- **`AutoCompleteHandlerRegistry`**: Manages the collection of registered Type Handlers. Uses last-registered-wins ordering. Also resolves Attribute Handlers from DI.
- **`AutoCompleteContext`**: Contains all information needed by a handler: query string, full input, cursor position, argument info, command info, and provided values. Handlers filter based on QueryString.
- **Syntax Handlers**: Built-in handlers for command structure completion (groups, commands, argument names/aliases). Implement `IAutoCompleteHandler` for consistency. Not pluggable; reimplemented as part of this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Commands with enum or boolean arguments receive autocomplete suggestions automatically without any configuration.
- **SC-002**: Custom Type Handlers can be created and registered in under 20 lines of code.
- **SC-003**: All legacy `AutoCompleteFunctionName` code is removed from the codebase.
- **SC-004**: Handler exceptions are caught and logged without user-visible errors or crashes.
- **SC-005**: Migration guide documents how to convert legacy autocomplete functions to the new handler model.
- **SC-006**: Command syntax autocomplete works for groups, commands, aliases, argument names, and argument aliases.
- **SC-007**: Explicit autocomplete attributes successfully override implicit type-based handlers.
- **SC-008**: Attempting to add multiple explicit autocomplete attributes to one argument results in a compile-time error.
- **SC-009**: Positional parameters track satisfaction state correctly, excluding satisfied args from suggestions.
- **SC-010**: Values containing spaces are automatically quoted when inserted.

## Assumptions

- The existing autocomplete infrastructure (`AutoCompleteController`, `AutoCompleteOptionSetBuilder`, etc.) will be rewritten to use the handler system.
- Built-in Type Handlers (enum, boolean) will be registered by default when the command line application is built.
- Handlers receive dependencies via constructor injection (resolved from DI).
- Handlers are responsible for filtering their own results based on QueryString.
- Existing commands using `AutoCompleteFunctionName` will need to be migrated to the new handler pattern.

## Breaking Changes

- **Removal of `AutoCompleteFunctionName`**: The `ArgumentAttribute.AutoCompleteFunctionName` property will be removed. Commands must use either:
  - Built-in Type Handlers (automatic for enums, booleans)
  - Custom Type Handlers registered via `RegisterAutoCompleteHandler<T>()`
  - Explicit `[AutoComplete<THandler>]` attribute
- **Removal of `AutoCompleteContext` record**: The legacy `AutoCompleteContext(string, Dictionary<ArgumentInfo, string>)` record will be replaced with the new `AutoCompleteContext` class in `AutoComplete/Handlers/`.
- **Test updates required**: Existing autocomplete tests using `AutoCompleteFunctionName` must be rewritten.

## Out of Scope

- Fuzzy matching or ranking of suggestions (extensions return ordered lists).
- Caching of extension results across invocations.
- Extension hot-reloading without application restart.
- Visual customization of autocomplete UI (styling, layout).
- Backward compatibility shim for legacy `AutoCompleteFunctionName` pattern.
