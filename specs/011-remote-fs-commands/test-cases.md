# Test Cases: Remote File System Management Commands (011-remote-fs-commands)

> Phase 2 output. Covers all 7 commands; every flag and argument has at least one dedicated test.
> Test level: **U** = Server Unit test (MockFileSystem + TestConsole), **I** = Integration test (TestEnvironment)

---

## UX — User Experience

Tests verifying output format, display text, and console rendering.

| ID | Level | Command | Scenario | Input / Setup | Expected Output |
|----|-------|---------|----------|---------------|-----------------|
| UX-001 | U | `ls` | Default list — files and directories shown | MockFS: `report.txt`, `images/` at root | `report.txt` and `images/` both appear in output |
| UX-002 | U | `ls` | Directories suffixed with `/` | MockFS: dir `images` | Output contains `images/` |
| UX-003 | U | `ls` | Files have no trailing `/` | MockFS: file `report.txt` | Output contains `report.txt` (no slash) |
| UX-004 | U | `ls -l` | Long format shows table with Type, Name, Size, Last Modified columns | MockFS: `report.txt` (100 bytes) | Table output with all 4 column headers present |
| UX-005 | U | `ls -l` | File size formatted as human-readable | MockFS: file 1,048,576 bytes | Output contains `1.0 MB` or equivalent |
| UX-006 | U | `ls -l` | Directory size column shows `—` | MockFS: dir | Output contains `—` in size column for dir |
| UX-007 | U | `ls --recursive` | Tree view shows nested entries | MockFS: `a/b/c.txt`, `a/d.txt` | All paths appear, hierarchy visible |
| UX-008 | U | `ls --sort size` | Entries ordered by size (smallest first) | MockFS: `big.txt` (1MB), `small.txt` (1KB) | `small.txt` appears before `big.txt` |
| UX-009 | U | `ls --sort size --reverse` | Entries ordered by size (largest first) | Same as UX-008 | `big.txt` appears before `small.txt` |
| UX-010 | U | `ls --sort modified` | Entries ordered by last modified (oldest first) | MockFS: two files with different timestamps | Older file first |
| UX-011 | U | `ls --sort name` | Entries ordered alphabetically | MockFS: `z.txt`, `a.txt` | `a.txt` before `z.txt` |
| UX-012 | U | `ls --reverse` | Reverses default (name) sort | MockFS: `a.txt`, `z.txt` | `z.txt` before `a.txt` |
| UX-013 | U | `mkdir` | Success message includes path | Path: `/reports` | Output: `Created: /reports` |
| UX-014 | U | `rm` | Per-item success indicator | Single file | Output contains `✓` and filename |
| UX-015 | U | `rm` | Multiple glob matches show item count | 3 matches; force | Output confirms 3 deletions |
| UX-016 | U | `mv` | Success shows source and destination | `mv /a.txt /b.txt` | Output: `Moved: /a.txt → /b.txt` |
| UX-017 | U | `cp` | Success shows source and destination | `cp /a.txt /b.txt` | Output: `Copied: /a.txt → /b.txt` |
| UX-018 | U | `cp --recursive` | Summary shows item count | 3 files copied | Output: `Copied 3 items.` |
| UX-019 | U | `cat` | Lines displayed without modification | File: `hello\nworld` | Console shows `hello` then `world` |
| UX-020 | U | `cat --lines=2` | Footer shows head indicator | File with 10 lines, `--lines=2` | Footer: `Showing first 2 of 10 lines` |
| UX-021 | U | `cat --tail=2` | Footer shows tail indicator | File with 10 lines, `--tail=2` | Footer: `Showing last 2 of 10 lines` |
| UX-022 | U | `cat` | No footer when neither `--lines` nor `--tail` used | Normal cat | No footer line |
| UX-023 | U | `stat` | All fields rendered for a file | File: `report.txt`, 512 bytes | Output contains Name, Type, Path, Size, Created, Last Modified |
| UX-024 | U | `stat` | Size shown in human-readable and raw bytes | File: 1,024 bytes | Shows both `1.0 KB` and `(1,024 bytes)` |
| UX-025 | U | `stat` | Directory shows ItemCount, FileCount, DirectoryCount | Dir with 2 files, 1 subdir | All three count fields present |
| UX-026 | I | `ls` | End-to-end output visible in VirtualConsole | Real file in tempDir | VirtualConsole contains filename |
| UX-027 | I | `mkdir` | Created message visible in VirtualConsole | Run `server mkdir /newdir` | VirtualConsole contains `Created` |
| UX-028 | I | `cat` | File content visible in VirtualConsole | File with known content | VirtualConsole contains exact text content |
| UX-029 | I | `help` | Command help text reflects remote file system syntax | Run `server ls --help` and `server rm --help` after connect | VirtualConsole contains expected arguments and flags for both commands |

