# BitPantry.CommandLine Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-24

## Active Technologies
- C# / .NET 8.0 (VirtualConsole targets .NET Standard 2.0) + FluentAssertions 6.12.0, Spectre.Console, MSTest 3.6.1 (005-virtualconsole-integration)
- C# / .NET 8.0 + Spectre.Console (progress display), Microsoft.AspNetCore.SignalR.Client (server communication), System.IO.Abstractions (file operations) (006-upload-command)
- Local filesystem (client), server-side sandboxed storage (via existing infrastructure) (006-upload-command)

- C# / .NET (matches existing solution) + BitPantry.Parsing.Strings (existing), MSTest, FluentAssertions, Moq (004-positional-arguments)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# / .NET (matches existing solution)

## Code Style

C# / .NET (matches existing solution): Follow standard conventions

## Recent Changes
- 006-upload-command: Added C# / .NET 8.0 + Spectre.Console (progress display), Microsoft.AspNetCore.SignalR.Client (server communication), System.IO.Abstractions (file operations)
- 005-virtualconsole-integration: Added C# / .NET 8.0 (VirtualConsole targets .NET Standard 2.0) + FluentAssertions 6.12.0, Spectre.Console, MSTest 3.6.1

- 004-positional-arguments: Added C# / .NET (matches existing solution) + BitPantry.Parsing.Strings (existing), MSTest, FluentAssertions, Moq

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
