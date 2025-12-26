# Feature Specification: Core CLI Commands & Prompt Redesign

**Feature Branch**: `006-core-commands`  
**Created**: 2025-12-25  
**Status**: Draft  
**Input**: User description: "Redesign core CLI commands and prompt system: remove obsolete ListCommands, add Version command, redesign server connection commands, add server profile management, implement extensible prompt segment architecture"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Display Application Version (Priority: P1)

As a CLI user, I want to run `myapp version` to see the current version of the application so I can verify which version I'm running and report issues accurately.

**Why this priority**: Version information is fundamental for troubleshooting, support requests, and verifying deployments. This is the most commonly expected built-in command in any CLI.

**Independent Test**: Can be fully tested by running `version` command and verifying version string output matches the executing assembly's version.

**Acceptance Scenarios**:

1. **Given** a CLI application is running, **When** the user executes `version`, **Then** the application displays the version of the executing assembly on a single line
2. **Given** the executing assembly has an `AssemblyInformationalVersion` attribute, **When** the user executes `version`, **Then** the version displayed is the informational version (e.g., `1.2.3-beta.1`)
3. **Given** the executing assembly has no `AssemblyInformationalVersion` but has an `AssemblyVersion`, **When** the user executes `version`, **Then** the version displayed falls back to the assembly version
4. **Given** the executing assembly has neither version attribute, **When** the user executes `version`, **Then** the application displays `0.0.0` as a fallback

---

### User Story 2 - Display Full Version Information (Priority: P2)

As a CLI user or support engineer, I want to run `myapp version --full` to see the version of my application plus all BitPantry.CommandLine framework assemblies loaded, so I can diagnose compatibility issues or report complete version information.

**Why this priority**: Detailed version information is essential for debugging framework-related issues and ensuring all components are compatible.

**Independent Test**: Can be fully tested by running `version --full` and verifying output includes executing assembly version plus all loaded BitPantry.CommandLine.* assembly names and versions.

**Acceptance Scenarios**:

1. **Given** a CLI application is running, **When** the user executes `version --full`, **Then** the first line displays the executing assembly version
2. **Given** a CLI application has loaded BitPantry.CommandLine framework assemblies, **When** the user executes `version --full`, **Then** each subsequent line displays a framework assembly name followed by its version (e.g., `BitPantry.CommandLine 2.0.0`)
3. **Given** a CLI application is running, **When** the user executes `version -f`, **Then** the output is identical to `version --full` (alias support)
4. **Given** no BitPantry.CommandLine.* assemblies are loaded, **When** the user executes `version --full`, **Then** only the executing assembly version is displayed

---

### User Story 3 - Remove Obsolete ListCommands Command (Priority: P1)

As a framework maintainer, I want to remove the obsolete `ListCommands` (`lc`) command since command discovery is now baked into the syntax, reducing maintenance burden and avoiding confusion with redundant functionality.

**Why this priority**: Removing obsolete code is essential for framework health. The `lc` command is redundant now that command discovery is built into the core syntax.

**Independent Test**: Can be fully tested by verifying `lc` is no longer a recognized command and the codebase no longer contains ListCommandsCommand.

**Acceptance Scenarios**:

1. **Given** the framework is updated, **When** a user executes `lc`, **Then** the application responds with a command not found error
2. **Given** the framework is updated, **When** a developer searches the codebase for ListCommandsCommand, **Then** no implementation file exists
3. **Given** an application previously relied on the built-in `lc` command for conflict testing, **When** the framework is updated, **Then** conflict tests use the new `version` command instead

---

### User Story 4 - Connect to Remote Server (Priority: P1)

As a CLI user, I want to connect to a remote server using `server connect` so I can execute commands on that server.

**Why this priority**: Server connection is the foundational capability for all remote operations.

**Independent Test**: Can be fully tested by running `server connect` with various argument combinations and verifying connection state.

**Acceptance Scenarios**:

