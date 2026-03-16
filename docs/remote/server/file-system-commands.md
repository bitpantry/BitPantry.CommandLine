# Remote File System Commands

Seven built-in commands manage files and directories on the server's [sandboxed file system](sandboxing.md). All paths are confined to the configured `StorageRootPath`.

---

## Command Summary

| Command | Description |
|---------|-------------|
| [`server ls`](#ls) | List directory contents |
| [`server mkdir`](#mkdir) | Create a directory |
| [`server rm`](#rm) | Remove files or directories |
| [`server mv`](#mv) | Move or rename |
| [`server cp`](#cp) | Copy files or directories |
| [`server cat`](#cat) | Display file contents |
| [`server stat`](#stat) | Show file or directory metadata |

---

## ls

List files and directories.

```
server ls [path] [--long] [--recursive] [--sort name|size|modified] [--reverse]
```

| Flag | Alias | Description |
|------|-------|-------------|
| `--long` | `-l` | Table format with Type, Name, Size, Last Modified columns |
| `--recursive` | — | Include subdirectories recursively |
| `--sort` | — | Sort by `name` (default), `size`, or `modified` |
| `--reverse` | — | Reverse the sort order |

**Path** is optional (defaults to storage root) and supports [glob patterns](#glob-patterns).

```
app> server ls
images/
report.txt

app> server ls -l
Type   Name          Size      Last Modified
FILE   report.txt    1.0 KB    2026-03-15 14:30
DIR    images/       —         2026-03-14 09:00

app> server ls --sort size --reverse
images/
report.txt

app> server ls *.txt
report.txt
```

If a glob pattern matches nothing, an explicit message is displayed:

```
app> server ls *.nomatch
No files matching: *.nomatch
```

---

## mkdir

Create a directory.

```
server mkdir <path> [--parents]
```

| Flag | Alias | Description |
|------|-------|-------------|
| `--parents` | `-p` | Create intermediate directories as needed |

```
app> server mkdir /reports
Created: /reports

app> server mkdir /a/b/c -p
Created: /a/b/c
```

Without `--parents`, an error is returned if the parent directory doesn't exist.

---

## rm

Remove files or directories.

```
server rm <path> [--recursive] [--directory] [--force]
```

| Flag | Alias | Description |
|------|-------|-------------|
| `--recursive` | `-r` | Remove non-empty directories and all contents |
| `--directory` | `-d` | Allow removal of empty directories |
| `--force` | `-f` | Skip confirmation prompts and ignore nonexistent paths |

Supports [glob patterns](#glob-patterns). When a glob matches 4 or more files (without `--force`), a confirmation prompt is shown.

```
app> server rm /old-report.txt
Removed: /old-report.txt

app> server rm /logs -r
Removed: /logs

app> server rm *.log -f
Removed 3 file(s)
```

If a glob pattern matches nothing (without `--force`), an explicit message is displayed:

```
app> server rm *.nomatch
No files matching: *.nomatch
```

The storage root directory cannot be deleted.

---

## mv

Move or rename a file or directory.

```
server mv <source> <destination> [--force]
```

| Flag | Alias | Description |
|------|-------|-------------|
| `--force` | `-f` | Overwrite the destination if it already exists |

```
app> server mv /draft.txt /final.txt
Moved: /draft.txt → /final.txt
```

Errors are returned if the source doesn't exist, the destination already exists (without `--force`), or source and destination are the same path.

---

## cp

Copy a file or directory.

```
server cp <source> <destination> [--recursive] [--force]
```

| Flag | Alias | Description |
|------|-------|-------------|
| `--recursive` | `-r` | Required for copying directories |
| `--force` | `-f` | Overwrite the destination if it already exists |

```
app> server cp /template.txt /template-copy.txt
Copied: /template.txt → /template-copy.txt

app> server cp /project /project-backup -r
Copied 12 items: /project → /project-backup
```

Copying a directory without `--recursive` produces an error.

---

## cat

Display the contents of a text file.

```
server cat <path> [--lines <n>] [--tail <n>] [--force]
```

| Flag | Alias | Description |
|------|-------|-------------|
| `--lines` | `-n` | Show only the first _n_ lines |
| `--tail` | `-t` | Show only the last _n_ lines |
| `--force` | `-f` | Bypass binary-file detection and large-file confirmation |

`--lines` and `--tail` are mutually exclusive. When either is used, a footer indicates the range shown:

```
app> server cat /data/log.txt --lines=5
2026-03-15 Starting...
2026-03-15 Connected.
2026-03-15 Processing batch 1
2026-03-15 Processing batch 2
2026-03-15 Processing batch 3
Showing first 5 of 120 lines
```

**Binary detection**: The first 8 KB of the file is scanned for null bytes. Binary files are rejected unless `--force` is used.

**Large-file prompt**: Files over 25 MB trigger a confirmation prompt unless `--force` or `--lines`/`--tail` is specified.

---

## stat

Display metadata for a file or directory.

```
server stat <path>
```

**File output:**

```
app> server stat /reports/q1.txt
Name: q1.txt
Type: File
Path: /reports/q1.txt
Size: 1.0 KB (1,024 bytes)
Created: 3/15/2026 2:30:00 PM
Last Modified: 3/15/2026 4:15:00 PM
```

**Directory output:**

```
app> server stat /reports
Name: reports
Type: Directory
Path: /reports
Size: 45.2 KB (46,285 bytes)
ItemCount: 8
FileCount: 6
DirectoryCount: 2
Created: 3/10/2026 9:00:00 AM
Last Modified: 3/15/2026 4:15:00 PM
```

Size for directories is the recursive sum of all contained files.

---

## Glob Patterns

`ls` and `rm` support glob patterns in the path argument:

| Pattern | Matches |
|---------|---------|
| `*.txt` | All `.txt` files in the directory |
| `data?.json` | `data1.json`, `dataA.json`, etc. |
| `[abc].log` | `a.log`, `b.log`, `c.log` |

Glob matching uses `Microsoft.Extensions.FileSystemGlobbing`.

---

## Security

All commands operate through `SandboxedFileSystem`, which enforces:

- **Path traversal protection** — Paths containing `../`, URL-encoded traversal sequences, or absolute paths outside the storage root are rejected with an "Access denied" error.
- **Storage root confinement** — Every resolved path must fall within the configured `StorageRootPath`.

See [File System & Sandboxing](sandboxing.md) for configuration details.

---

## See Also

- [File System & Sandboxing](sandboxing.md)
- [File Transfers](../client/file-transfers.md)
- [Setting Up the Server](index.md)
