# Implementation Plan: Remote File System Management Commands

**Branch**: `011-remote-fs-commands` | **Date**: 2026-03-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/011-remote-fs-commands/spec.md`

## Summary

Add seven file system management commands (`ls`, `mkdir`, `rm`, `mv`, `cp`, `cat`, `stat`) as **server-side commands** in the SignalR server library. Commands are registered in `AddCommandLineHub()` and automatically appear on connected clients as remote stubs. Each command injects `IFileSystem` (automatically `SandboxedFileSystem` on the server) and `IAnsiConsole` (automatically `SignalRAnsiConsole` during execution). No new RPC infrastructure is required — all seven commands execute via the existing `RunRequest` / `RunResponse` path.

## Technical Context

**Language/Version**: C# / .NET 8.0
**Primary Dependencies**: Spectre.Console (output), System.IO.Abstractions (file operations), Microsoft.Extensions.FileSystemGlobbing (glob matching), Microsoft.AspNetCore.SignalR (transport — no changes)
**Storage**: Server's sandboxed file system (`SandboxedFileSystem` wrapping `IFileSystem`)
**Testing**: MSTest 3.6.1 + FluentAssertions 6.12.0 + Moq + System.IO.Abstractions.TestingHelpers (MockFileSystem) + Spectre.Console.Testing (TestConsole) + `TestEnvironment` integration harness
**Target Platform**: .NET 8.0 server library
**Performance Goals**: Individual commands complete in < 2 seconds for baseline workloads (up to 1,000 entries, total data <= 100 MB, local TestServer environment). `cp -r` / `ls --recursive` of larger directories surface progress where specified.
**Constraints**: Path traversal enforcement via `SandboxedFileSystem` (automatic). `cat` must never push an entire large unfiltered binary file through the SignalR channel — binary detection + large-file prompt mitigate this.
**Scale/Scope**: 7 new command classes in server project + `ServerGroup.cs` marker. Zero new RPC messages or HTTP endpoint changes.

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-Driven Development | ✅ PASS | Unit tests (server) + integration tests defined in test-cases.md before code is written |
| II. Dependency Injection | ✅ PASS | `IFileSystem` and `IAnsiConsole` injected via constructor; `SandboxedFileSystem` wired automatically by DI |
| III. Security by Design | ✅ PASS | `SandboxedFileSystem` enforces path sandboxing on all file operations; no additional path validation needed per command; `rm` blocks storage root deletion explicitly |
| IV. Follow Existing Patterns | ✅ PASS | Same `[Group]` / `[Command]` / `[Argument]` / `[Flag]` attribute pattern as all other commands; `opt.RegisterCommand<T>()` in `AddCommandLineHub()` mirrors `builder.RegisterCommand<T>()` in client |
| V. Integration Testing | ✅ PASS | Integration test per command (happy path) via `TestEnvironment` |

## Project Structure

### Documentation (this feature)

```text
specs/011-remote-fs-commands/
├── plan.md              ← this file
├── research.md          ← Phase 0 decisions (R-001 through R-011)
├── data-model.md        ← entity definitions, IFileSystem call maps, output patterns
├── quickstart.md        ← developer onboarding guide
├── contracts/
│   └── rpc-contracts.md ← no new RPCs; ServerGroup resolution notes
└── test-cases.md        ← Phase 2 test case definitions
```

### Source Code Layout

```text
BitPantry.CommandLine.Remote.SignalR.Server/
├── Configuration/
│   └── IServiceCollectionExtensions.cs    ← MODIFY: add opt.RegisterCommand<T>() for all 7 commands
└── Commands/
    ├── ServerGroup.cs                      ← NEW: [Group(Name="server")] marker class
    ├── LsCommand.cs                        ← NEW
    ├── MkdirCommand.cs                     ← NEW
    ├── RmCommand.cs                        ← NEW
    ├── MvCommand.cs                        ← NEW
    ├── CpCommand.cs                        ← NEW
    ├── CatCommand.cs                       ← NEW
    └── StatCommand.cs                      ← NEW

BitPantry.CommandLine.Tests.Remote.SignalR/
├── ServerTests/
│   ├── LsCommandTests.cs                   ← NEW
│   ├── MkdirCommandTests.cs                ← NEW
│   ├── RmCommandTests.cs                   ← NEW
│   ├── MvCommandTests.cs                   ← NEW
│   ├── CpCommandTests.cs                   ← NEW
│   ├── CatCommandTests.cs                  ← NEW
│   └── StatCommandTests.cs                 ← NEW
└── IntegrationTests/
    ├── IntegrationTests_LsCommand.cs       ← NEW
    ├── IntegrationTests_MkdirCommand.cs    ← NEW
    ├── IntegrationTests_RmCommand.cs       ← NEW
    ├── IntegrationTests_MvCommand.cs       ← NEW
    ├── IntegrationTests_CpCommand.cs       ← NEW
    ├── IntegrationTests_CatCommand.cs      ← NEW
    └── IntegrationTests_StatCommand.cs     ← NEW
