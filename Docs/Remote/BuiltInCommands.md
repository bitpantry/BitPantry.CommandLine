# Remote Built-in Commands

[← Back to Client Configuration](Client.md)

When the SignalR client is configured, these commands are automatically registered for managing remote server connections.

## Server Commands Overview

| Command | Group | Description |
|---------|-------|-------------|
| `connect` | `server` | Connects to a remote server |
| `disconnect` | `server` | Disconnects from the current server |
| `status` | `server` | Displays connection and profile status |

For profile management commands, see [Profile Management](ProfileManagement.md).

---

## Connect Command (`server connect`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.ConnectCommand`

Connects to a remote command line server using direct credentials or a saved profile.

### Syntax

```
server connect [<profile>] [--uri|-u <server-uri>] [--apikey|-k <key>] [--tokenrequestendpoint|-e <endpoint>]
```

### Arguments

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `<profile>` | | `string` | No | Profile name to load connection settings from |
| `--uri` | `-u` | `string` | No* | The remote server URI (e.g., `https://server.com/cli`) |
| `--apikey` | `-k` | `string` | No | API key for authentication |
| `--tokenrequestendpoint` | `-e` | `string` | No | Token request endpoint URL |

*Either a profile or `--uri` is required. If neither is provided, the default profile is used.

### Behavior

1. **Profile Loading** - If a profile name is specified, loads URI and credentials from the profile
2. **Default Profile** - If no URI or profile specified, uses the default profile
3. **Credential Resolution** - API key loaded from secure credential store if profile has one stored
4. **Validates URI** - Ensures a valid URI is available
5. **Attempts connection** - Connects to the remote server via SignalR
6. **Handles authentication** - If server returns 401 Unauthorized:
   - Uses provided/loaded credentials to request access token
   - If no credentials available, prompts for API key interactively
7. **Loads remote commands** - After successful connection, remote commands become available

### Examples

#### Connect Using a Profile

```
> server connect production
Connecting to server.example.com ...
Connected to server.example.com
```

#### Connect Using Default Profile

```
> server connect
Using default profile 'production'
Connecting to server.example.com ...
Connected to server.example.com
```

#### Connect with Direct URI

```
> server connect --uri https://localhost:5001/cli
Connecting to localhost:5001 ...
Connected to localhost:5001
```

#### Connect with Direct Credentials

```
> server connect -u https://server.example.com/cli -k my-api-key -e https://server.example.com/cli-auth/token-request
Connecting to server.example.com ...
Requesting access token ...
Connected to server.example.com
```

---

## Disconnect Command (`server disconnect`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.DisconnectCommand`

Disconnects from the current remote server.

### Syntax

```
server disconnect [--force|-f]
```

### Arguments

| Argument | Alias | Type | Description |
|----------|-------|------|-------------|
| `--force` | `-f` | `Switch` | Skip confirmation prompt |

### Behavior

1. **Checks connection status** - If not connected, displays a message
2. **Confirmation** - Prompts for confirmation (unless `--force` is set)
3. **Disconnects** - Terminates the SignalR connection
4. **Resets prompt** - Returns to local mode prompt

### Examples

#### Disconnect with Confirmation

```
> server disconnect
Disconnect from server.example.com? (y/n): y
Disconnected from server.example.com
```

#### Force Disconnect

```
> server disconnect --force
Disconnected from server.example.com
```

If not connected:

```
> server disconnect
Not connected to a server.
```

---

## Status Command (`server status`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.StatusCommand`

Displays the current connection status and profile information.

### Syntax

```
server status [--verbose|-v]
```

### Arguments

| Argument | Alias | Type | Description |
|----------|-------|------|-------------|
| `--verbose` | `-v` | `Switch` | Show detailed profile information |

### Output

#### Basic Output

```
> server status

Connection Status
─────────────────
  Status:  Connected
  Server:  server.example.com

Profile Status
──────────────
  Profiles:  3
  Default:   production
```

#### Verbose Output

```
> server status --verbose

Connection Status
─────────────────
  Status:  Connected
  Server:  server.example.com
  Port:    443
  Scheme:  https

Profile Status
──────────────
  Profiles:  3
  Default:   production

Configured Profiles
───────────────────
  * production (default)
    staging
    development
```

#### Disconnected Status

```
> server status

Connection Status
─────────────────
  Status:  Disconnected

Profile Status
──────────────
  Profiles:  3
  Default:   production
```

---

## After Connecting

Once connected to a remote server:

1. **Remote commands are available** - The `--help` flag lists all commands (local and remote)
2. **Prompt changes** - Shows the connected server name
3. **Commands execute remotely** - Remote commands run on the server

```
server.example.com> --help
# Shows both local and remote commands

server.example.com> remote-command --arg value
# Executes on the remote server
```

## See Also

- [Profile Management](ProfileManagement.md) - Manage saved connection profiles
- [Client Configuration](Client.md) - Client configuration
- [SignalRClientOptions](SignalRClientOptions.md) - Client options
- [CommandLineServer](CommandLineServer.md) - Server setup
- [Troubleshooting](Troubleshooting.md) - Connection issues
- [Built-in Commands](../CommandLine/BuiltInCommands.md) - Core built-in commands
