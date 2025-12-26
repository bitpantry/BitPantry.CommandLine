# Quickstart: Core CLI Commands & Prompt Redesign

**Feature**: 006-core-commands  
**Date**: 2025-12-26

This guide demonstrates how to use the new built-in commands and prompt system.

---

## 1. Version Command

### Basic Usage

```bash
# Display application version
> version
1.2.3

# Display full version information including framework assemblies
> version --full
1.2.3
BitPantry.CommandLine 2.0.0
BitPantry.CommandLine.Remote.SignalR 2.0.0
BitPantry.CommandLine.Remote.SignalR.Client 2.0.0

# Using alias
> version -f
```

### In Your Application

The `version` command is automatically registered when using `CommandLineApplicationBuilder`:

```csharp
var app = new CommandLineApplicationBuilder()
    .RegisterCommands()
    .Build();

// version command is available automatically
await app.Run("version");
```

---

## 2. Server Connection Commands

### Connecting to a Server

```bash
# Connect with URI
> server connect https://api.example.com
Connecting to api.example.com...
✓ Connected to api.example.com

# Connect with API key
> server connect https://api.example.com --api-key myapikey
Connecting to api.example.com...
✓ Connected to api.example.com

# Connect using a saved profile
> server connect --profile prod
Connecting to api.example.com...
✓ Connected to api.example.com

# Connect with custom timeout (default: 10 seconds)
> server connect https://slow-server.com --timeout 30
```

### Checking Connection Status

```bash
# Human-readable status
> server status
Server Connection
─────────────────
  Status:   Connected
  Server:   api.example.com
  Profile:  prod
  Since:    2 hours ago

# JSON output (for scripting)
> server status --json
{"connected":true,"server":"api.example.com","profile":"prod","connectedAt":"2025-12-26T10:00:00Z"}

# Not connected
> server status
Server Connection
─────────────────
  Status: Not connected
```

### Disconnecting

```bash
> server disconnect
Disconnecting from api.example.com...
✓ Disconnected

# If not connected
> server disconnect
⚠ Not connected to any server
```

---

## 3. Server Profile Management

### Creating a Profile

```bash
# Create a new profile (prompts for API key)
> server profile add prod --uri https://api.example.com
API Key: ********
✓ Profile 'prod' created

# Create with API key and set as default
> server profile add prod --uri https://api.example.com --api-key mykey --default
✓ Profile 'prod' created (set as default)

# Overwrite existing without confirmation
> server profile add prod --uri https://new-api.example.com --force
API Key: ********
✓ Profile 'prod' updated
```

### Listing Profiles

```bash
> server profile list
Server Profiles
───────────────
  prod *      api.example.com
  staging     staging.example.com
  dev         localhost:5000

* = default profile

# No profiles
> server profile list
No server profiles saved.

Create one with:
  server profile add <name> --uri <uri>
```

### Viewing Profile Details

```bash
> server profile show prod
Profile: prod
─────────────
  URI:          api.example.com
  Credentials:  Saved
  Default:      Yes
```

### Managing Default Profile

```bash
# Set default
> server profile set-default staging
✓ Default profile set to 'staging'

# Now connect without specifying profile
> server connect
Connecting to staging.example.com...
✓ Connected to staging.example.com
```

### Updating Credentials

```bash
> server profile set-key prod
API Key: ********
✓ Credentials updated for 'prod'

# Or provide key directly
> server profile set-key prod --api-key newkey
✓ Credentials updated for 'prod'
```

### Removing a Profile

```bash
> server profile remove prod
✓ Profile 'prod' removed
```

---

## 4. Autocomplete for Profiles

In REPL mode, profile names autocomplete automatically:

```bash
> server connect --profile pr<TAB>
prod

> server profile show st<TAB>
staging

> server profile remove <TAB>
dev    prod    staging
```

---

## 5. Custom Prompt Segments

### Understanding the New Prompt System

