# Data Model: Remote File System Management Commands (011-remote-fs-commands)

> Phase 1 output. Server-side commands only — no new RPC envelopes required.

---

## Key Entities

### `DirectoryListingEntry` (in-memory, command output only)

Used by `LsCommand` to hold a single file system entry before rendering. Not serialized over the wire.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | File or directory name (no path) |
| `FullPath` | `string` | Full sandboxed path |
| `EntryType` | `EntryType` | `File` or `Directory` |
| `Size` | `long` | File size in bytes; `0` for directories |
| `LastModified` | `DateTime` | UTC last-modified timestamp |

### `EntryType` (enum, server project)

```csharp
public enum EntryType { File, Directory }
```

### `StatResult` (in-memory, command output only)

Used by `StatCommand` to hold all metadata before rendering. Not serialized.

| Property | Type | File | Directory |
|----------|------|------|-----------|
| `Name` | `string` | ✓ | ✓ |
| `FullPath` | `string` | ✓ | ✓ |
| `EntryType` | `EntryType` | ✓ | ✓ |
| `Size` | `long` | bytes | total bytes (recursive sum) |
| `CreatedUtc` | `DateTime` | ✓ | ✓ |
| `LastModifiedUtc` | `DateTime` | ✓ | ✓ |
| `ItemCount` | `int?` | — | total file + dir count (recursive) |
| `FileCount` | `int?` | — | file count (recursive) |
| `DirectoryCount` | `int?` | — | directory count (recursive) |

---

## Constants

| Constant | Value | Used By |
|----------|-------|---------|
| `RmConfirmationThreshold` | `5` | `RmCommand` — prompt before deleting ≥ 5 matched items |
| `CatBinaryCheckBytes` | `8192` (8 KB) | `CatCommand` — bytes to scan for null characters |
| `CatLargeFileSizeBytes` | `1_048_576` (1 MB) | `CatCommand` — prompt before displaying unfiltered file |
| `CpProgressThreshold` | `10` | `CpCommand` — show item count progress when copying ≥ 10 items |

---

## `IFileSystem` Operations Per Command

These are the `System.IO.Abstractions` operations each command uses. The injected `IFileSystem` is the server's `SandboxedFileSystem`, which enforces path sandboxing transparently.

### `LsCommand`

```
IFileSystem.Directory.Exists(path)
IFileSystem.Directory.GetFileSystemEntries(path, searchPattern, searchOption)
IFileSystem.FileInfo.New(filePath).Length / LastWriteTimeUtc
IFileSystem.DirectoryInfo.New(dirPath).LastWriteTimeUtc
```

Glob matching via `Microsoft.Extensions.FileSystemGlobbing.Matcher` (already a dependency of the server project).

### `MkdirCommand`

```
IFileSystem.Directory.Exists(path)
IFileSystem.Directory.CreateDirectory(path)   // creates intermediate dirs if --parents
```

Without `--parents`, fail if the parent directory does not exist (check `IFileSystem.Directory.Exists(parent)`).

### `RmCommand`

```
IFileSystem.File.Exists(path)
IFileSystem.Directory.Exists(path)
IFileSystem.File.Delete(path)
IFileSystem.Directory.Delete(path, recursive)
// Glob enumeration:
IFileSystem.Directory.GetFileSystemEntries(baseDir, "*", SearchOption.TopDirectoryOnly)
Matcher.Match(relativePaths)
```

Block deletion of storage root: compare `IFileSystem.Path.GetFullPath(path)` to sandboxed root.

### `MvCommand`

```
IFileSystem.File.Exists(src)
IFileSystem.Directory.Exists(src)
IFileSystem.File.Move(src, dst, overwrite)
IFileSystem.Directory.Move(src, dst)
IFileSystem.Path.GetDirectoryName(dst)  // verify parent exists
```

### `CpCommand`

```
IFileSystem.File.Exists(src)
IFileSystem.Directory.Exists(src)
IFileSystem.File.Copy(src, dst, overwrite)
// Recursive directory copy helper:
IFileSystem.Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly)
IFileSystem.Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly)
IFileSystem.Directory.CreateDirectory(destSubDir)
```

Returns count of items copied (for progress / summary output).

### `CatCommand`

```
IFileSystem.File.Exists(path)
IFileSystem.File.OpenRead(path)          // for binary detection (first 8KB)
IFileSystem.File.ReadAllLines(path)      // for --tail (need all lines)
IFileSystem.File.OpenText(path)          // for --head (stream, close after N lines)
IFileSystem.FileInfo.New(path).Length    // for large-file prompt
```

### `StatCommand`

```
IFileSystem.File.Exists(path)
IFileSystem.Directory.Exists(path)
IFileSystem.FileInfo.New(path)           // Length, CreationTimeUtc, LastWriteTimeUtc
IFileSystem.DirectoryInfo.New(path)      // CreationTimeUtc, LastWriteTimeUtc
IFileSystem.Directory.GetFiles(path, "*", SearchOption.AllDirectories)
IFileSystem.Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
```

---

## Console Output Patterns

### `LsCommand` — Default

```
report.txt
images/
README.md
```

### `LsCommand` — `--long` / `-l`

```
┌──────────┬──────────────┬────────────┬─────────────────────────┐
│ Type     │ Name         │ Size       │ Last Modified           │
├──────────┼──────────────┼────────────┼─────────────────────────┤
│ [dir]    │ images/      │ —          │ 2026-03-01 14:22 UTC    │
│ [file]   │ README.md    │ 4.2 KB     │ 2026-03-09 08:15 UTC    │
│ [file]   │ report.txt   │ 1.1 MB     │ 2026-03-10 11:00 UTC    │
└──────────┴──────────────┴────────────┴─────────────────────────┘
```

### `MkdirCommand`

```
Created: /data/reports
```

### `RmCommand` — per item

```
✓ report.txt
✓ archive.zip
```

### `MvCommand`

```
Moved: /old/path.txt → /new/path.txt
```

### `CpCommand`

```
Copied: /src/report.txt → /dst/report.txt
Copied 3 items.
```

### `CatCommand`

```
[content lines]

── Showing first 50 of 412 lines ──
```
(footer only when `--head` or `--tail` active)

### `StatCommand`

```
Name           report.txt
Type           File
Path           /reports/report.txt
Size           1.1 MB (1,123,456 bytes)
Created        2026-03-01 14:22 UTC
Last Modified  2026-03-10 11:00 UTC
```

---

## Error Messages

| Scenario | Message |
|----------|---------|
| Path not found | `Path not found: /foo/bar` |
| Not a directory | `/foo/bar.txt is not a directory` |
| Not a file | `/foo/bar/ is a directory` |
| Directory not empty (rm without -r) | `Directory is not empty. Use --recursive to delete.` |
| Recursive required (cp dir) | `Source is a directory. Use --recursive to copy.` |
| Binary file (cat) | `Binary file detected. Use --force to display anyway.` |
| Destination exists (mv/cp without -f) | `Destination already exists. Use --force to overwrite.` |
| Deleting storage root | `Cannot delete the storage root directory.` |
| `--head` and `--tail` together (cat) | `--head and --tail cannot be used together.` |
| Parents missing (mkdir without --parents) | `Parent directory does not exist. Use --parents to create.` |
