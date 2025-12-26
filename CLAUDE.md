# BitPantry.CommandLine Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-24

## Active Technologies
- C# / .NET 8.0 + Spectre.Console, Microsoft.AspNetCore.SignalR.Client, System.Security.Cryptography.ProtectedData (Windows), Sodium.Core (cross-platform) (006-core-commands)
- JSON file for profiles, OS credential store + encrypted file fallback for credentials (006-core-commands)

- C# / .NET (matches existing solution) + BitPantry.Parsing.Strings (existing), MSTest, FluentAssertions, Moq (004-positional-arguments)

## Project Structure

```text
src/
tests/
```

## Built-in Commands

### Core Package (BitPantry.CommandLine)
- `version [--full]` - Display application version information

### SignalR Client Package (BitPantry.CommandLine.Remote.SignalR.Client)
- `server connect [profile] [--uri] [--apikey]` - Connect to remote server
- `server disconnect [--force]` - Disconnect from server  
- `server status [--verbose]` - Show connection and profile status
- `server profile add <name> --uri` - Create connection profile
- `server profile remove <name>` - Remove profile
- `server profile list` - List all profiles
- `server profile show <name>` - Show profile details
- `server profile set-default <name>` - Set default profile
- `server profile set-key <name> --apikey` - Update API key

## Prompt System

The prompt uses a segment-based architecture:
- `IPromptSegment` - Interface for prompt segments with `Order` and `Render()` 
- `IPrompt` - Interface for prompt rendering
- `CompositePrompt` - Aggregates segments sorted by Order
- `AppNameSegment` - Shows application name (core)
- `ServerConnectionSegment` - Shows connection status (SignalR client)
- `ProfileSegment` - Shows active profile (SignalR client)

## Profile/Credential Storage

- Profiles: JSON at cross-platform config paths
- Credentials: `ICredentialStore` with OS credential store or encrypted file fallback
- Profile manager: `IProfileManager` interface

## Code Style

C# / .NET (matches existing solution): Follow standard conventions

## Recent Changes
- 006-core-commands: Added prompt system (IPromptSegment, CompositePrompt), profile management, version command, removed lc command
- 006-core-commands: Added C# / .NET 8.0 + Spectre.Console, Microsoft.AspNetCore.SignalR.Client, System.Security.Cryptography.ProtectedData (Windows), Sodium.Core (cross-platform)

- 004-positional-arguments: Added C# / .NET (matches existing solution) + BitPantry.Parsing.Strings (existing), MSTest, FluentAssertions, Moq

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