---

## CV — Command Validation

Tests verifying argument parsing, flag interactions, required arguments, and mutual exclusions.

| ID | Level | Command | Scenario | Input | Expected Behaviour |
|----|-------|---------|----------|-------|--------------------|
| CV-001 | U | `ls` | Path argument is optional | No path provided | Uses storage root, no error |
| CV-002 | U | `ls` | Path argument accepted as positional | `ls /reports` | Lists `/reports` contents |
| CV-003 | U | `ls` | `--long` / `-l` flag activates long mode | `-l` provided | Table output with size column |
| CV-004 | U | `ls` | `--recursive` flag activates recursion | `--recursive` provided | Subdirectories also enumerated |
| CV-005 | U | `ls` | `--sort` accepts `name` | `--sort name` | Sorts by name |
| CV-006 | U | `ls` | `--sort` accepts `size` | `--sort size` | Sorts by size |
| CV-007 | U | `ls` | `--sort` accepts `modified` | `--sort modified` | Sorts by modified |
| CV-008 | U | `ls` | `--reverse` alone reverses default sort | `--reverse` | Name sort reversed |
| CV-009 | U | `ls` | `--reverse` combined with `--sort size` | `--sort size --reverse` | Largest first |
| CV-010 | U | `mkdir` | `path` argument is required | No path | Error or missing arg handling |
| CV-011 | U | `mkdir` | `--parents` / `-p` flag activates deep creation | `-p` provided | CreateDirectory called without checking parent |
| CV-012 | U | `rm` | `path` argument is required | No path | Error or missing arg handling |
| CV-013 | U | `rm` | `--recursive` / `-r` flag allows non-empty dir deletion | `-r` on non-empty dir | Deletes successfully |
| CV-014 | U | `rm` | `--directory` / `-d` flag allows empty dir deletion | `-d` on empty dir | Deletes successfully |
| CV-015 | U | `rm` | `--force` / `-f` flag skips confirmation | `-f` + 10 glob matches | No prompt shown |
| CV-016 | U | `rm` | Without `-r` deleting non-empty dir produces error | Non-empty dir, no flags | Error: use `--recursive` |
| CV-017 | U | `rm` | Without `-d` deleting empty dir produces error | Empty dir, no flags | Error: use `--directory` |
| CV-018 | U | `mv` | `source` argument is required | No source | Error or missing arg handling |
| CV-019 | U | `mv` | `destination` argument is required | No destination | Error or missing arg handling |
| CV-020 | U | `mv` | `--force` / `-f` flag allows overwrite | `-f` + dest exists | Move succeeds |
| CV-021 | U | `cp` | `source` argument is required | No source | Error or missing arg handling |
| CV-022 | U | `cp` | `destination` argument is required | No destination | Error or missing arg handling |
| CV-023 | U | `cp` | `--recursive` / `-r` required for directory copy | Dir source, no `-r` | Error: use `--recursive` |
| CV-024 | U | `cp` | `--recursive` / `-r` accepted for directory copy | Dir source, `-r` | Copy succeeds |
| CV-025 | U | `cp` | `--force` / `-f` flag allows overwrite | `-f` + dest file exists | Copy succeeds, overwrites |
| CV-026 | U | `cat` | `path` argument is required | No path | Error or missing arg handling |
| CV-027 | U | `cat` | `--lines` / `-n` accepts integer | `--lines=5` | First 5 lines output |
| CV-028 | U | `cat` | `--tail` / `-t` accepts integer | `--tail=5` | Last 5 lines output |
| CV-029 | U | `cat` | `--lines` and `--tail` mutually exclusive | Both provided | Error: cannot use together |
| CV-030 | U | `cat` | `--force` / `-f` flag bypasses binary check | Binary file + `-f` | Output displayed without error |
| CV-031 | U | `cat` | `--force` / `-f` flag bypasses large-file prompt | 2MB file, no `--lines`, `-f` | Output starts without prompt |
| CV-032 | U | `stat` | `path` argument is required | No path | Error or missing arg handling |
| CV-033 | I | `ls` | `-l` alias accepted as `--long` | `-l` in Cli.Run | Table output |
| CV-034 | I | `rm` | `-r` alias accepted as `--recursive` | `-r` in Cli.Run | Recursive delete |
| CV-035 | I | `cp` | `-r` alias accepted as `--recursive` | `-r` in Cli.Run | Recursive copy |
| CV-036 | I | `mkdir` | `-p` alias accepted as `--parents` | `-p` in Cli.Run | Deep dir created |
| CV-037 | I | `mv` | `-f` alias accepted as `--force` | `-f` in Cli.Run | Overwrite succeeds |

