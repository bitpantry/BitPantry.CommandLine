# Feature Specification: Command Groups

**Feature Branch**: `003-command-groups`  
**Created**: December 23, 2025  
**Status**: Draft  
**Input**: Replace namespace-based command organization with hierarchical command groups, aligning with industry CLI best practices (Docker, Azure CLI, dotnet CLI patterns)

## Overview

This feature replaces the existing "namespace" concept with "command groups" to align with industry-standard CLI patterns. The change affects command organization, syntax (dot-notation to space-separated), discoverability (invoking a group shows its contents), and the developer API (dedicated Group marker classes instead of string-based namespace attributes).

### Current vs. New Behavior

| Aspect | Current (Namespace) | New (Groups) |
|--------|---------------------|--------------|
| Syntax | `math.add --num1 5` | `math add --num1 5` |
| Invoking group alone | Not supported | Shows group help, exits 0 |
| Definition | String in `[Command(Namespace = "...")]` | Dedicated `[Group]` marker class |
| Nesting | Dot-notation in string: `"files.io"` | C# nested classes: `Files.Io` |
| Discoverability | `lc --filter "Namespace == \"math\""` | `myapp math` shows available commands |

## Clarifications

### Session 2025-12-23

- Q: What help content is displayed for a command when `--help`/`-h` is invoked? → A: Framework auto-generates help from command metadata (arguments, aliases, descriptions).
- Q: What happens when root application is invoked alone or with `--help`? → A: Display all top-level groups and root-level commands with descriptions.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Define and Invoke Grouped Commands (Priority: P1)

As a CLI developer, I want to organize related commands into groups using dedicated Group classes so that users can discover and invoke commands using space-separated syntax that matches industry-standard CLIs like Docker and Azure CLI.

**Why this priority**: Core functionality—without this, no other group features work. This is the fundamental replacement for the namespace system.

**Independent Test**: Create a Group class and a Command class referencing it, build the app, and invoke the command using space-separated syntax.

**Acceptance Scenarios**:

1. **Given** a Group class `[Group(Description = "Math operations")] class Math { }` and a command `[Command(Group = typeof(Math), Name = "add")] class AddCommand`, **When** user types `myapp math add --num1 5 --num2 3`, **Then** the AddCommand executes with the provided arguments.

2. **Given** the same setup, **When** user types `myapp math.add --num1 5`, **Then** the command is NOT recognized (dot-notation no longer valid for invocation).

3. **Given** a command with no Group specified, **When** user types `myapp version`, **Then** the root-level command executes successfully.

---

### User Story 2 - Group Discoverability (Priority: P1)

As a CLI user, I want to type a group name alone and see what commands are available within it, so that I can explore the CLI without reading external documentation.

**Why this priority**: Discoverability is a primary motivation for adopting the group pattern over namespaces.

**Independent Test**: Invoke a group name alone and verify help output is displayed.

**Acceptance Scenarios**:

1. **Given** a group `Math` with commands `add`, `subtract`, and `multiply`, **When** user types `myapp math`, **Then** the CLI displays usage information listing all available commands in the group with their descriptions, and exits with code 0.

2. **Given** the same group, **When** user types `myapp math --help`, **Then** the output is identical to invoking `myapp math` alone.

3. **Given** the same group, **When** user types `myapp math -h`, **Then** the output is identical to invoking `myapp math` alone.

4. **Given** a nested group structure `Files > Io` with commands `upload` and `download`, **When** user types `myapp files`, **Then** the CLI displays subgroups (including `io`) and any direct commands under `files`.

5. **Given** the same nested structure, **When** user types `myapp files io`, **Then** the CLI displays commands available under `files io`.

---

### User Story 3 - Define Nested Groups via C# Class Nesting (Priority: P2)

As a CLI developer, I want to define nested command groups using C# nested classes so that the code structure mirrors the CLI hierarchy.

**Why this priority**: Enables multi-level organization for complex CLIs, builds on P1 foundation.

**Independent Test**: Create nested Group classes and verify commands are accessible via multi-word group paths.

**Acceptance Scenarios**:

1. **Given** nested Group classes with outer class `[Group(Description = "File operations")] class Files` containing inner class `[Group(Description = "File I/O operations")] class Io`, and a command `[Command(Group = typeof(Files.Io), Name = "upload")]`, **When** user types `myapp files io upload --path "C:\file.txt"`, **Then** the upload command executes.