1. **Given** the user is not connected, **When** the user executes `server connect api.example.com`, **Then** the application connects to the server and displays `✓ Connected to api.example.com`
2. **Given** the server requires authentication and no API key is provided, **When** the user executes `server connect api.example.com` in interactive mode, **Then** the application prompts for an API key
3. **Given** the user provides `--api-key`, **When** the user executes `server connect api.example.com --api-key xyz`, **Then** the application uses that key for authentication without prompting
4. **Given** a profile named "prod" exists with saved credentials, **When** the user executes `server connect --profile prod`, **Then** the application connects using the profile's URI and saved credentials
5. **Given** the user is already connected to a different server, **When** the user executes `server connect api.example.com`, **Then** the application displays `⚠ Disconnecting from old.server.com...` and proceeds to connect
6. **Given** the user is already connected to the same server, **When** the user executes `server connect api.example.com`, **Then** the application displays `✓ Already connected to api.example.com`
7. **Given** the application is in REPL mode, **When** the user successfully connects, **Then** the prompt updates to show the connected server (e.g., `api.example.com> `)

---

### User Story 5 - Disconnect from Remote Server (Priority: P1)

As a CLI user, I want to disconnect from a remote server using `server disconnect` so I can return to local mode or connect to a different server.

**Why this priority**: Clean disconnection is essential for proper session management.

**Independent Test**: Can be fully tested by connecting, then disconnecting, and verifying state.

**Acceptance Scenarios**:

1. **Given** the user is connected to a server, **When** the user executes `server disconnect`, **Then** the application disconnects and displays `✓ Disconnected`
2. **Given** the user is not connected to any server, **When** the user executes `server disconnect`, **Then** the application displays `⚠ Not connected to any server`
3. **Given** the application is in REPL mode, **When** the user disconnects, **Then** the prompt reverts to the local prompt (e.g., `myapp> `)

---

### User Story 6 - Check Server Connection Status (Priority: P2)

As a CLI user, I want to check my current connection status using `server status` so I can see if I'm connected and to which server.

**Why this priority**: Status visibility is essential for users to understand their current context.

**Independent Test**: Can be fully tested by checking status in connected and disconnected states.

**Acceptance Scenarios**:

1. **Given** the user is connected to a server, **When** the user executes `server status`, **Then** the application displays connection details including server address and profile (if applicable)
2. **Given** the user is not connected, **When** the user executes `server status`, **Then** the application displays `Status: Not connected`
3. **Given** the user requests JSON output, **When** the user executes `server status --json`, **Then** the application outputs status as a JSON object
4. **Given** the user is not connected, **When** the user executes `server status` in a script, **Then** the command exits with a non-zero exit code

---

### User Story 7 - Manage Server Profiles (Priority: P2)

As a CLI user, I want to save, list, and manage server profiles so I can quickly connect to frequently-used servers without re-entering credentials.

**Why this priority**: Profile management significantly improves developer experience for users working with multiple environments.

**Independent Test**: Each profile command can be tested independently by performing CRUD operations and verifying stored profile data.

**Acceptance Scenarios**:

1. **Given** no profiles exist, **When** the user executes `server profile list`, **Then** the application displays a message indicating no profiles are saved
2. **Given** profiles exist, **When** the user executes `server profile list`, **Then** the application displays all profiles with their URIs and default indicator
3. **Given** a profile named "prod" does not exist, **When** the user executes `server profile add prod --uri api.example.com`, **Then** the profile is created and the user is prompted for an API key
4. **Given** a profile named "prod" already exists, **When** the user executes `server profile add prod --uri new.example.com`, **Then** the application prompts for confirmation before overwriting
5. **Given** the user provides `--force`, **When** the user executes `server profile add prod --uri new.example.com --force`, **Then** the profile is overwritten without confirmation
6. **Given** a profile named "prod" exists, **When** the user executes `server profile remove prod`, **Then** the profile and its saved credentials are deleted
7. **Given** a profile named "prod" exists, **When** the user executes `server profile show prod`, **Then** the application displays the profile's URI and whether credentials are saved (not the credentials themselves)
8. **Given** a profile named "prod" exists, **When** the user executes `server profile set-default prod`, **Then** that profile becomes the default for `server connect` without arguments
9. **Given** a profile named "prod" exists, **When** the user executes `server profile set-key prod`, **Then** the application prompts for a new API key and updates the saved credentials

---

### User Story 8 - Autocomplete for Profile Names (Priority: P2)