---

## DF — Data Flow / Functional

Tests verifying that the operations actually work correctly — files created, deleted, moved, etc.

| ID | Level | Command | Scenario | Setup | Expected Outcome |
|----|-------|---------|----------|-------|-----------------|
| DF-001 | U | `ls` | Lists files at specified path | MockFS: `/reports/q1.txt`, `/reports/q2.txt` | Both files returned |
| DF-002 | U | `ls` | Lists subdir contents when path is a dir | MockFS: `/data/a.txt` in subdir | `a.txt` listed |
| DF-003 | U | `ls` | Glob pattern `*.txt` filters to text files | MockFS: `a.txt`, `b.log` | Only `a.txt` returned |
| DF-004 | U | `ls` | Glob `*.log` matches multiple | MockFS: `a.log`, `b.log`, `c.txt` | Two log files returned, not c.txt |
| DF-005 | U | `ls --recursive` | Traverses subdirectories | MockFS: nested 3 levels | All files at all depths listed |
| DF-006 | U | `ls --sort size` | Actual sort by file size | MockFS: files with different known sizes | Order matches expected size sort |
| DF-007 | U | `mkdir` | Directory created at path | Path: `/reports` | `_fileSystem.Directory.Exists("/reports")` is true |
| DF-008 | U | `mkdir --parents` | All intermediate dirs created | Path: `/a/b/c` | `/a`, `/a/b`, `/a/b/c` all exist |
| DF-009 | U | `mkdir` | Fails if parent missing without `--parents` | Path `/a/b`, `/a` doesn't exist | Directory NOT created, error displayed |
| DF-010 | U | `mkdir` | Idempotent when directory already exists | Path already exists | No error, exists is still true |
| DF-011 | U | `rm` | Single file deleted | MockFS: `file.txt` | `File.Exists` false after |
| DF-012 | U | `rm -d` | Empty directory deleted | MockFS: empty dir | `Directory.Exists` false after |
| DF-013 | U | `rm` | Non-existent path with `--force` | File doesn't exist, `-f` | No error displayed |
| DF-014 | U | `rm` | Non-existent path without `--force` | File doesn't exist | Error: path not found |
| DF-015 | U | `rm -r` | Non-empty directory deleted recursively | MockFS: dir with files | Dir and all contents gone |
| DF-016 | U | `rm` | Glob pattern matches and deletes multiple | MockFS: `a.log`, `b.log`, `c.txt`; pattern `*.log`; `-f` | `a.log` and `b.log` deleted, `c.txt` remains |
| DF-017 | U | `rm` | Glob with fewer than threshold — no prompt | MockFS: 3 files matching; no `-f` | Files deleted without prompt |
| DF-018 | U | `rm` | Glob with ≥ threshold — prompts (answered yes) | MockFS: 5 files matching; no `-f`; console confirms | All 5 deleted |
| DF-019 | U | `rm` | Glob with ≥ threshold — prompts (answered no) | 5 matches; no `-f`; console declines | None deleted |
| DF-020 | U | `rm` | Cannot delete storage root | Path is root | Error: cannot delete storage root |
| DF-021 | U | `mv` | File moved to new location | `a.txt` → `b.txt` | `b.txt` exists, `a.txt` gone |
| DF-022 | U | `mv` | Directory moved | `/src/` → `/dst/` | `/dst/` exists, `/src/` gone |
| DF-023 | U | `mv` | Fails if source not found | Source doesn't exist | Error displayed, no change |
| DF-024 | U | `mv` | Fails if destination exists without `--force` | Dest exists, no `-f` | Error: destination exists |
| DF-025 | U | `mv --force` | Overwrites existing destination file | Dest exists, `-f` | Move succeeds, dest has src content |
| DF-026 | U | `mv` | Fails if source same as destination | src == dst | Error displayed |
| DF-027 | U | `cp` | File copied, original preserved | `a.txt` → `b.txt` | Both `a.txt` and `b.txt` exist |
| DF-028 | U | `cp -r` | Directory and contents copied | `/src/a.txt` → `/dst/` | `/dst/a.txt` exists, `/src/a.txt` still exists |
| DF-029 | U | `cp -r` | Nested directory structure preserved | `/src/a/b.txt` | `/dst/a/b.txt` created |
| DF-030 | U | `cp` | Fails if source directory without `--recursive` | Dir source, no `-r` | Error displayed |
| DF-031 | U | `cp` | Fails if dest file exists without `--force` | Dest exists, no `-f` | Error displayed |
| DF-032 | U | `cp --force` | Overwrites existing destination | Dest exists, `-f` | Copy succeeds |
| DF-033 | U | `cat` | Outputs all lines of text file | File: "line1\nline2\nline3" | All 3 lines in output |
| DF-034 | U | `cat --lines=2` | Outputs only first 2 lines | File with 10 lines | Lines 1 and 2 only; line 3 absent |
| DF-035 | U | `cat --lines=100` | `--lines` > file length: all lines | File with 5 lines, `--lines=100` | All 5 lines; no error |
| DF-036 | U | `cat --tail=2` | Outputs only last 2 lines | File: "1\n2\n3\n4\n5" | Lines 4 and 5 only |
| DF-037 | U | `cat --tail=100` | `--tail` > file length: all lines | File with 5 lines, `--tail=100` | All 5 lines |
| DF-038 | U | `cat` | Binary file detected — aborts | File with null byte | Error displayed; no content output |
| DF-039 | U | `cat --force` | Binary file with `--force` — outputs anyway | File with null byte, `-f` | Content output |
| DF-040 | U | `cat` | Large file without `--lines` prompts (yes) | File > 1MB; no `--lines`; console confirms | Content output |
| DF-041 | U | `cat` | Large file without `--lines` prompts (no) | File > 1MB; no `--lines`; console declines | No output |
| DF-042 | U | `cat --force` | Large file with `--force` — no prompt | File > 1MB, `-f` | Content output without prompt |
| DF-043 | U | `stat` | Returns correct name and path for file | File: `/reports/q1.txt` | Name `q1.txt`, Path `/reports/q1.txt` |
| DF-044 | U | `stat` | Returns correct size for file | File with 512 bytes | Size shows 512 |
| DF-045 | U | `stat` | Returns created and modified timestamps | File with known timestamps | Both timestamps appear in output |
| DF-046 | U | `stat` | Returns correct file count for directory | Dir with 3 files, 1 subdir | FileCount=3, DirectoryCount=1, ItemCount=4 |
| DF-047 | U | `stat` | Directory total size is recursive sum | Dir with 2 files (100B each) | TotalSize=200 |
| DF-048 | I | `ls` | End-to-end: files in tempDir appear after connect | Plant `report.txt` in tempDir | `server ls` output contains `report.txt` |
| DF-049 | I | `mkdir` | End-to-end: directory exists on disk after command | Run `server mkdir /newdir` | `Directory.Exists(Path.Combine(tempDir, "newdir"))` is true |
| DF-050 | I | `rm` | End-to-end: file gone after command | Plant file; run `server rm /file.txt` | `File.Exists` false |
| DF-051 | I | `mv` | End-to-end: file at new location | Plant `a.txt`; run `server mv /a.txt /b.txt` | `b.txt` exists, `a.txt` gone |
| DF-052 | I | `cp` | End-to-end: both files exist | Plant `a.txt`; run `server cp /a.txt /b.txt` | Both exist |
| DF-053 | I | `cat` | End-to-end: file content visible in VirtualConsole | File: "hello world"; run `server cat /file.txt` | VirtualConsole contains `hello world` |
| DF-054 | I | `stat` | End-to-end: stat output visible | Plant file; run `server stat /file.txt` | VirtualConsole contains file name |
| DF-055 | I | `ls` | Server commands appear after connect | Connect to test server | `server ls` resolves as a known command |