2. **Given** the same structure, **When** user types `myapp files io`, **Then** group help for `Files.Io` is displayed showing the `upload` command.

---

### User Story 4 - Startup Validation (Priority: P2)

As a CLI developer, I want the application to validate group and command configuration at startup so that configuration errors are caught during development, not at runtime.

**Why this priority**: Fail-fast behavior prevents shipping broken CLIs.

**Independent Test**: Register invalid configurations and verify the app fails to start with clear error messages.

**Acceptance Scenarios**:

1. **Given** a Group class with no commands registered under it, **When** the application starts, **Then** startup fails with an error message identifying the empty group.

2. **Given** a command and a group with the same name at the same level (e.g., root-level command `files` and root-level group `Files`), **When** the application starts, **Then** startup fails with an error message explaining the collision.

3. **Given** two commands with the same name in the same group and `ReplaceDuplicateCommands = false`, **When** the application starts, **Then** startup fails with an error identifying the duplicate.

4. **Given** a valid group and command structure, **When** the application starts, **Then** startup succeeds with no errors.

---

### User Story 5 - Case Sensitivity Configuration (Priority: P3)

As a CLI developer, I want to configure whether command and group names are case-sensitive so that I can match my users' expectations based on platform.

**Why this priority**: Configurability for cross-platform support; has sensible default.

**Independent Test**: Configure case sensitivity and verify matching behavior changes accordingly.

**Acceptance Scenarios**:

1. **Given** default configuration (case-sensitive), **When** user types `myapp Math Add`, **Then** the command is NOT found (case mismatch).

2. **Given** `CaseSensitive = false` configured, **When** user types `myapp Math Add`, **Then** the command executes (case ignored).

---

### User Story 6 - Built-in Command Override Control (Priority: P3)

As a CLI developer, I want control over whether my commands can override built-in commands so that I can either prevent accidental shadowing or intentionally replace built-ins.

**Why this priority**: Safety mechanism; default changed from permissive to restrictive.

**Independent Test**: Attempt to register a command with a built-in name under different configurations.

**Acceptance Scenarios**:

1. **Given** `ReplaceDuplicateCommands = false` (new default), **When** developer registers a command named `help` that conflicts with a built-in, **Then** startup fails with an error explaining the conflict with a built-in command.

2. **Given** `ReplaceDuplicateCommands = true`, **When** developer registers a command named `help`, **Then** the developer's command replaces the built-in.

---

### Edge Cases

- What happens when a user types a group name followed by an invalid subcommand? → Display error "'{subcommand}' is not a valid command in '{group}'" with list of available commands, exit non-zero.
- What happens when a user types a non-existent group name? → Display error "'{name}' is not a recognized command or group" with suggestions, exit non-zero.
- What happens with deeply nested groups (3+ levels)? → Supported; same behavior at each level.
- How are Group class names converted to CLI group names? → Class name is used, lowercased by default.
- What if a Group class has no `[Group]` attribute? → The class is not recognized as a group; commands referencing it via `typeof()` cause a startup validation error.

## Requirements *(mandatory)*

### Functional Requirements

#### Group Definition

- **FR-001**: System MUST support defining groups via classes decorated with `[Group]` attribute.
- **FR-002**: The `[Group]` attribute MUST support a `Description` property for group help text.
- **FR-003**: Group names MUST be derived from the class name (lowercased), matching existing `CommandAttribute` name derivation behavior.
- **FR-004**: Nested groups MUST be defined via C# nested classes, each with their own `[Group]` attribute.
- **FR-005**: Commands MUST reference their group via `[Command(Group = typeof(GroupClass))]`.
- **FR-006**: Commands without a `Group` property MUST be registered at the root level.

#### Group Behavior

- **FR-007**: Groups MUST NOT be executable—they exist only to organize commands.
- **FR-008**: Invoking a group name alone MUST display help listing available subgroups and commands.
- **FR-009**: Invoking a group with `--help` or `-h` MUST produce identical output to invoking the group alone.
- **FR-010**: Group help MUST exit with code 0 (successful discovery, not an error).
- **FR-011**: Group help MUST display: usage syntax, group description, list of subgroups (if any), list of commands with descriptions.

#### Parsing

- **FR-012**: System MUST parse space-separated tokens to navigate group hierarchy (e.g., `files io upload`).
- **FR-013**: Dot-notation (`math.add`) MUST NOT be recognized as valid invocation syntax (breaking change from namespace behavior).
- **FR-014**: After resolving to a terminal command, remaining tokens MUST be parsed as arguments/options.
- **FR-015**: Case sensitivity MUST be configurable via a `CaseSensitive` option, defaulting to `true` (case-sensitive).

