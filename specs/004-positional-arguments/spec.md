# Feature Specification: Positional Arguments

**Feature Branch**: `004-positional-arguments`  
**Created**: December 24, 2025  
**Status**: Draft  
**Input**: User description: "Add positional arguments support to the CLI framework"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Define Positional Arguments on Commands (Priority: P1)

As a CLI implementer, I want to define positional arguments on my command classes so that users can invoke commands with a natural, concise syntax without prefixing every value with `--argName`.

**Why this priority**: This is the foundational capability that enables all other positional argument features. Without the ability to define positional arguments, no other functionality can work.

**Independent Test**: Can be fully tested by creating a command class with positional argument properties and verifying the command registers successfully at startup.

**Acceptance Scenarios**:

1. **Given** a command class with a property decorated with `[Argument(Position = 0)]`, **When** the command is registered, **Then** the argument is recognized as positional at position 0.
2. **Given** a command class with multiple positional arguments at positions 0, 1, and 2, **When** the command is registered, **Then** all positional arguments are recognized in their defined order.
3. **Given** a command class with both positional (`Position >= 0`) and named (`Position = -1` or unspecified) arguments, **When** the command is registered, **Then** both argument types are recognized correctly.

---

### User Story 2 - Invoke Commands with Positional Arguments (Priority: P1)

As a CLI user, I want to provide argument values by position (without `--` prefixes) so that I can use familiar, concise command syntax like `copy source.txt dest.txt`.

**Why this priority**: This is the core user-facing capability that delivers the primary value of positional arguments.

**Independent Test**: Can be fully tested by invoking a command with space-separated values and verifying they are correctly bound to the corresponding positional properties.

**Acceptance Scenarios**:

1. **Given** a command with positional arguments at positions 0 and 1, **When** the user invokes `mycommand value0 value1`, **Then** `value0` is bound to position 0 and `value1` is bound to position 1.
2. **Given** a command with a positional argument and a named argument, **When** the user invokes `mycommand positionalValue --namedArg namedValue`, **Then** both values are correctly bound.
3. **Given** a command with required positional arguments, **When** the user invokes the command without providing the required values, **Then** an appropriate error is displayed indicating which positional arguments are missing.

---

### User Story 3 - Variadic Positional Arguments (Priority: P2)

As a CLI implementer, I want to define a "rest" positional argument that captures all remaining positional values so that users can provide multiple values naturally, like `rm file1.txt file2.txt file3.txt`.

**Why this priority**: Variadic arguments are a common CLI pattern that significantly improves usability for commands that operate on multiple items.

**Independent Test**: Can be fully tested by creating a command with an `IsRest` collection property and invoking it with multiple trailing values.

**Acceptance Scenarios**:

1. **Given** a command with `[Argument(Position = 1, IsRest = true)]` on a `string[]` property, **When** the user invokes `mycommand first second third fourth`, **Then** `first` binds to position 0 and `["second", "third", "fourth"]` binds to the rest property.
2. **Given** a command with an `IsRest` positional argument, **When** the user provides zero additional values after the required positionals, **Then** the rest property is an empty collection.
3. **Given** a command with an `IsRest` positional and a named argument, **When** the user invokes `mycommand pos1 pos2 pos3 --flag`, **Then** positional values stop at the first `--` prefix and the named argument is handled separately.

---

### User Story 4 - Repeated Named Options (Priority: P2)

As a CLI user, I want to specify the same named option multiple times so that I can build up a collection of values naturally, like `--file a.txt --file b.txt`.

**Why this priority**: This is a POSIX-standard pattern that complements the existing delimiter-based collection parsing and provides flexibility for users.

**Independent Test**: Can be fully tested by invoking a command with repeated `--option value` pairs and verifying all values are collected.

**Acceptance Scenarios**:

1. **Given** a command with a collection-type named argument, **When** the user invokes `mycommand --items one --items two --items three`, **Then** the property contains `["one", "two", "three"]`.
2. **Given** a command with a collection-type named argument, **When** the user provides both delimiter syntax and repeated options (`--items "a;b" --items c`), **Then** all values are merged into `["a", "b", "c"]`.
3. **Given** a command with a scalar (non-collection) named argument, **When** the user provides the same option twice, **Then** an appropriate error is displayed indicating duplicate argument.

---

### User Story 5 - Auto-Complete for Positional Arguments (Priority: P3)

As a CLI user, I want auto-complete to suggest values for positional argument slots so that I get the same assistance I receive for named arguments.

**Why this priority**: Auto-complete enhances usability but is not required for basic positional argument functionality.

**Independent Test**: Can be fully tested by triggering auto-complete at a positional slot and verifying the correct argument's completion function is invoked.

**Acceptance Scenarios**:

