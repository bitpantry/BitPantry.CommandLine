# Feature Specification: Server Profile Management

**Feature Branch**: `009-server-profile`  
**Created**: 2026-02-02  
**Status**: Draft  
**Input**: User description: "Server profile management system for storing and reusing remote server connection settings with secure credential storage"

---

## Overview

Users need a way to save, manage, and reuse remote server connection settings. Currently, connecting to a server requires providing the URI and API key each time. This feature enables users to save connection "profiles" with a friendly name, store credentials securely using OS-native mechanisms, and connect quickly by referencing the profile name.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Use a Connection Profile (Priority: P1) 🎯 MVP

A user wants to save a server connection so they don't have to retype the URI and API key every time. They create a named profile, then use that profile name to connect.

**Why this priority**: This is the core value proposition - saving time and reducing errors when connecting to frequently-used servers.

**Independent Test**: Can be fully tested by creating a profile with `server profile add`, then verifying it can be used for connection. Delivers immediate value by eliminating repetitive credential entry.

**Acceptance Scenarios**:

1. **Given** no profiles exist, **When** user runs `server profile add prod https://api.example.com`, **Then** system prompts for API key and saves the profile
2. **Given** a profile "prod" exists, **When** user runs `server connect --profile prod`, **Then** system connects using stored settings
3. **Given** user provides `--apikey` argument, **When** creating profile, **Then** system does not prompt for API key
4. **Given** profile name contains invalid characters, **When** user attempts to create it, **Then** system displays validation error

---

### User Story 2 - List and View Saved Profiles (Priority: P1)

A user wants to see all their saved profiles and view details of a specific profile to verify settings.

**Why this priority**: Essential for profile discoverability and verification before connecting.

**Independent Test**: Can be tested by creating profiles then running `server profile list` and `server profile show <name>`.

**Acceptance Scenarios**:

1. **Given** multiple profiles exist, **When** user runs `server profile list`, **Then** all profiles display with default indicator
2. **Given** no profiles exist, **When** user runs `server profile list`, **Then** helpful message with usage hint displays
3. **Given** a profile exists, **When** user runs `server profile show prod`, **Then** profile details display (URI host, credential status, default status)
4. **Given** profile does not exist, **When** user runs `server profile show unknown`, **Then** error message displays

---

### User Story 3 - Set Default Profile (Priority: P2)

A user wants to designate one profile as the default so they can connect without specifying a profile name.

**Why this priority**: Convenience feature that enhances the core connection flow.

**Independent Test**: Can be tested by setting default with `server profile set-default` and verifying `server connect` uses it.

**Acceptance Scenarios**:

1. **Given** a profile "prod" exists, **When** user runs `server profile set-default prod`, **Then** profile is marked as default
2. **Given** "prod" is default, **When** user runs `server connect` without `--profile`, **Then** system connects using "prod" settings
3. **Given** profile does not exist, **When** user runs `server profile set-default unknown`, **Then** error message displays

---

### User Story 4 - Update Profile Credentials (Priority: P2)

A user's API key has been rotated and they need to update the stored credentials without recreating the profile.

**Why this priority**: Important for security key rotation workflows.

**Independent Test**: Can be tested by updating credentials with `server profile set-key` and verifying new key is used for connection.

**Acceptance Scenarios**:

1. **Given** a profile exists, **When** user runs `server profile set-key prod --apikey newkey`, **Then** credential is updated
2. **Given** API key not provided, **When** running `server profile set-key prod`, **Then** system prompts for key securely (masked input)
3. **Given** profile does not exist, **When** user runs `server profile set-key unknown`, **Then** error message displays

---

### User Story 5 - Remove Profile (Priority: P2)

A user wants to delete a saved profile they no longer need, including its stored credentials.

**Why this priority**: Cleanup and security - users should be able to remove stale/compromised credentials.

**Independent Test**: Can be tested by removing profile with `server profile remove` and verifying it no longer appears in list.

**Acceptance Scenarios**:

1. **Given** a profile "staging" exists, **When** user runs `server profile remove staging`, **Then** profile and credentials are deleted
2. **Given** deleted profile was the default, **When** profile is removed, **Then** default setting is cleared
3. **Given** profile does not exist, **When** user runs `server profile remove unknown`, **Then** error message displays