```

**Structure Decision**: All seven commands live in the server project's `Commands/` subdirectory. No client code changes. Test classes split between `ServerTests/` (unit, fast) and `IntegrationTests/` (end-to-end with TestEnvironment).

---

## Technical Design

### 1. `ServerGroup.cs` (New)

```csharp
using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands;

[Group(Name = "server")]
[Description("Remote server connection commands")]
public class ServerGroup { }
```

This duplicates the marker class from the client project. The group name `"server"` matches the client's `ServerGroup`, so remote stubs from the server merge into the existing "server" group on connected clients.

### 2. `IServiceCollectionExtensions.cs` Change

After `var commandRegistry = opt.CommandRegistryBuilder.Build(services);` is moved later, register commands before the build call:

```csharp
// Register remote file system commands on the server
opt.RegisterCommand<LsCommand>();
opt.RegisterCommand<MkdirCommand>();
opt.RegisterCommand<RmCommand>();
opt.RegisterCommand<MvCommand>();
opt.RegisterCommand<CpCommand>();
opt.RegisterCommand<CatCommand>();
opt.RegisterCommand<StatCommand>();
```

### 3. Command Argument Maps

All commands inherit `CommandBase`. Connection guard is **not needed** — server-side commands run on the server and have direct `IFileSystem` access; there is no "not connected" state.

#### `LsCommand`

```csharp
[InGroup<ServerGroup>]
[Command(Name = "ls")]
[Description("List files and directories")]
public class LsCommand : CommandBase
{
    [Argument(Position = 0, Name = "path", IsRequired = false)]
    [Description("Path or glob pattern to list (default: storage root)")]
    [ServerFilePathAutoComplete]
    public string Path { get; set; }

    [Argument(Name = "long"), Alias('l'), Flag]
    [Description("Detailed listing (size, date, type)")]
    public bool Long { get; set; }

    [Argument(Name = "recursive"), Flag]
    [Description("List recursively")]  
    public bool Recursive { get; set; }

    [Argument(Name = "sort")]
    [Description("Sort by: name (default), size, modified")]
    public string Sort { get; set; } = "name";

    [Argument(Name = "reverse"), Flag]
    [Description("Reverse sort order")]
    public bool Reverse { get; set; }
}
```

#### `MkdirCommand`

```csharp
[Argument(Position = 0, Name = "path", IsRequired = true)]
[ServerDirectoryPathAutoComplete]

[Argument(Name = "parents"), Alias('p'), Flag]
[Description("Create parent directories as needed")]
```

#### `RmCommand`

```csharp
[Argument(Position = 0, Name = "path", IsRequired = true)]
[ServerFilePathAutoComplete]

[Argument(Name = "recursive"), Alias('r'), Flag]
[Description("Remove directories and their contents recursively")]

[Argument(Name = "directory"), Alias('d'), Flag]
[Description("Remove empty directories")]

[Argument(Name = "force"), Alias('f'), Flag]
[Description("Do not prompt for confirmation; ignore non-existent files")]
```

#### `MvCommand`

```csharp
[Argument(Position = 0, Name = "source", IsRequired = true)]
[ServerFilePathAutoComplete]

[Argument(Position = 1, Name = "destination", IsRequired = true)]
[ServerFilePathAutoComplete]

[Argument(Name = "force"), Alias('f'), Flag]
[Description("Overwrite destination if it exists")]
```

#### `CpCommand`

```csharp
[Argument(Position = 0, Name = "source", IsRequired = true)]
[ServerFilePathAutoComplete]

[Argument(Position = 1, Name = "destination", IsRequired = true)]
[ServerFilePathAutoComplete]

[Argument(Name = "recursive"), Alias('r'), Flag]
[Description("Copy directories recursively")]

[Argument(Name = "force"), Alias('f'), Flag]
[Description("Overwrite destination files if they exist")]
```

#### `CatCommand`

```csharp
[Argument(Position = 0, Name = "path", IsRequired = true)]
[ServerFilePathAutoComplete]

[Argument(Name = "lines"), Alias('n')]
[Description("Display only the first N lines")]
public int? Lines { get; set; }

[Argument(Name = "tail"), Alias('t')]
[Description("Display only the last N lines")]
public int? Tail { get; set; }