1. **Given** a command with a positional argument that has an `AutoCompleteFunctionName` defined, **When** the user triggers auto-complete at that position, **Then** the completion function is invoked and suggestions are displayed.
2. **Given** a command with an `IsRest` positional argument, **When** the user triggers auto-complete after providing some values, **Then** the rest argument's completion function is invoked with previously entered values available in context.
3. **Given** a command with positional arguments but no completion function defined, **When** the user triggers auto-complete, **Then** no suggestions are shown (graceful no-op).

---

### User Story 6 - Help Display for Positional Arguments (Priority: P3)

As a CLI user, I want help output to clearly show positional arguments in the usage synopsis so that I understand the expected command syntax.

**Why this priority**: Help display improves discoverability but is not required for core functionality.

**Independent Test**: Can be fully tested by requesting help for a command with positional arguments and verifying the synopsis format.

**Acceptance Scenarios**:

1. **Given** a command with positional arguments, **When** the user requests help, **Then** the usage synopsis shows positional arguments with angle brackets (e.g., `copy <source> <destination>`).
2. **Given** a command with a variadic positional argument, **When** the user requests help, **Then** the synopsis indicates multiple values with ellipsis (e.g., `rm <files>...`).
3. **Given** a command with optional positional arguments, **When** the user requests help, **Then** optional positionals are shown with square brackets (e.g., `greet <name> [<title>]`).

---

### Edge Cases

- What happens when the user provides more positional values than defined positional arguments (and no `IsRest` exists)? → Error indicating excess positional arguments.
- What happens when positional arguments are provided after named arguments (violating positional-first policy)? → Values after `--arg` are treated as that named argument's value, not positional.
- How does the system handle a positional argument whose value starts with `--`? → Users can quote it or use the `--` end-of-options separator.
- How does the `--` end-of-options separator work? → A bare `--` token signals that subsequent tokens (until the first named option) are positional values even if they start with `-` or `--` (e.g., `rm -- -rf.txt` treats `-rf.txt` as a filename). The `--` separator does not allow positional values after named options have begun.
- What happens when `IsRest` is defined on a non-collection type? → Registration-time validation error.
- What happens when position indices have gaps (e.g., 0, 2 with no 1)? → Registration-time validation error.

## Requirements *(mandatory)*

### Functional Requirements

#### Attribute & Definition

- **FR-001**: System MUST support a `Position` property on `ArgumentAttribute` where `-1` indicates named-only and `0+` indicates positional order.
- **FR-002**: System MUST support an `IsRest` boolean property on `ArgumentAttribute` to indicate the argument captures all remaining positional values.
- **FR-003**: System MUST expose `Position`, `IsRest`, and computed `IsPositional` properties on `ArgumentInfo`.

#### Registration-Time Validation

- **FR-004**: System MUST validate at registration that `IsRest = true` is only applied to collection types (array or `ICollection<T>`).
- **FR-005**: System MUST validate at registration that `IsRest = true` requires a positional argument (`Position >= 0`).
- **FR-006**: System MUST validate at registration that only one argument per command has `IsRest = true`.
- **FR-007**: System MUST validate at registration that the `IsRest` argument has the highest position index.
- **FR-008**: System MUST validate at registration that position indices are contiguous (0, 1, 2... no gaps).
- **FR-009**: System MUST validate at registration that no duplicate position indices exist.
- **FR-010**: System MUST provide descriptive error messages identifying the command and property when validation fails.

#### Parsing

- **FR-011**: System MUST classify non-prefixed tokens appearing before any `--` or `-` prefixed tokens as positional values.
- **FR-012**: System MUST enforce positional-first policy: positional arguments must appear before named arguments in user input.
- **FR-013**: System MUST support a new `PositionalValue` element type in the parsing pipeline.
- **FR-014**: System MUST support the POSIX `--` end-of-options separator: a bare `--` token signals that subsequent tokens are treated as positional values regardless of prefix, but only within the contiguous positional region before named options.

#### Resolution & Binding

- **FR-015**: System MUST match positional elements to `ArgumentInfo` entries by position order.
- **FR-016**: System MUST collect all remaining positional tokens for `IsRest` arguments.
- **FR-017**: System MUST parse each token individually for `IsRest` arguments using BitPantry.Parsing.Strings to convert to the collection's element type.
- **FR-018**: System MUST report errors for missing required positional arguments.
- **FR-019**: System MUST report errors for excess positional values when no `IsRest` is defined.

#### Repeated Named Options

- **FR-020**: System MUST allow the same named option to appear multiple times for collection-type arguments.
- **FR-021**: System MUST aggregate values from repeated named options into the collection.
- **FR-022**: System MUST merge delimiter-parsed values with repeated option values when both are provided.
- **FR-023**: System MUST continue to report duplicate errors for repeated non-collection named arguments.

#### Auto-Complete