---

### User Story 6 - Autocomplete Profile Names (Priority: P3)

A user wants tab-completion for profile names to avoid typos and speed up command entry.

**Why this priority**: Quality-of-life enhancement that improves usability.

**Independent Test**: Can be tested by pressing TAB after typing partial profile name and verifying suggestions appear.

**Acceptance Scenarios**:

1. **Given** profiles "prod" and "staging" exist, **When** user types `server profile show p<TAB>`, **Then** "prod" is suggested
2. **Given** autocomplete is triggered, **When** suggestions display, **Then** default profile is shown first
3. **Given** no profiles match prefix, **When** user presses TAB, **Then** no suggestions appear

---

### User Story 7 - Prompt Segment Display (Priority: P3)

When connected via a profile, the command prompt should show the profile name so users know which server they're connected to.

**Why this priority**: Visual feedback enhancement for connection awareness.

**Independent Test**: Can be tested by connecting with a profile and verifying prompt shows `[profile-name]`.

**Acceptance Scenarios**:

1. **Given** connected via profile "prod", **When** viewing prompt, **Then** prompt includes `[prod]`
2. **Given** not connected, **When** viewing prompt, **Then** no profile segment appears
3. **Given** connected without profile (direct URI), **When** viewing prompt, **Then** no profile segment appears

---

### Edge Cases

- What happens when profile name exceeds 64 characters? → Validation error
- What happens when JSON configuration file is corrupted? → Backup created, file reset, warning displayed
- What happens when credential store fails (permissions, keychain locked)? → `CredentialStoreException` with meaningful message
- What happens when profile file doesn't exist yet? → Created automatically on first save
- What happens when two profiles have same name with different casing? → Names are case-insensitive; treated as same profile
- What happens when libsodium is unavailable on Linux/macOS? → Error: "Credential storage requires libsodium. Install via: [package manager instructions]"
- What happens when `server connect` has both `--profile` and `--uri`? → Explicit `--uri` overrides profile's URI; credentials still from profile

---

## Command Syntax Reference

### Command Group Structure

```
server                          # Root group for server commands
  └── profile                   # Nested group for profile management
        ├── add                 # Create/update profile
        ├── list                # List all profiles
        ├── show                # Show profile details
        ├── remove              # Delete profile
        ├── set-default         # Set default profile
        └── set-key             # Update API key
```

### `server profile add`

Creates a new connection profile or updates an existing one.

**Syntax**:
```
server profile add <name> <uri> [--apikey|-k <key>] [--default] [--force|-f]
```

**Arguments**:

| Argument | Position | Required | Type | Alias | Autocomplete | Description |
|----------|----------|----------|------|-------|--------------|-------------|
| `name` | 0 | Yes | string | - | - | Unique profile name (alphanumeric, hyphen, underscore; max 64 chars) |
| `uri` | 1 | Yes | string | - | - | Server URI (must be valid http/https URL) |
| `--apikey` | - | No | string | `-k` | - | API key (prompts if not provided) |
| `--default` | - | No | flag | - | - | Set this profile as default |
| `--force` | - | No | flag | `-f` | - | Overwrite existing profile without confirmation |

**Validation**:
- Profile name must match pattern `^[a-zA-Z0-9_-]+$`
- Profile name must be ≤64 characters
- URI must be valid absolute http or https URL
- If profile exists and `--force` not provided, prompt for confirmation

**Behavior**:
- If API key not provided via argument, prompt with masked/secret input
- If `--default` specified, set as default after creation
- Store credentials in secure OS credential store
- Set `HasCredentials = true` on profile after storing key

---

### `server profile list`

Lists all saved server profiles.

**Syntax**:
```
server profile list
```

**Arguments**: None

**Output Format**:
```
Server Profiles

  prod *      api.example.com
  staging     staging.example.com

* = default profile
```

**Behavior**:
- If no profiles exist, display: "No server profiles saved." with usage hint
- Order profiles alphabetically by name
- Mark default profile with `*` suffix
- Show hostname portion of URI (not full URI)

---

### `server profile show`

Displays details of a specific profile.

**Syntax**:
```
server profile show <name>
```

**Arguments**:

| Argument | Position | Required | Type | Autocomplete | Description |
|----------|----------|----------|------|--------------|-------------|
| `name` | 0 | Yes | string | `ProfileNameProvider` | Profile name to display |

**Output Format**:
```
Profile: prod

  URI:          api.example.com
  Credentials:  Saved
  Default:      Yes
```

**Validation**:
- Profile must exist; display error if not found

---

### `server profile remove`

Deletes a saved server profile and its credentials.

**Syntax**:
```
server profile remove <name>
```

**Arguments**:

| Argument | Position | Required | Type | Autocomplete | Description |
|----------|----------|----------|------|--------------|-------------|
| `name` | 0 | Yes | string | `ProfileNameProvider` | Profile name to remove |

**Behavior**:
- Remove profile from configuration file
- Remove credentials from secure store
- If removed profile was default, clear default setting
- Display success message with profile name
- If profile not found, display error

---

### `server profile set-default`

Sets a profile as the default for connections.

**Syntax**:
```
server profile set-default <name>
```

**Arguments**:

| Argument | Position | Required | Type | Autocomplete | Description |
|----------|----------|----------|------|--------------|-------------|
| `name` | 0 | Yes | string | `ProfileNameProvider` | Profile name to set as default |

**Validation**:
- Profile must exist; display error if not found

---

### `server profile set-key`

Updates the API key for an existing profile.

**Syntax**:
```
server profile set-key <name> [--apikey|-k <key>]
```

**Arguments**:

| Argument | Position | Required | Type | Alias | Autocomplete | Description |
|----------|----------|----------|------|-------|--------------|-------------|
| `name` | 0 | Yes | string | - | `ProfileNameProvider` | Profile name to update |
| `--apikey` | - | No | string | `-k` | - | New API key (prompts if not provided) |

**Validation**:
- Profile must exist; display error if not found
- API key cannot be empty/whitespace

**Behavior**:
- If API key not provided, prompt with masked/secret input
- Store new credentials in secure OS credential store
- Update profile's `HasCredentials = true`

---

## Requirements *(mandatory)*

### Functional Requirements

#### Profile Management
- **FR-001**: System MUST allow users to create named connection profiles with server URI
- **FR-002**: System MUST validate profile names contain only alphanumeric, hyphen, and underscore characters
- **FR-003**: System MUST limit profile names to 64 characters maximum
- **FR-004**: System MUST validate URIs are absolute http or https URLs
- **FR-005**: System MUST support updating existing profiles
- **FR-006**: System MUST prompt for confirmation when overwriting profiles unless `--force` specified
- **FR-007**: System MUST allow deletion of profiles by name
- **FR-008**: System MUST clear default setting when default profile is deleted

#### Default Profile
- **FR-009**: System MUST allow setting one profile as default
- **FR-010**: System MUST use default profile when `server connect` is run without `--profile` argument, using both stored URI and credentials
- **FR-010a**: System MUST allow `server connect --profile <name>` to connect using only the profile (no `--uri` required)
- **FR-010b**: System MUST allow explicit `--uri` to override profile's stored URI while still using profile's credentials
- **FR-011**: System MUST indicate default profile in listing with visible marker

#### Credential Storage
- **FR-012**: System MUST store API keys in OS-native secure credential store
- **FR-013**: System MUST use DPAPI for credential encryption on Windows
- **FR-014**: System MUST use libsodium with machine-derived key for encryption on Linux/macOS
- **FR-015**: System MUST remove credentials from store when profile is deleted
- **FR-016**: System MUST prompt for API key with masked input when not provided as argument
- **FR-017**: System MUST never store credentials in plain text in configuration files
- **FR-017a**: System MUST fail with actionable error if libsodium is unavailable on Linux/macOS (no fallback to weaker encryption)
- **FR-017b**: System MUST log profile operations at Debug level only (operation, profile name, success/failure)
- **FR-017c**: System MUST never log credentials or API keys at any log level

#### Profile Storage
- **FR-018**: System MUST store profile configuration in JSON format
- **FR-019**: System MUST store profiles in platform-appropriate configuration directory:
  - Windows: `%APPDATA%\BitPantry\CommandLine\profiles.json`
  - Linux/macOS: `~/.config/bitpantry-commandline/profiles.json` (respecting `XDG_CONFIG_HOME`)