As a CLI user, I want profile names to autocomplete when I type commands that accept a profile name, so I can quickly select profiles without remembering exact names.

**Why this priority**: Autocomplete significantly improves usability and reduces errors when working with multiple profiles.

**Independent Test**: Can be tested by triggering autocomplete in REPL mode for profile-related arguments and verifying suggestions.

**Acceptance Scenarios**:

1. **Given** profiles "prod", "staging", and "dev" exist, **When** the user types `server connect --profile ` and triggers autocomplete, **Then** all three profile names are suggested
2. **Given** profiles "prod", "staging", and "dev" exist, **When** the user types `server connect --profile pr` and triggers autocomplete, **Then** "prod" is suggested
3. **Given** profiles exist, **When** the user types `server profile add ` and triggers autocomplete, **Then** all profile names are suggested (for update scenarios)
4. **Given** profiles exist, **When** the user types `server profile remove ` and triggers autocomplete, **Then** all profile names are suggested
5. **Given** profiles exist, **When** the user types `server profile show ` and triggers autocomplete, **Then** all profile names are suggested
6. **Given** profiles exist, **When** the user types `server profile set-default ` and triggers autocomplete, **Then** all profile names are suggested
7. **Given** profiles exist, **When** the user types `server profile set-key ` and triggers autocomplete, **Then** all profile names are suggested
8. **Given** no profiles exist, **When** the user triggers autocomplete for profile name, **Then** no suggestions are shown

---

### User Story 9 - Extensible Prompt System (Priority: P1)

As a framework developer, I want the prompt to be composed of independent segments that each package can contribute, so the prompt automatically reflects connection state and other context without tight coupling between packages.

**Why this priority**: The current prompt system requires commands to manually update the prompt, creating coupling and potential for state inconsistencies. The new system is foundational for proper UX.

**Independent Test**: Can be tested by registering different segment combinations and verifying prompt output.

**Acceptance Scenarios**:

1. **Given** only the core library is used, **When** the REPL prompt renders, **Then** it displays the application name (e.g., `myapp> `)
2. **Given** the SignalR client is configured and connected, **When** the REPL prompt renders, **Then** it includes the server hostname (e.g., `myapp @api.example.com> `)
3. **Given** connected via a named profile, **When** the REPL prompt renders, **Then** it includes the profile name (e.g., `myapp @api.example.com [prod]> `)
4. **Given** a custom segment is registered, **When** the REPL prompt renders, **Then** the custom segment appears in the correct order
5. **Given** a segment returns null, **When** the REPL prompt renders, **Then** that segment is skipped with no visual artifacts
6. **Given** the user registers a custom `IPrompt` implementation, **When** the REPL prompt renders, **Then** the custom implementation is used instead of the composite

---

### User Story 10 - Built-in Command Documentation (Priority: P2)

As a developer using the BitPantry.CommandLine framework, I want comprehensive documentation for all built-in commands organized by package, so I can understand what commands are available, how to use them, and which package provides them.

**Why this priority**: Documentation is essential for developer adoption and reduces support burden. Developers need to know which commands come with which packages.

**Independent Test**: Can be validated by reviewing documentation for completeness against the command reference in this spec.

**Acceptance Scenarios**:

1. **Given** a developer is reading the documentation, **When** they navigate to the built-in commands section, **Then** they see an index of all built-in commands organized by package
2. **Given** a developer wants to use the `version` command, **When** they read its documentation, **Then** they see the package (Core), syntax, arguments, behaviors, error cases, and examples
3. **Given** a developer wants to use `server connect`, **When** they read its documentation, **Then** they see the package (SignalR Client), syntax, arguments, behaviors, error cases, and examples
4. **Given** a developer is evaluating which packages to install, **When** they read the built-in commands index, **Then** they can see which commands require which packages
5. **Given** documentation exists for a command, **When** the command behavior changes, **Then** the documentation is updated to match

---

## Command Reference *(mandatory)*

### Core Commands

#### `version`

**Syntax**: `version [--full | -f]`

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `--full` | `-f` | flag | No | Include framework assembly versions |

**Output (default)**:
```
1.2.3
```

**Output (--full)**:
```
1.2.3
BitPantry.CommandLine 2.0.0
BitPantry.CommandLine.Remote.SignalR 2.0.0
BitPantry.CommandLine.Remote.SignalR.Client 2.0.0
```