- **FR-024**: System MUST determine positional slot by counting preceding positional tokens.
- **FR-025**: System MUST invoke the appropriate argument's `AutoCompleteFunctionName` for positional slots.
- **FR-026**: System MUST provide already-entered values in `AutoCompleteContext` for all argument types including repeated options and `IsRest` positionals.

##### Prefix-Driven Intent Detection

The autocomplete system uses prefix-driven intent detection to determine what completions to offer. The following table defines the priority order of operations:

| Priority | User Input Pattern | System Behavior |
|----------|-------------------|-----------------|
| 1 | `--` or `--<partial>` + Tab | Show named argument names (--verbose, --mode), filtered by partial |
| 2 | `-` or `-<partial>` + Tab | Show argument aliases (-v, -m), filtered by partial |
| 3 | `<text>` (no dash prefix) + Tab | Show positional completions for current slot, filtered by text |
| 4 | (nothing) + Tab | Show positional completions if slot available; otherwise show options |

- **FR-024a**: System MUST use prefix-driven intent detection: `--` prefix triggers named argument name completion, `-` prefix triggers alias completion, no prefix triggers positional completion.
- **FR-024b**: If no positional completions are available (all slots filled, no completion function defined, or completion function returns empty), system MUST fall back to suggesting named argument names.
- **FR-024c**: Autocomplete MUST be cursor-position-aware. When cursor is positioned in the middle of a command line, autocomplete considers only the context at the cursor position, not tokens that appear after the cursor.

##### Dual-Mode Positional Arguments

Positional arguments can be satisfied either by position OR by explicit naming:

- **FR-025a**: System MUST allow positional arguments to be satisfied via explicit `--Name value` syntax.
- **FR-025b**: When determining the current positional slot for autocomplete, system MUST exclude positions already satisfied via named syntax.
- **FR-025c**: Required positional argument validation MUST be satisfied if the argument is provided either positionally or by name.

#### Help Display

- **FR-027**: System MUST display positional arguments in usage synopsis with angle brackets for required arguments.
- **FR-028**: System MUST display variadic positional arguments with ellipsis notation.
- **FR-029**: System MUST display optional positional arguments with square brackets.
- **FR-030**: System MUST document that collection-type named options can be repeated.

### Key Entities

- **ArgumentAttribute**: Extended with `Position` (int) and `IsRest` (bool) properties.
- **ArgumentInfo**: Extended with `Position`, `IsRest`, and computed `IsPositional` properties.
- **CommandElementType**: Extended with `PositionalValue` enum value.
- **InputMap**: Value type changed from `ParsedCommandElement` to `ArgumentValues` wrapper to support multiple values per argument for repeated options and `IsRest`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: CLI implementers can define positional arguments using familiar attribute-based syntax consistent with existing argument definitions.
- **SC-002**: CLI users can invoke commands using standard positional syntax (e.g., `copy src dest`) without learning new conventions.
- **SC-003**: Invalid positional argument configurations are caught at application startup with clear error messages, not at runtime.
- **SC-004**: Auto-complete provides suggestions for positional slots with the same responsiveness as named arguments.
- **SC-005**: Help output clearly distinguishes positional arguments from named options using industry-standard notation.
- **SC-006**: Commands with variadic arguments can accept unlimited trailing values without artificial limits.
- **SC-007**: Repeated named options work seamlessly with existing delimiter-based collection parsing.

## Clarifications

### Session 2025-12-24

- Q: Should the system support the POSIX `--` end-of-options separator? → A: Yes, support `--` separator: a bare `--` token ends option parsing; all subsequent tokens are positional.
- Q: Should positional values be allowed after named options via `--` separator (interleaving)? → A: No interleaving; all positional values must be contiguous before the first named option; `--` only escapes dash-prefixed values at that position.

### Session 2025-12-28

- Q: Can positional arguments be satisfied via named syntax (e.g., `--Name value` for position 0)? → A: Yes, dual-mode: positional arguments can be satisfied either by position or by explicit naming.
- Q: How does autocomplete decide between positional completions and option name completions? → A: Prefix-driven intent detection. `--` prefix shows options, `-` prefix shows aliases, no prefix shows positional completions.
- Q: What if user is at a position where both positional and named are valid? → A: Tab with no prefix shows positional completions (if available); user types `--` to see options.
- Q: Should autocomplete be cursor-aware when editing mid-line? → A: Yes, autocomplete considers only context at cursor position, not tokens after cursor.

## Assumptions

- Positional arguments appearing after named arguments will be treated as values for the preceding named argument, not as positional values (positional-first policy).
- Positional values must be contiguous: once a named option is encountered, no further positional binding occurs. The `--` separator only escapes dash-prefixed tokens within the positional region.
- The existing BitPantry.Parsing.Strings library will be used for parsing individual positional tokens to their target types.
- Position indices are 0-based to align with common programming conventions.
- The `Option<T>` type (flag-style arguments) is not meaningful for positional arguments since presence-based flags require a prefix.