---

## EH — Error Handling

Tests verifying error conditions, boundary cases, and security enforcement.

| ID | Level | Command | Scenario | Input | Expected Error Behaviour |
|----|-------|---------|----------|-------|--------------------------|
| EH-001 | U | `ls` | Path not found | `/nonexistent` | Error: `Path not found: /nonexistent` |
| EH-002 | U | `ls` | Path is a file (not a dir) and no glob | `ls /file.txt` | Lists `file.txt` only (single item) |
| EH-003 | U | `mkdir` | Parent does not exist | `/a/b` (no `/a`) without `-p` | Error: parent does not exist |
| EH-004 | U | `rm` | Path not found without `--force` | `/nonexistent` | Error message displayed |
| EH-005 | U | `rm` | Path not found with `--force` | `/nonexistent`, `-f` | No error, silent success |
| EH-006 | U | `rm` | Non-empty dir without `-r` or `-d` | Non-empty dir | Error: directory not empty; use `--recursive` |
| EH-007 | U | `rm` | Empty dir without `-d` or `-r` | Empty dir | Error: use `--directory` to remove |
| EH-008 | U | `rm` | Attempt to delete storage root | Root path | Error: cannot delete storage root directory |
| EH-009 | U | `mv` | Source not found | `/nosuchfile.txt` → `/dst` | Error: path not found |
| EH-010 | U | `mv` | Destination already exists without `--force` | Dest exists | Error: destination already exists |
| EH-011 | U | `mv` | Source equals destination | `/a.txt` → `/a.txt` | Error: source and destination are the same |
| EH-012 | U | `cp` | Source not found | `/nosuchfile.txt` | Error: path not found |
| EH-013 | U | `cp` | Source is directory without `--recursive` | Dir source | Error: use `--recursive` |
| EH-014 | U | `cp` | Destination exists without `--force` | Dest file exists | Error: destination already exists |
| EH-015 | U | `cat` | File not found | `/nosuchfile.txt` | Error: path not found |
| EH-016 | U | `cat` | Path is a directory | `/somedir` | Error: is a directory |
| EH-017 | U | `cat` | Binary content without `--force` | Binary file | Error: binary file detected |
| EH-018 | U | `cat` | `--lines=0` | Zero lines requested | No output, no error |
| EH-019 | U | `cat` | `--lines` and `--tail` together | Both flags | Error: cannot use `--lines` and `--tail` together |
| EH-020 | U | `stat` | Path not found | `/nosuchpath` | Error: path not found |
| EH-021 | U | `ls` | `SandboxedFileSystem` blocks path traversal attempt | `../../etc/passwd` | Exception caught; error message; no output |
| EH-022 | U | `mkdir` | Path traversal attempt | `../../tmp/evil` | Error; directory not created |
| EH-023 | U | `rm` | Path traversal attempt | `../../etc/` | Error; nothing deleted |
| EH-024 | U | `mv` | Path traversal in source | `../../private` → `/dst` | Error; nothing moved |
| EH-025 | U | `cp` | Path traversal in destination | `/src` → `../../evil/` | Error; nothing copied |
| EH-026 | U | `cat` | Path traversal attempt | `../../etc/shadow` | Error; no content output |
| EH-027 | U | `stat` | Path traversal attempt | `../../etc/` | Error; no output |
| EH-028 | I | `rm` | Cannot delete outside sandbox in integration | Attempt to delete system temp path outside storage root | Error displayed; path exists after |
| EH-029 | I | `ls` | Path not found returns error (not exception) | Run `server ls /nosuchdir` | VirtualConsole contains error message, not stack trace |
| EH-030 | I | `cat` | Binary file error shown end-to-end | Plant binary file; run `server cat /file.bin` | VirtualConsole contains binary file error message |
| EH-031 | I | `ls` | Command invoked while disconnected | Start CLI without server connection; run `server ls` | VirtualConsole contains standard not-connected message |
| EH-032 | U | `ls` | Glob pattern matches nothing | Pattern `*.nomatch` against non-matching directory | Output includes explicit no-matches message |
| EH-033 | U | `rm` | Glob pattern matches nothing | Pattern `*.nomatch` against non-matching directory | Output includes explicit no-matches message and no deletions |
| EH-034 | I | `cp` | Mid-operation disconnect is handled clearly | Start large recursive copy; disconnect during operation | Operation aborts and VirtualConsole shows clear disconnection error |