**Exit Codes**:
- `0`: Success

---

### Server Connection Commands

#### `server connect`

**Syntax**: `server connect [uri] [--profile | -p <name>] [--api-key | -k <key>] [--timeout | -t <seconds>]`

| Argument | Alias | Type | Required | Autocomplete | Description |
|----------|-------|------|----------|--------------|-------------|
| `uri` | - | positional | No* | - | Server URI to connect to |
| `--profile` | `-p` | string | No* | Profile names | Use saved profile |
| `--api-key` | `-k` | string | No | - | API key for authentication |
| `--timeout` | `-t` | int | No | - | Connection timeout in seconds (default: 10) |

*Either `uri` or `--profile` or a default profile must be available.

**Behavior**:
1. Validate: If both `uri` AND `--profile` provided → error
2. Validate: If neither `uri` nor `--profile` nor default profile → error with usage help
3. If already connected to same server → display success, no action
4. If already connected to different server → disconnect first, then connect
5. Resolve credentials: `--api-key` → profile credentials → interactive prompt
6. Attempt connection with progress indicator
7. Update REPL prompt on success

**Output Examples**:
```
# Success
Connecting to api.example.com...
✓ Connected to api.example.com

# Already connected
✓ Already connected to api.example.com

# Switching servers
⚠ Disconnecting from old.server.com...
Connecting to api.example.com...
✓ Connected to api.example.com
```

**Error Examples**:
```
# No target specified
✗ No connection target specified

Usage:
  server connect <uri>              Connect to a server
  server connect --profile <name>   Connect using a saved profile
  server connect                    Connect using default profile

No default profile is set. Set one with:
  server profile set-default <name>

# Both uri and profile
✗ Cannot specify both URI and --profile

# Connection failed
✗ Could not reach api.example.com

# Connection timeout
✗ Connection to api.example.com timed out after 10 seconds

# Auth failed
✗ Authentication failed — invalid API key
```

**Exit Codes**:
- `0`: Successfully connected
- `1`: Connection failed
- `2`: Authentication failed
- `3`: Invalid arguments

---

#### `server disconnect`

**Syntax**: `server disconnect`

No arguments.

**Behavior**:
1. If not connected → display warning
2. Disconnect with progress indicator
3. Update REPL prompt on success

**Output Examples**:
```
# Success
Disconnecting from api.example.com...
✓ Disconnected

# Not connected
⚠ Not connected to any server
```

**Exit Codes**:
- `0`: Successfully disconnected (or was not connected)

---

#### `server status`

**Syntax**: `server status [--json | -j]`

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `--json` | `-j` | flag | No | Output as JSON |

**Output (connected)**:
```
Server Connection
─────────────────
  Status:   Connected
  Server:   api.example.com
  Profile:  prod
  Since:    2 hours ago
```

**Output (disconnected)**:
```
Server Connection
─────────────────
  Status: Not connected
```

**Output (--json, connected)**:
```json
{
  "connected": true,
  "server": "api.example.com",
  "profile": "prod",
  "connectedAt": "2025-12-25T10:00:00Z"
}
```

**Output (--json, disconnected)**:
```json
{
  "connected": false
}
```

**Exit Codes**:
- `0`: Connected
- `1`: Not connected

---

### Server Profile Commands

#### `server profile list`

**Syntax**: `server profile list`

No arguments.

**Output (profiles exist)**:
```
Server Profiles
───────────────
  prod *      api.example.com
  staging     staging.example.com
  dev         localhost:5000

* = default profile
```

**Output (no profiles)**:
```
No server profiles saved.

Create one with:
  server profile add <name> --uri <uri>
```

**Exit Codes**:
- `0`: Success

---

#### `server profile add`

**Syntax**: `server profile add <name> --uri <uri> [--api-key | -k <key>] [--default] [--force | -f]`

| Argument | Alias | Type | Required | Autocomplete | Description |
|----------|-------|------|----------|--------------|-------------|
| `name` | - | positional | Yes | Profile names | Profile name (autocomplete for updates) |
| `--uri` | - | string | Yes | - | Server URI |
| `--api-key` | `-k` | string | No | - | API key (prompts if not provided) |
| `--default` | - | flag | No | - | Set as default profile |
| `--force` | `-f` | flag | No | - | Overwrite without confirmation |