#### Reserved Flags

- **FR-016**: The `--help` and `-h` flags MUST be reserved at the framework level for displaying help.
- **FR-017**: Commands MUST NOT be allowed to define arguments or options named `help` or with alias `h`; attempts to do so MUST cause startup validation failure.
- **FR-018**: The framework MUST intercept `--help`/`-h` before command execution and display appropriate help (command help for commands, group help for groups).
- **FR-018a**: Help MUST only be valid when the help flag is the ONLY element after the command/group path (no other arguments or options).
- **FR-018b**: If a help flag is detected alongside other arguments or in a pipeline, the system MUST return an error: `error: --help cannot be combined with other arguments` followed by `For usage, run: <slug> --help` where `<slug>` is the group/command path.
- **FR-019**: Command help MUST be auto-generated from command metadata including: command name, description, all arguments with names/aliases/descriptions, and usage syntax.
- **FR-020**: Invoking the application with no arguments or with `--help`/`-h` MUST display all top-level groups and root-level commands with their descriptions.

#### Validation

- **FR-021**: System MUST validate group/command configuration at application startup (during `Build()`).
- **FR-022**: Empty groups (groups with no commands AND no subgroups) MUST cause startup failure with descriptive error. A group containing only subgroups is valid as it provides navigation structure.
- **FR-023**: Name collisions between a command and group at the same level MUST cause startup failure.
- **FR-024**: Duplicate command names within the same group MUST be handled per `ReplaceDuplicateCommands` setting.
- **FR-025**: `ReplaceDuplicateCommands` MUST default to `false` (error on duplicates).
- **FR-026**: When a duplicate conflicts with a built-in command, the error message MUST indicate it is a built-in.
- **FR-027**: Attempts to define arguments named `help` or with alias `h` MUST cause startup validation failure with descriptive error.

#### Full Namespace Removal

- **FR-028**: The `Namespace` property on `[Command]` attribute MUST be fully removed (not deprecated).
- **FR-029**: The `CommandInfo.Namespace` property MUST be fully removed and replaced with a `Group` reference to `GroupInfo`.
- **FR-030**: All namespace-related code, classes, properties, and helper methods MUST be removed from the codebase.
- **FR-031**: Existing tests using namespace-based organization MUST be rewritten to use group-based organization.
- **FR-032**: All existing documentation referencing namespaces MUST be updated to document the group concept instead; no namespace references should remain in documentation.

### Key Entities

- **Group**: A non-executable organizational unit that contains commands and/or subgroups. Defined by a class with `[Group]` attribute. Has Name (from class name) and Description.
- **Command**: An executable action. Defined by a class inheriting `CommandBase` with `[Command]` attribute. Optionally references a Group via `typeof()`.
- **GroupInfo**: Runtime metadata about a group including Name, Description, Parent (if nested), Subgroups list, Commands list.
- **CommandInfo**: Existing entity, modified to reference GroupInfo instead of Namespace string.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All existing CLI functionality (command execution, argument parsing, aliases, options, pipeline) continues to work with the new group-based organization.
- **SC-002**: Users can discover available commands by typing any group name alone (100% of groups respond with help).
- **SC-003**: CLI developers can organize commands into groups with zero string-based configuration (fully type-safe via `typeof()`).
- **SC-004**: All configuration errors are caught at startup—no group/command structure errors surface at runtime.
- **SC-005**: Migration from namespace to groups requires only changing attribute usage, not command logic (Execute methods unchanged).
- **SC-006**: All existing tests are updated and passing with group-based syntax.
- **SC-007**: New tests cover all group-specific behaviors (discoverability, nesting, validation, case sensitivity).
- **SC-008**: All documentation is updated to reflect group-based organization with zero references to the former namespace concept.

## Assumptions

- The `lc` (list commands) built-in will continue to exist but may be enhanced to work with the group model.
- Tab-completion behavior changes are out of scope for this spec but should continue to function.
- Remote command functionality will continue to work with groups.
- The breaking change from dot-notation to space-separation is acceptable.

## Out of Scope

- Help formatting/styling improvements beyond basic group help display.
- Tab-completion enhancements specific to groups.
- Changes to the pipeline (`|`) behavior.
- Positional argument support (separate feature).