- **FR-020**: System MUST create configuration directory if it doesn't exist
- **FR-021**: System MUST handle corrupted configuration by creating backup and resetting
- **FR-022**: System MUST treat profile names as case-insensitive

#### Autocomplete
- **FR-023**: System MUST provide autocomplete suggestions for profile names in applicable commands
- **FR-024**: System MUST show default profile first in autocomplete suggestions
- **FR-025**: System MUST filter suggestions by typed prefix (case-insensitive)

#### Prompt Integration
- **FR-026**: System MUST display profile name in prompt when connected via profile
- **FR-027**: System MUST display profile segment in format `[profile-name]`
- **FR-028**: System MUST hide profile segment when not connected or connected without profile

---

### Key Entities

#### ServerProfile
Represents a saved connection profile.

| Attribute | Type | Description |
|-----------|------|-------------|
| Name | string | Unique identifier (alphanumeric, hyphen, underscore; max 64 chars) |
| Uri | string | Server URI (http/https) |
| HasCredentials | bool | Whether credentials are stored for this profile |
| CreatedAt | DateTimeOffset | Timestamp when profile was created |
| ModifiedAt | DateTimeOffset | Timestamp when profile was last modified |

#### ProfileConfiguration
Root storage object for profiles.json.

| Attribute | Type | Description |
|-----------|------|-------------|
| DefaultProfile | string? | Name of default profile, or null |
| Profiles | Dictionary&lt;string, ServerProfile&gt; | Profiles keyed by name (case-insensitive) |

---

### Secure Credential Storage by Platform

#### Windows
- **Mechanism**: Data Protection API (DPAPI)
- **Scope**: CurrentUser (tied to logged-in Windows user)
- **Storage**: Encrypted credentials file at `%APPDATA%\BitPantry\CommandLine\credentials.enc`

#### Linux/macOS
- **Mechanism**: libsodium SecretBox encryption
- **Key Derivation**: 32-byte key from machine ID + username via GenericHash
- **Machine ID**: `/etc/machine-id` on Linux, `Environment.MachineName` as fallback
- **Storage**: Encrypted credentials file at `~/.config/bitpantry-commandline/credentials.enc`

#### Credential File Format
JSON dictionary mapping profile names (lowercase) to Base64-encoded encrypted API keys.

#### Portability
Credentials are machine-bound by design. When migrating to a new machine, users must re-enter API keys for each profile. Profile metadata (names, URIs) can be copied via `profiles.json`, but `credentials.enc` is not portable.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a profile and connect using it within 30 seconds
- **SC-002**: Users can list 100+ profiles without noticeable delay
- **SC-003**: Profile autocomplete suggestions appear instantly (< 100ms perceived)
- **SC-004**: 100% of stored credentials are encrypted at rest (never plain text)
- **SC-005**: Profile commands work identically across Windows, Linux, and macOS
- **SC-006**: Users receive clear error messages for all validation failures
- **SC-007**: Corrupted configuration files are automatically recovered without data loss for intact profiles

---

## Assumptions

1. The command-line framework already supports nested command groups via `[Group]` attribute
2. The autocomplete infrastructure supports custom `ICompletionProvider` implementations
3. The prompt segment system supports custom `IPromptSegment` implementations
4. The `IServerProxy` interface exposes connection state for prompt segment visibility
5. libsodium (Sodium.Core NuGet package) is available for cross-platform encryption
6. The `server connect` command will be updated to accept `--profile` argument (separate concern)

---

## Clarifications

### Session 2026-02-02

- Q: What happens during profile migration to a new machine? → A: Credentials are machine-bound; migrating profiles requires re-entering API keys on new device (standard practice for CLI tools storing keys)
- Q: What happens when libsodium is unavailable on Linux/macOS? → A: Fail with clear error message; require libsodium for credential storage (no fallback to weaker encryption)
- Q: Does `--profile` alone suffice for connection, or is `--uri` also required? → A: Profile stores both URI and credentials; `--profile` alone is sufficient to connect
- Q: What happens when both `--profile` and `--uri` are provided? → A: Explicit `--uri` overrides profile's stored URI; credentials still used from profile
- Q: Should profile operations be logged for debugging? → A: Log at Debug level only (operation name, profile name, success/failure); never log credentials