**Behavior**:
1. If profile exists and `--force` not provided → prompt for confirmation
2. Prompt for API key if not provided via `--api-key`
3. Store profile in config file (cross-platform path)
4. Store credentials in OS credential store (fallback to encrypted file)
5. If `--default` → set as default profile

**Output Examples**:
```
# New profile
API Key: ********
✓ Profile 'prod' created

# New profile with --default
API Key: ********
✓ Profile 'prod' created (set as default)

# Overwrite prompt
⚠ Profile 'prod' already exists (api.example.com)
Overwrite? [y/N]: y
API Key: ********
✓ Profile 'prod' updated

# Overwrite with --force
API Key: ********
✓ Profile 'prod' updated
```

**Error Examples**:
```
# Missing URI
✗ --uri is required

# Invalid URI format
✗ Invalid URI format: not-a-valid-uri
```

**Exit Codes**:
- `0`: Profile created/updated
- `1`: Error (missing args, invalid URI)
- `2`: User cancelled overwrite

---

#### `server profile remove`

**Syntax**: `server profile remove <name>`

| Argument | Alias | Type | Required | Autocomplete | Description |
|----------|-------|------|----------|--------------|-------------|
| `name` | - | positional | Yes | Profile names | Profile name |

**Behavior**:
1. Remove profile from config file
2. Remove saved credentials from credential store
3. If was default → clear default

**Output Examples**:
```
# Success
✓ Profile 'prod' removed

# Not found
✗ Profile 'prod' not found
```

**Exit Codes**:
- `0`: Profile removed
- `1`: Profile not found

---

#### `server profile show`

**Syntax**: `server profile show <name>`

| Argument | Alias | Type | Required | Autocomplete | Description |
|----------|-------|------|----------|--------------|-------------|
| `name` | - | positional | Yes | Profile names | Profile name |

**Output**:
```
Profile: prod
─────────────
  URI:          api.example.com
  Credentials:  Saved
  Default:      Yes
```

**Error Examples**:
```
✗ Profile 'prod' not found
```

**Exit Codes**:
- `0`: Success
- `1`: Profile not found

---

#### `server profile set-default`

**Syntax**: `server profile set-default <name>`

| Argument | Alias | Type | Required | Autocomplete | Description |
|----------|-------|------|----------|--------------|-------------|
| `name` | - | positional | Yes | Profile names | Profile name |

**Output Examples**:
```
# Success
✓ Default profile set to 'prod'

# Not found
✗ Profile 'prod' not found
```

**Exit Codes**:
- `0`: Default set
- `1`: Profile not found

---

#### `server profile set-key`

**Syntax**: `server profile set-key <name> [--api-key | -k <key>]`

| Argument | Alias | Type | Required | Autocomplete | Description |
|----------|-------|------|----------|--------------|-------------|
| `name` | - | positional | Yes | Profile names | Profile name |
| `--api-key` | `-k` | string | No | - | New API key (prompts if not provided) |

**Output Examples**:
```
# Interactive
API Key: ********
✓ Credentials updated for 'prod'

# With argument
✓ Credentials updated for 'prod'

# Not found
✗ Profile 'prod' not found
```

**Exit Codes**:
- `0`: Credentials updated
- `1`: Profile not found

---

## Requirements *(mandatory)*

### Functional Requirements

#### Version Command

- **FR-001**: System MUST register a root-level command named `version` (no group)
- **FR-002**: System MUST NOT register an alias for the `version` command
- **FR-003**: The `version` command MUST accept an optional boolean argument `--full` with alias `-f`
- **FR-004**: When executed without `--full`, the command MUST output only the executing assembly version on a single line
- **FR-005**: When executed with `--full`, the command MUST output the executing assembly version on the first line, followed by each loaded BitPantry.CommandLine.* assembly name and version on subsequent lines
- **FR-006**: Version extraction MUST use `AssemblyInformationalVersionAttribute` when available, falling back to `AssemblyVersion`, then to `0.0.0`
- **FR-007**: Framework assembly discovery MUST use loaded assemblies filtered by name prefix `BitPantry.CommandLine`

