# Remote Built-in Commands

[← Back to Client Configuration](Client.md)

When the SignalR client is configured, these commands are automatically registered for managing remote server connections.

## Connect Command (`server.connect`)

`BitPantry.CommandLine.Remote.SignalR.Client.ConnectCommand`

Connects to a remote command line server.

### Syntax

```
server.connect --uri|-u <server-uri> [--apikey|-k <key>] [--tokenrequestendpoint|-e <endpoint>] [--force|-f]
```

### Arguments

| Argument | Alias | Required | Description |
|----------|-------|----------|-------------|
| `--uri` | `-u` | Yes | The remote server URI (e.g., `https://server.com/cli`) |
| `--apikey` | `-k` | No* | API key for authentication |
| `--tokenrequestendpoint` | `-e` | No* | Token request endpoint URL |
| `--force` | `-f` | No | Force disconnect of any existing connection without confirmation |

*If authentication is required, both `--apikey` and `--tokenrequestendpoint` must be provided together.

### Behavior

1. **Validates URI** - Ensures a valid URI is provided
2. **Checks existing connection** - If already connected, prompts to disconnect (unless `--force` is set)
3. **Attempts connection** - Connects to the remote server
4. **Handles authentication** - If server returns 401 Unauthorized:
   - If credentials provided, requests access token
   - If not provided, prompts for API key interactively
5. **Loads remote commands** - After successful connection, remote commands become available

### Examples

#### Basic Connection (No Authentication)

```
> server.connect --uri https://localhost:5001/cli
Connecting to localhost:5001 ...
Connected to localhost:5001
```

#### Connection with Authentication

```
> server.connect -u https://server.example.com/cli -k my-api-key -e https://server.example.com/cli-auth/token-request
Connecting to server.example.com ...
Requesting access token ...
Connected to server.example.com
```

#### Interactive Authentication

If authentication is required but credentials aren't provided, you'll be prompted:

```
> server.connect --uri https://server.example.com/cli
Connecting to server.example.com ...
Server returned unauthorized response
Enter API Key: ********
Requesting access token ...
Connected to server.example.com
```

#### Force Reconnect

Skip the disconnect confirmation when already connected:

```
> server.connect -u https://other-server.com/cli -d
Disconnecting from server.example.com ...
Connecting to other-server.com ...
Connected to other-server.com
```

---

## Disconnect Command (`server.disconnect`)

`BitPantry.CommandLine.Remote.SignalR.Client.DisconnectCommand`

Disconnects from the current remote server.

### Syntax

```
server.disconnect
```

### Arguments

None.

### Behavior

1. **Checks connection status** - If not connected, displays a message
2. **Disconnects** - Terminates the SignalR connection
3. **Resets prompt** - Returns to local mode prompt

### Examples

```
> server.disconnect
Disconnecting from server.example.com ...
```

If not connected:

```
> server.disconnect
No active connection
```

---

## After Connecting

Once connected to a remote server:

1. **Remote commands are available** - Use `lc` to list all commands (local and remote)
2. **Prompt changes** - Shows the connected server name
3. **Commands execute remotely** - Remote commands run on the server

```
server.example.com> lc
# Shows both local and remote commands

server.example.com> remote-command --arg value
# Executes on the remote server
```

## See Also

- [Client](Client.md) - Client configuration
- [SignalRClientOptions](SignalRClientOptions.md) - Client options
- [CommandLineServer](CommandLineServer.md) - Server setup
- [Troubleshooting](Troubleshooting.md) - Connection issues
- [Built-in Commands](../CommandLine/BuiltInCommands.md) - Local built-in commands
