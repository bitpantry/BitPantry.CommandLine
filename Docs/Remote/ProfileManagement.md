# Profile Management

[← Back to Remote Built-in Commands](BuiltInCommands.md)

Connection profiles provide a convenient way to store and reuse server connection settings. Profiles are stored locally and credentials are kept in the system's secure credential store.

## Profile Commands Overview

| Command | Group | Description |
|---------|-------|-------------|
| `add` | `server profile` | Creates a new connection profile |
| `remove` | `server profile` | Removes an existing profile |
| `show` | `server profile` | Shows profile details |
| `list` | `server profile` | Lists all configured profiles |
| `set-default` | `server profile` | Sets the default profile |
| `set-key` | `server profile` | Updates the API key for a profile |

---

## Add Profile (`server profile add`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.ProfileAddCommand`

Creates a new connection profile with server connection settings.

### Syntax

```
server profile add <name> --uri|-u <server-uri> [--tokenrequestendpoint|-e <endpoint>] [--apikey|-k <key>]
```

### Arguments

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `<name>` | | `string` | Yes | Unique name for the profile |
| `--uri` | `-u` | `string` | Yes | The remote server URI |
| `--tokenrequestendpoint` | `-e` | `string` | No | Token request endpoint URL |
| `--apikey` | `-k` | `string` | No | API key for authentication |

### Behavior

1. **Validates name** - Ensures name is unique and valid
2. **Validates URI** - Ensures URI is properly formatted
3. **Stores profile** - Saves profile settings to local storage
4. **Stores credentials** - If API key provided, stores in secure credential store
5. **Sets default** - If this is the first profile, sets it as default

### Examples

#### Create Basic Profile

```
> server profile add production --uri https://server.example.com/cli
Profile 'production' created successfully.
```

#### Create Profile with Authentication

```
> server profile add staging -u https://staging.example.com/cli -e https://staging.example.com/cli-auth/token-request -k my-api-key
Profile 'staging' created successfully.
```

---

## Remove Profile (`server profile remove`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.ProfileRemoveCommand`

Removes an existing connection profile.

### Syntax

```
server profile remove <name>
```

### Arguments

| Argument | Type | Required | Description |
|----------|------|----------|-------------|
| `<name>` | `string` | Yes | Name of the profile to remove |

### Behavior

1. **Validates existence** - Ensures profile exists
2. **Removes credentials** - Removes API key from secure credential store
3. **Removes profile** - Deletes profile from local storage
4. **Updates default** - If removed profile was default, clears default setting

### Examples

```
> server profile remove staging
Profile 'staging' removed successfully.
```

If profile doesn't exist:

```
> server profile remove nonexistent
Profile 'nonexistent' not found.
```

---

## Show Profile (`server profile show`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.ProfileShowCommand`

Displays the details of a specific profile.

### Syntax

```
server profile show <name>
```

### Arguments

| Argument | Type | Required | Description |
|----------|------|----------|-------------|
| `<name>` | `string` | Yes | Name of the profile to show |

### Output

```
> server profile show production

Profile: production
───────────────────
  URI:                    https://server.example.com/cli
  Token Request Endpoint: https://server.example.com/cli-auth/token-request
  API Key:                ****configured****
  Is Default:             Yes
```

If profile has no API key configured:

```
> server profile show development

Profile: development
────────────────────
  URI:                    https://localhost:5001/cli
  Token Request Endpoint: (not configured)
  API Key:                (not configured)
  Is Default:             No
```

---

## List Profiles (`server profile list`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.ProfileListCommand`

Lists all configured connection profiles.

### Syntax

```
server profile list
```

### Output

```
> server profile list

Configured Profiles
───────────────────
  * production (default)
    staging
    development
```

If no profiles exist:

```
> server profile list
No profiles configured. Use 'server profile add' to create one.
```

---

## Set Default Profile (`server profile set-default`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.ProfileSetDefaultCommand`

Sets a profile as the default for connections.

### Syntax

```
server profile set-default <name>
```

### Arguments

| Argument | Type | Required | Description |
|----------|------|----------|-------------|
| `<name>` | `string` | Yes | Name of the profile to set as default |

### Behavior

The default profile is used when running `server connect` without specifying a profile or URI.

### Examples

```
> server profile set-default staging
Profile 'staging' set as default.
```

If profile doesn't exist:

```
> server profile set-default nonexistent
Profile 'nonexistent' not found.
```

---

## Set API Key (`server profile set-key`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.ProfileSetKeyCommand`

Updates or sets the API key for an existing profile.

### Syntax

```
server profile set-key <name> --apikey|-k <key>
```

### Arguments

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `<name>` | | `string` | Yes | Name of the profile |
| `--apikey` | `-k` | `string` | Yes | New API key value |

### Behavior

1. **Validates profile** - Ensures profile exists
2. **Updates credential** - Stores new API key in secure credential store
3. **Confirms update** - Displays success message

### Examples

```
> server profile set-key production --apikey new-secret-key
API key updated for profile 'production'.
```

---

## Profile Storage

### Profile Data Location

Profiles are stored in a JSON file at a platform-specific location:

| Platform | Location |
|----------|----------|
| Windows | `%APPDATA%\BitPantry.CommandLine\profiles.json` |
| macOS | `~/Library/Application Support/BitPantry.CommandLine/profiles.json` |
| Linux | `~/.config/BitPantry.CommandLine/profiles.json` |

### Credential Storage

API keys are stored in the system's secure credential store:

| Platform | Mechanism |
|----------|-----------|
| Windows | Windows Credential Manager |
| macOS | Keychain |
| Linux | Secret Service API / libsecret |

The profile JSON file only contains non-sensitive settings. Credentials are never stored in plain text.

### Profile JSON Structure

```json
{
  "profiles": {
    "production": {
      "uri": "https://server.example.com/cli",
      "tokenRequestEndpoint": "https://server.example.com/cli-auth/token-request"
    },
    "staging": {
      "uri": "https://staging.example.com/cli",
      "tokenRequestEndpoint": "https://staging.example.com/cli-auth/token-request"
    }
  },
  "defaultProfile": "production"
}
```

---

## Autocomplete Support

All profile commands that accept a profile name support tab completion:

```
> server profile show pro<TAB>
> server profile show production
```

Profile names are suggested based on configured profiles.

---

## See Also

- [Remote Built-in Commands](BuiltInCommands.md) - Connect, disconnect, and status commands
- [Client Configuration](Client.md) - Client setup options
- [ICredentialStore](../CommandLine/DependencyInjection.md) - Custom credential storage
- [Troubleshooting](Troubleshooting.md) - Common issues
