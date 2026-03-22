---
name: cross-platform-testing
description: "Validate tests on Linux via WSL. Use when: running cross-platform tests, fixing CI failures on ubuntu-latest, verifying path-handling code, preparing releases, debugging Linux-only test failures, cross-platform file path issues."
---

# Cross-Platform Testing via WSL

Run the .NET test suite on Linux locally using Windows Subsystem for Linux (WSL), without pushing to GitHub Actions.

## When to Use

- Before pushing a release tag (CI runs on `ubuntu-latest`)
- After fixing cross-platform path issues (Windows `\` vs Linux `/`)
- When CI fails on Linux but tests pass on Windows
- Any time code touches file paths, `MockFileSystem`, `Path.Combine`, or `Path.GetFullPath`

## Prerequisites

- WSL2 with Ubuntu distribution installed and running
- .NET SDK installed at `/usr/share/dotnet/` (see Setup section if missing)

## Quick Reference

### Run all tests on Linux

```
wsl -d Ubuntu -- bash -c 'cd /mnt/c/src/bitpantry/BitPantry.CommandLine && /usr/share/dotnet/dotnet test --configuration Release --verbosity normal'
```

### Run a specific test project on Linux

```
wsl -d Ubuntu -- bash -c 'cd /mnt/c/src/bitpantry/BitPantry.CommandLine && /usr/share/dotnet/dotnet test BitPantry.CommandLine.Tests.Remote.SignalR --configuration Release --verbosity normal'
```

### Get just the pass/fail summary

```
wsl -d Ubuntu -- bash -c 'cd /mnt/c/src/bitpantry/BitPantry.CommandLine && /usr/share/dotnet/dotnet test --configuration Release --verbosity normal 2>&1 | grep -E "(Passed!|Failed!|Test Run|Total tests|     Passed|     Failed|     Skipped)"'
```

### Run on Windows (regression check)

```
dotnet test --configuration Release --verbosity normal
```

## Procedure

### 1. Run tests on Windows first

Confirm baseline — all tests should pass on Windows before testing Linux.

### 2. Run tests on Linux via WSL

Use the commands above. The source tree at `C:\src\bitpantry\BitPantry.CommandLine` is shared via `/mnt/c/` — no file copying needed. Edits made on Windows are immediately visible in WSL.

### 3. Analyze failures

Linux-only failures are almost always caused by:
- **Hardcoded Windows paths** (`C:\storage`, `C:\work`) — not valid rooted paths on Linux
- **Path separator assumptions** — `\` in assertions vs `/` from `Path.Combine` on Linux
- **`MockFileSystem` root** — `C:\storage` is not rooted on Linux; use platform-portable roots
- **Relative path resolution** — `Path.GetFullPath("./relative")` resolves differently depending on CWD
- **ANSI/color differences** — Spectre.Console may emit different SGR sequences on Linux

### 4. Fix and iterate

Edit files on Windows, re-run via WSL. Repeat until both platforms pass.

### 5. Confirm no Windows regression

Run `dotnet test` on Windows one final time before pushing.

## Setup (One-Time)

If .NET SDK is not available at `/usr/share/dotnet/dotnet` in WSL:

```
wsl -d Ubuntu -- bash -c 'wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh 2>/dev/null && chmod +x /tmp/dotnet-install.sh && sudo /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet'
```

Verify:

```
wsl -d Ubuntu -- bash -c '/usr/share/dotnet/dotnet --version'
```

## Gotchas

- **Do NOT use `dotnet` (bare command) in WSL** — `/usr/bin/dotnet` is the Ubuntu apt package (older SDK, e.g., 8.0.125). Always use the full path `/usr/share/dotnet/dotnet` which has the Microsoft-installed SDK matching the Windows version.
- **PATH escaping** — Windows PATH variables leak into WSL via `/mnt/c/...` and contain spaces that break `bash -c "export PATH=..."`. Always use single-quoted `bash -c '...'` commands, or use the full `/usr/share/dotnet/dotnet` path directly.
- **Shared filesystem** — `/mnt/c/` is the Windows C: drive. The `bin/` and `obj/` directories are shared between Windows and WSL. If you see strange build errors, try `dotnet clean` in WSL first.
- **File permissions** — Files on `/mnt/c/` have Windows permissions. This doesn't normally affect `dotnet test` but can cause issues with scripts that check `chmod` bits.