#### ListCommands Removal

- **FR-008**: System MUST remove the `ListCommandsCommand` class and file
- **FR-009**: System MUST remove the `ListCommandsCommand` registration from `CommandLineApplicationBuilder`
- **FR-010**: System MUST update any tests that relied on `lc` as a built-in command to use `version` instead
- **FR-011**: System MUST remove or archive documentation for the `lc` command

#### Server Connection Commands

- **FR-012**: System MUST register a `server` command group in the SignalR Client package
- **FR-013**: The `server connect` command MUST accept an optional positional `uri` argument
- **FR-014**: The `server connect` command MUST accept an optional `--profile` argument with alias `-p`
- **FR-015**: The `server connect` command MUST accept an optional `--api-key` argument with alias `-k`
- **FR-016**: The `server connect` command MUST fail if both `uri` AND `--profile` are provided
- **FR-017**: The `server connect` command MUST fail with usage help if no connection target is available
- **FR-018**: The `server connect` command MUST automatically disconnect from existing connections before connecting to a new server
- **FR-018a**: The `server connect` command MUST accept an optional `--timeout` argument with alias `-t` specifying connection timeout in seconds
- **FR-018b**: The default connection timeout MUST be 10 seconds
- **FR-018c**: The `server connect` command MUST display a clear timeout error message if the connection does not complete within the timeout period
- **FR-019**: The `server connect` command MUST display success, warning, or error messages using consistent indicators (✓, ⚠, ✗)
- **FR-020**: The `server disconnect` command MUST gracefully handle being called when not connected
- **FR-021**: The `server status` command MUST accept an optional `--json` argument with alias `-j`
- **FR-022**: The `server status` command MUST return exit code `1` when not connected (for scripting)

#### Server Profile Commands

- **FR-023**: System MUST register a `server profile` command subgroup
- **FR-024**: Profile data MUST be stored in a cross-platform configuration directory using standard OS paths
- **FR-025**: API keys MUST be stored in the OS credential store when available, with fallback to an encrypted file
- **FR-026**: The `server profile add` command MUST prompt for confirmation when overwriting an existing profile (unless `--force`)
- **FR-027**: The `server profile add` command MUST prompt for API key if not provided via `--api-key`
- **FR-028**: The `server profile remove` command MUST also remove saved credentials from the credential store
- **FR-029**: The `server profile show` command MUST indicate whether credentials are saved without displaying them
- **FR-030**: Only one profile can be set as default at a time

#### Autocomplete

- **FR-046**: The `--profile` argument of `server connect` MUST provide autocomplete suggestions from saved profile names
- **FR-047**: The `<name>` argument of `server profile remove` MUST provide autocomplete suggestions from saved profile names
- **FR-048**: The `<name>` argument of `server profile show` MUST provide autocomplete suggestions from saved profile names
- **FR-049**: The `<name>` argument of `server profile set-default` MUST provide autocomplete suggestions from saved profile names
- **FR-050**: The `<name>` argument of `server profile set-key` MUST provide autocomplete suggestions from saved profile names
- **FR-051**: Autocomplete MUST filter suggestions based on the partial text the user has typed
- **FR-052**: Autocomplete MUST return an empty list when no profiles exist
- **FR-053**: The `<name>` argument of `server profile add` MUST provide autocomplete suggestions from saved profile names (for update scenarios)

#### Documentation

- **FR-054**: Documentation MUST include a "Built-in Commands" section organized by package
- **FR-055**: Each command's documentation MUST include: package name, syntax, arguments, behaviors, error cases, exit codes, and examples
- **FR-056**: The built-in commands index MUST clearly indicate which package provides each command
- **FR-057**: Documentation for `lc` command MUST be removed or archived
- **FR-058**: Documentation MUST be updated whenever command behavior changes

#### Prompt Segment Architecture