[Argument(Name = "force"), Alias('f'), Flag]
[Description("Output binary files and skip large-file prompt")]
public bool Force { get; set; }
```

#### `StatCommand`

```csharp
[Argument(Position = 0, Name = "path", IsRequired = true)]
[ServerFilePathAutoComplete]
```

### 4. `LsCommand` Implementation Notes

```csharp
public async Task Execute(CommandExecutionContext ctx)
{
    var targetPath = string.IsNullOrWhiteSpace(Path) ? "/" : Path;
    
    // Resolve glob vs. directory path
    IEnumerable<DirectoryListingEntry> entries;
    if (GlobPatternHelper.ContainsGlobCharacters(targetPath))
    {
        var (baseDir, pattern) = GlobPatternHelper.ParseGlobPattern(targetPath);
        entries = EnumerateWithGlob(baseDir, pattern, Recursive);
    }
    else
    {
        entries = EnumerateDirect(targetPath, Recursive);
    }
    
    // Sort
    entries = SortEntries(entries, Sort, Reverse);
    
    // Render
    if (Long)
        RenderLongFormat(entries);
    else if (Recursive)
        RenderTreeFormat(entries);
    else
        RenderSimpleFormat(entries);
}
```

### 5. `RmCommand` Glob Flow

```csharp
if (GlobPatternHelper.ContainsGlobCharacters(path))
{
    var matches = EnumerateMatches(path);
    if (matches.Count >= RmConfirmationThreshold && !Force)
    {
        if (!_console.Confirm($"Delete {matches.Count} items?", defaultValue: false))
            return;
    }
    foreach (var match in matches) DeleteEntry(match);
}
else
{
    DeleteSingleEntry(path);
}
```

### 6. `CatCommand` Flow

```csharp
// 1. Validate mutual exclusion
if (Lines.HasValue && Tail.HasValue)
{
    _console.MarkupLine("[red]--lines and --tail cannot be used together.[/]");
    return;
}

// 2. Binary detection (first 8KB)
if (!Force)
{
    using var stream = _fileSystem.File.OpenRead(path);
    var buffer = new byte[CatBinaryCheckBytes];
    var read = stream.Read(buffer, 0, buffer.Length);
    if (Array.IndexOf(buffer, (byte)0, 0, read) >= 0)
    {
        _console.MarkupLine("[red]Binary file detected. Use --force to display anyway.[/]");
        return;
    }
}

// 3. Large-file prompt
var fileSize = _fileSystem.FileInfo.New(path).Length;
if (!Force && !Lines.HasValue && !Tail.HasValue && fileSize > CatLargeFileSizeBytes)
{
    if (!_console.Confirm($"File is {FormatSize(fileSize)}. Display all?", defaultValue: false))
        return;
}

// 4. Output
var allLines = _fileSystem.File.ReadAllLines(path);
var totalLines = allLines.Length;
string[] outputLines;

if (Lines.HasValue)
    outputLines = allLines.Take(Lines.Value).ToArray();
else if (Tail.HasValue)
    outputLines = allLines.TakeLast(Tail.Value).ToArray();
else
    outputLines = allLines;

foreach (var line in outputLines)
    _console.WriteLine(line);

if (Lines.HasValue || Tail.HasValue)
    _console.MarkupLine($"[grey]── Showing {(Lines.HasValue ? "first" : "last")} {outputLines.Length} of {totalLines} lines ──[/]");
```

---

## Testing Approach

### Unit Tests — `ServerTests/` (fast, no network)

**Setup pattern**:
```csharp
[TestInitialize]
public void Setup()
{
    _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { "/storage/report.txt", new MockFileData("line1\nline2\n") },
        { "/storage/images/", new MockDirectoryData() }
    });
    _console = new TestConsole();
    _command = new LsCommand(_fileSystem, _console);
}
```

**Coverage per command (examples)**:
- `LsCommand`: default output, `--long` table, `--recursive` tree, `--sort size`, `--reverse`, glob matching, empty directory, path not found
- `MkdirCommand`: creates directory, creates with `--parents`, fails without `--parents` when parent missing, fails if already exists
- `RmCommand`: delete file, delete empty dir with `-d`, delete non-empty dir with `-r`, glob confirmation threshold, `--force` skips prompt, cannot delete storage root
- `MvCommand`: move file, fail if source missing, fail if dest exists without `--force`
- `CpCommand`: copy file, recursive copy dir, fail without `--recursive` for dirs, `--force` overwrites
- `CatCommand`: normal output, `--lines=N` head, `--tail=N`, binary detection, large-file prompt, `--force` bypasses prompt, mutual exclusion error
- `StatCommand`: file stats, directory stats (with recursive totals), path not found

**Every flag is tested for its specific behaviour** — separate test method per flag/argument.

### Integration Tests — `IntegrationTests/` (real server)

```csharp
[TestMethod]
public async Task LsCommand_WithFiles_ListsThem()
{
    // Arrange
    File.WriteAllText(Path.Combine(_tempDir, "report.txt"), "content");
    
    using var env = new TestEnvironment(opts =>
    {
        opts.ServerOptions = new TestServerOptions { StorageRootPath = _tempDir };
    });
    await env.Cli.Run($"server connect -u {env.ServerUrl}");
    
    // Act
    await env.Cli.Run("server ls");
    
    // Assert
    env.Console.Should().ContainText("report.txt");
}
```

Integration tests validate:
- Server commands appear in client's command list after `server connect`
- File system state actually changes (directory created, file deleted, etc.)
- End-to-end output rendering (VirtualConsole captures SignalRAnsiConsole output)
- Error paths display correctly end-to-end

---

## Complexity Tracking

> No constitution violations.