The prompt is now composed of segments that render independently:

```
myapp @api.example.com [prod]> 
│     │                │
│     │                └── ProfileSegment (Order: 110)
│     └── ServerConnectionSegment (Order: 100)
└── AppNameSegment (Order: 0)
```

### Creating a Custom Segment

```csharp
using BitPantry.CommandLine.Input;

public class GitBranchSegment : IPromptSegment
{
    public int Order => 50; // Between app name and connection
    
    public string? Render()
    {
        var branch = GetCurrentGitBranch();
        return branch != null ? $"({branch})" : null;
    }
    
    private string? GetCurrentGitBranch()
    {
        // Implementation to get current git branch
        // Returns null if not in a git repo
    }
}
```

### Registering Custom Segments

```csharp
var app = new CommandLineApplicationBuilder()
    .ConfigureServices(services =>
    {
        // Add custom segment
        services.AddSingleton<IPromptSegment, GitBranchSegment>();
    })
    .Build();
```

**Result:**
```
myapp (main) @api.example.com [prod]> 
```

### Replacing the Entire Prompt

```csharp
var app = new CommandLineApplicationBuilder()
    .ConfigureServices(services =>
    {
        // Replace default CompositePrompt
        services.AddSingleton<IPrompt, MyCustomPrompt>();
    })
    .Build();
```

---

## 6. Profile Storage Location

Profiles are stored in a cross-platform location:

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\BitPantry\CommandLine\profiles.json` |
| Linux | `~/.config/bitpantry-commandline/profiles.json` |
| macOS | `~/.config/bitpantry-commandline/profiles.json` |

Credentials are stored securely:
- **Windows**: DPAPI (Windows Credential Manager equivalent)
- **Linux/macOS**: Encrypted file with libsodium

---

## 7. Error Handling

### Invalid Profile Name

```bash
> server profile add my@profile --uri https://api.example.com
✗ Invalid profile name 'my@profile'. Names may only contain letters, numbers, hyphens, and underscores.
```

### Connection Errors

```bash
> server connect https://unreachable.example.com
Connecting to unreachable.example.com...
✗ Could not reach unreachable.example.com

> server connect https://api.example.com --api-key wrongkey
Connecting to api.example.com...
✗ Authentication failed — invalid API key

> server connect https://slow.example.com
Connecting to slow.example.com...
✗ Connection to slow.example.com timed out after 10 seconds
```

### Profile Errors

```bash
> server connect --profile nonexistent
✗ Profile 'nonexistent' not found

> server profile show nonexistent
✗ Profile 'nonexistent' not found
```

---

## 8. Exit Codes

Commands return appropriate exit codes for scripting:

| Command | Success | Failure |
|---------|---------|---------|
| `version` | 0 | - |
| `server connect` | 0 (connected) | 1 (failed), 2 (auth), 3 (args) |
| `server disconnect` | 0 | - |
| `server status` | 0 (connected) | 1 (disconnected) |
| `server profile *` | 0 | 1 (not found), 2 (cancelled) |

**Example Script:**
```bash
#!/bin/bash
if myapp server status --json > /dev/null 2>&1; then
    echo "Connected"
else
    echo "Not connected, connecting..."
    myapp server connect --profile prod
fi
```

---

## 9. Migration from Previous Version

### Breaking Changes

The `lc` (list commands) command has been removed. Command discovery is now built into the syntax via help:

```bash
# Old (removed)
> lc

# New
> --help           # List all commands
> server --help    # List server subcommands
```

### Server Connect Changes

The `--confirmDisconnect` flag has been removed. The new behavior:
- Already connected to same server → Shows success, no action
- Already connected to different server → Auto-disconnects first

```bash
# Old
> server connect https://new.example.com --confirmDisconnect

# New (automatic)
> server connect https://new.example.com
⚠ Disconnecting from old.example.com...
Connecting to new.example.com...
✓ Connected to new.example.com
```