- **FR-031**: System MUST define an `IPromptSegment` interface in the core library
- **FR-032**: Each `IPromptSegment` MUST have an `Order` property for controlling position
- **FR-033**: Each `IPromptSegment` MUST have a `Render()` method that returns `string?` (null to skip)
- **FR-034**: System MUST define an `IPrompt` interface with a `Render()` method returning `string`
- **FR-035**: System MUST provide a default `CompositePrompt` implementation that aggregates all registered `IPromptSegment` instances
- **FR-036**: The `CompositePrompt` MUST order segments by their `Order` property
- **FR-037**: The `CompositePrompt` MUST skip segments that return null
- **FR-038**: The `CompositePrompt` MUST join non-null segments with a space separator and append `> `
- **FR-039**: Each segment MUST own its complete output including any decorators (e.g., `@`, `[]`)
- **FR-040**: The core library MUST register a default `AppNameSegment` with Order 0
- **FR-041**: The SignalR Client package MUST register a `ServerConnectionSegment` with Order 100
- **FR-042**: The SignalR Client package MUST register a `ProfileSegment` with Order 110
- **FR-043**: Users MUST be able to replace `IPrompt` entirely via the application builder
- **FR-044**: Segments MUST be resolved from DI container, allowing injection of dependencies
- **FR-045**: The existing `Prompt` class and manual prompt update pattern MUST be removed

---

### Key Entities

- **VersionCommand**: A root-level command that outputs version information. Has one optional boolean property `Full` with alias `f`.
- **ServerGroup**: Command group containing `connect`, `disconnect`, `status` commands.
- **ProfileGroup**: Command subgroup under `server` containing `list`, `add`, `remove`, `show`, `set-default`, `set-key` commands.
- **ServerProfile**: Stored configuration containing profile name, URI, and reference to stored credentials.
- **IPromptSegment**: Interface for contributing prompt content. Properties: `Order`, `Render()`.
- **IPrompt**: Interface for complete prompt rendering. Methods: `Render()`.
- **CompositePrompt**: Default implementation aggregating all registered segments.

---

### Package Ownership

Commands and components are distributed across packages based on their dependencies:

#### `BitPantry.CommandLine` (Core)

| Component | Type | Description |
|-----------|------|-------------|
| `version` | Command | Display version information |
| `IPromptSegment` | Interface | Segment contribution interface |
| `IPrompt` | Interface | Complete prompt interface |
| `CompositePrompt` | Class | Aggregates registered segments |
| `AppNameSegment` | Class | Base prompt segment (Order 0) |

#### `BitPantry.CommandLine.Remote.SignalR.Client`

| Component | Type | Description |
|-----------|------|-------------|
| `server connect` | Command | Connect to remote server |
| `server disconnect` | Command | Disconnect from server |
| `server status` | Command | Show connection status |
| `server profile list` | Command | List saved profiles |
| `server profile add` | Command | Create/update profile |
| `server profile remove` | Command | Delete profile |
| `server profile show` | Command | Display profile details |
| `server profile set-default` | Command | Set default profile |
| `server profile set-key` | Command | Update profile credentials |
| `ServerConnectionSegment` | Class | Prompt segment for hostname (Order 100) |
| `ProfileSegment` | Class | Prompt segment for profile name (Order 110) |
| `ProfileManager` | Class | CRUD operations for profiles.json |
| `CredentialStore` | Class | OS credential store / encrypted fallback |

---

### Profile Storage

