# Remote Built-in Commands

[← Back to Client Configuration](Client.md)

When the SignalR client is configured, these commands are automatically registered for managing remote server connections and file operations.

## Command Groups Overview

### Server Commands

| Command | Group | Description |
|---------|-------|-------------|
| `connect` | `server` | Connects to a remote server |
| `disconnect` | `server` | Disconnects from the current server |
| `status` | `server` | Displays connection and profile status |

### File Commands

| Command | Group | Description |
|---------|-------|-------------|
| `ls` | `file` | Lists files and directories on the remote server |
| `upload` | `file` | Uploads a local file to the remote server |
| `download` | `file` | Downloads a file from the remote server |
| `rm` | `file` | Removes a file or directory from the remote server |
| `mkdir` | `file` | Creates a directory on the remote server |
| `cat` | `file` | Displays the contents of a remote file |
| `info` | `file` | Shows detailed metadata for a file or directory |

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

## File Commands

File commands allow you to manage files on the remote server. These commands are available when connected to a remote server that has file system access enabled.

> **Note**: All file operations are sandboxed to the server's configured storage root directory. Path traversal attempts (e.g., `../`) are rejected for security.

---

## List Files (`file ls`)

`BitPantry.CommandLine.Remote.SignalR.Server.Commands.File.FileListCommand`

Lists files and directories on the remote server.

### Syntax

```
file ls [<path>] [--long|-l] [--recursive|-r]
```

### Arguments

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `<path>` | | `string` | No | Path to list (defaults to storage root) |
| `--long` | `-l` | `Switch` | No | Show detailed information (type, size, date) |
| `--recursive` | `-r` | `Switch` | No | List all files recursively |

### Examples

#### List Storage Root

```
server.example.com> file ls
documents/
reports/
config.json
data.csv
```

#### List Subdirectory

```
server.example.com> file ls reports/
2024/
2025/
summary.xlsx
```

#### Long Format

```
server.example.com> file ls --long
[DIR]   2025-01-15 14:30  -         documents/
[DIR]   2025-01-10 09:15  -         reports/
[FILE]  2025-01-02 08:00  1.2 KB    config.json
[FILE]  2025-01-01 12:00  45.6 MB   data.csv
```

#### Recursive Listing

```
server.example.com> file ls --recursive
documents/
documents/readme.txt
reports/
reports/2024/
reports/2024/q1.xlsx
reports/2025/
config.json
```

---

## Upload File (`file upload`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.File.FileUploadCommand`

Uploads a local file to the remote server with progress display and checksum verification.

### Syntax

```
file upload <local-path> [<remote-path>] [--force|-f]
```

### Arguments

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `<local-path>` | | `string` | Yes | Path to local file to upload |
| `<remote-path>` | | `string` | No | Destination path on server (defaults to filename) |
| `--force` | `-f` | `Switch` | No | Overwrite existing file without confirmation |

### Behavior

1. **Validates local file** - Ensures the local file exists
2. **Checks connection** - Verifies connection to server
3. **Uploads with progress** - Shows progress bar during transfer
4. **Checksum verification** - Validates file integrity after upload
5. **Displays confirmation** - Shows success message with file details

### Examples

#### Upload to Storage Root

```
server.example.com> file upload ./data.csv
Uploading data.csv ...
[████████████████████████████████] 100%  45.6 MB/45.6 MB
✓ Upload complete - checksum verified
```

#### Upload to Subdirectory

```
server.example.com> file upload ./report.xlsx reports/2025/q1.xlsx
Uploading report.xlsx ...
[████████████████████████████████] 100%  2.1 MB/2.1 MB
✓ Upload complete - checksum verified
```

#### Force Overwrite

```
server.example.com> file upload ./config.json --force
Uploading config.json ...
[████████████████████████████████] 100%  1.2 KB/1.2 KB
✓ Upload complete - checksum verified
```

---

## Download File (`file download`)

`BitPantry.CommandLine.Remote.SignalR.Client.Commands.File.FileDownloadCommand`

Downloads a file from the remote server with progress display and checksum verification.

### Syntax

```
file download <remote-path> [<local-path>] [--force|-f]
```

### Arguments

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `<remote-path>` | | `string` | Yes | Path to file on server |
| `<local-path>` | | `string` | No | Local destination path (defaults to current directory + filename) |
| `--force` | `-f` | `Switch` | No | Overwrite existing local file without confirmation |

### Behavior

1. **Checks connection** - Verifies connection to server
2. **Downloads with progress** - Shows progress bar during transfer
3. **Creates directories** - Creates local directories as needed
4. **Checksum verification** - Validates file integrity after download
5. **Displays confirmation** - Shows success message with file details

### Examples

#### Download to Current Directory

```
server.example.com> file download config.json
Downloading config.json ...
[████████████████████████████████] 100%  1.2 KB/1.2 KB
✓ Download complete - checksum verified
```

#### Download to Specific Path

```
server.example.com> file download reports/summary.xlsx ./local/reports/summary.xlsx
Downloading summary.xlsx ...
[████████████████████████████████] 100%  15.3 MB/15.3 MB
✓ Download complete - checksum verified
```

---

## Remove File (`file rm`)

`BitPantry.CommandLine.Remote.SignalR.Server.Commands.File.FileRemoveCommand`

Removes a file or directory from the remote server.

### Syntax

```
file rm <path> [--recursive|-r] [--force|-f]
```

### Arguments

| Argument | Alias | Type | Required | Description |
|----------|-------|------|----------|-------------|
| `<path>` | | `string` | Yes | Path to file or directory to remove |
| `--recursive` | `-r` | `Switch` | No | Remove directories and their contents recursively |
| `--force` | `-f` | `Switch` | No | Required - confirms deletion intent |

> **Note**: The `--force` flag is required for remote file removal operations. Interactive confirmation prompts are not supported over remote connections.

### Behavior

1. **Validates path** - Ensures the path exists
2. **Checks type** - Determines if path is file or directory
3. **Removes item** - Deletes file or directory (recursive if specified)
4. **Displays confirmation** - Shows success message

### Examples

#### Remove File

```
server.example.com> file rm temp.txt --force
Removed: temp.txt
```

#### Remove Empty Directory

```
server.example.com> file rm old-reports/ --force
Removed: old-reports/
```

#### Remove Directory Tree

```
server.example.com> file rm archive/ --recursive --force
Removed: archive/ (12 items deleted)
```

#### Error: Non-Empty Directory Without Recursive

```
server.example.com> file rm reports/ --force
Error: Directory is not empty. Use --recursive to remove directory and contents.
```

---

## Create Directory (`file mkdir`)

`BitPantry.CommandLine.Remote.SignalR.Server.Commands.File.FileMkdirCommand`

Creates a directory on the remote server.

### Syntax

```
file mkdir <path>
```

### Arguments

| Argument | Type | Required | Description |
|----------|------|----------|-------------|
| `<path>` | `string` | Yes | Path of directory to create |

### Behavior

1. **Creates directory** - Creates the directory and any missing parent directories
2. **Idempotent** - Succeeds silently if directory already exists
3. **Displays confirmation** - Shows success message

### Examples

#### Create Single Directory

```
server.example.com> file mkdir reports
Created: reports/
```

#### Create Nested Directories

```
server.example.com> file mkdir reports/2025/q1
Created: reports/2025/q1/
```

#### Create Existing Directory (Idempotent)

```
server.example.com> file mkdir reports
Created: reports/
```

---

## View File Contents (`file cat`)

`BitPantry.CommandLine.Remote.SignalR.Server.Commands.File.FileCatCommand`

Displays the contents of a file on the remote server.

### Syntax

```
file cat <path>
```

### Arguments

| Argument | Type | Required | Description |
|----------|------|----------|-------------|
| `<path>` | `string` | Yes | Path to file to display |

### Behavior

1. **Validates file** - Ensures the file exists
2. **Detects binary** - Warns if file appears to be binary (contains null bytes)
3. **Truncates large files** - Files over 1 MB are truncated with a warning
4. **Displays content** - Outputs file content to console

### Examples

#### Display Text File

```
server.example.com> file cat config.json
{
  "setting1": "value1",
  "setting2": "value2"
}
```

#### Large File Truncation

```
server.example.com> file cat largefile.log
[First 1 MB of file displayed]
...
⚠ Output truncated. File size: 15.3 MB (showing first 1 MB)
```

---

## File Information (`file info`)

`BitPantry.CommandLine.Remote.SignalR.Server.Commands.File.FileInfoCommand`

Displays detailed metadata for a file or directory on the remote server.

### Syntax

```
file info <path>
```

### Arguments

| Argument | Type | Required | Description |
|----------|------|----------|-------------|
| `<path>` | `string` | Yes | Path to file or directory |

### Output

Displays the following metadata:

- **Path**: Relative path within storage root
- **Type**: File or Directory
- **Size**: File size in human-readable format (files only)
- **Created**: Creation date and time
- **Modified**: Last modification date and time

### Examples

#### File Information

```
server.example.com> file info data.csv

Path:      data.csv
Type:      File
Size:      45.6 MB
Created:   2025-01-01 12:00:00
Modified:  2025-01-02 08:30:15
```

#### Directory Information

```
server.example.com> file info reports/

Path:      reports/
Type:      Directory
Created:   2024-06-15 10:00:00
Modified:  2025-01-15 14:30:00
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
- [File System Configuration](FileSystemConfiguration.md) - Configure server storage root and file system options
- [File System](FileSystem.md) - File system abstraction and sandboxing details
- [Troubleshooting](Troubleshooting.md) - Connection issues
- [Built-in Commands](../CommandLine/BuiltInCommands.md) - Core built-in commands