**Configuration Path Resolution**:

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\BitPantry\CommandLine\` |
| Linux | `~/.config/bitpantry-commandline/` |
| macOS | `~/.config/bitpantry-commandline/` |

**File Structure**:
```
<config-dir>/
├── profiles.json       # Profile configurations
```

**profiles.json Schema**:
```json
{
  "defaultProfile": "production",
  "profiles": {
    "production": {
      "name": "production",
      "uri": "https://api.example.com"
    },
    "staging": {
      "name": "staging",
      "uri": "https://staging.example.com"
    }
  }
}
```

**Credential Storage**: OS credential store (Windows Credential Manager, macOS Keychain, Linux libsecret) with fallback to encrypted file.

**Encrypted File Fallback**:
- **Windows**: Use `System.Security.Cryptography.ProtectedData` (DPAPI) to encrypt credentials to the current user scope
- **Linux/macOS**: Use `libsodium` via the `Sodium.Core` NuGet package with a machine-derived key
- **File location**: `<config-dir>/credentials.enc`
- **Key derivation (non-Windows)**: Machine ID + user ID combined as entropy source

**Required NuGet Packages**:
- `System.Security.Cryptography.ProtectedData` (Windows DPAPI)
- `Sodium.Core` (cross-platform libsodium bindings)

---

### Prompt Segment Order Convention

| Order Range | Purpose | Package |
|-------------|---------|---------|
| 0-99 | Core (app name, base state) | Core |
| 100-199 | Connection state | SignalR Client |
| 200-299 | Session state | Future packages |
| 300+ | Custom/user segments | User code |

**Example Prompt Compositions**:

| State | Segments | Rendered Prompt |
|-------|----------|-----------------|
| Disconnected | `["myapp"]` | `myapp> ` |
| Connected ad-hoc | `["myapp", "@api.example.com"]` | `myapp @api.example.com> ` |
| Connected via profile | `["myapp", "@api.example.com", "[prod]"]` | `myapp @api.example.com [prod]> ` |

---

## Edge Cases

### Migration (Breaking Changes)

- **Existing `ConnectCommand.cs` and `DisconnectCommand.cs`** → Delete and replace with redesigned implementations. This is a breaking change.
- **Existing argument names** (e.g., `--confirmDisconnect`) → Not preserved. Clean break with new argument semantics.
- **Release notes** → Must document breaking changes to server connection commands with migration guidance.

### Version Command

- Executing assembly has neither `AssemblyInformationalVersion` nor `AssemblyVersion` → Display `0.0.0`
- No BitPantry.CommandLine.* assemblies loaded in `--full` mode → Display only executing assembly version
- `version --full --help` → Follow existing framework behavior for help with other args

### Server Connect

- Both `uri` AND `--profile` provided → Error with clear message
- Neither `uri` nor `--profile` nor default profile → Error with usage help
- Connection timeout → Display `✗ Connection to <server> timed out after <n> seconds` and exit with code 1
- Invalid timeout value (negative, zero, non-numeric) → Validation error before attempting connection
- Already connected to same server → Success message without reconnecting
- Already connected to different server → Disconnect first, then connect
- Server unreachable → Clear error message with server address
- Invalid API key → Clear authentication error message
- Invalid URI format → Validation error before attempting connection

### Server Profile

- Profile name contains special characters → Validation error: `✗ Invalid profile name '<name>'. Names may only contain letters, numbers, hyphens, and underscores.`
- Profile name validation regex → `^[a-zA-Z0-9_-]+$`
- Profile add with existing name → Prompt confirmation (unless `--force`)
- Profile remove when connected via that profile → Allow removal, connection remains active
- Credentials storage unavailable (no OS store, encrypted file fails) → Clear error with guidance
- Corrupted profiles.json → Rename to `profiles.json.bak`, create fresh empty file, display: `⚠ Profile configuration was corrupted. A backup was saved to profiles.json.bak`

### Prompt Segments

- Segment throws exception during render → Skip segment, log warning, continue with other segments
- No segments registered → Render `> ` (just the suffix)
- Multiple segments return same Order → Stable sort, preserve registration order
- Segment returns empty string (not null) → Include in output (allows intentional empty)

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can retrieve application version in under 1 second by running `version`
- **SC-002**: Users can retrieve complete framework version information by running `version --full`
- **SC-003**: The `lc` command is no longer recognized by the framework
- **SC-004**: All existing tests pass after migration from `lc` to `version` for conflict testing
- **SC-005**: Users can connect to a server using ad-hoc URI with `server connect <uri>`
- **SC-006**: Users can connect to a server using saved profile with `server connect --profile <name>`
- **SC-007**: Users can check connection status with `server status` and receive JSON output with `--json`
- **SC-008**: Users can create, list, and manage server profiles with the `server profile` subcommands
- **SC-009**: API keys are stored securely in OS credential store (or encrypted fallback)
- **SC-010**: REPL prompt automatically reflects connection state without commands manually updating it
- **SC-011**: Extension packages can contribute prompt segments without core library changes
- **SC-012**: Users can replace the entire prompt system with a custom implementation
- **SC-013**: Autocomplete suggests profile names for all commands that accept a profile name argument
- **SC-014**: Built-in command documentation exists for all commands, organized by package
- **SC-015**: 100% of acceptance scenarios have corresponding passing tests
