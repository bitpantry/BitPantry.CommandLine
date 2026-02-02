# Feature: Upload Command

## Overview

Add an `upload` command to the `server` command group that enables users to upload files from the local machine to the connected remote server. The command follows the conventional `<source> <destination>` pattern and supports glob patterns for multi-file uploads.

## User Stories

### US-001: Single File Upload
**As a** CLI user  
**I want to** upload a single file to a remote server  
**So that** I can transfer files without leaving the command line

**Acceptance Criteria:**
- Command syntax: `server upload <source> <destination>`
- Source is a local file path
- Destination is the remote directory or file path
- Upload completes with success message showing file transferred

### US-002: Multi-File Upload with Glob Pattern
**As a** CLI user  
**I want to** upload multiple files using wildcard patterns  
**So that** I can efficiently transfer batches of files

**Acceptance Criteria:**
- Source supports glob patterns (`*`, `?`)
- Pattern is expanded client-side before upload
- Each matched file uploads to the destination directory
- Summary shows count of files transferred

### US-003: Progress Display
**As a** CLI user  
**I want to** see upload progress  
**So that** I can monitor transfers

**Acceptance Criteria:**
- Single file >= 1MB: Display progress bar with filename
- Single file < 1MB: Upload silently (success message only)
- Multi-file upload: Always display progress for all files
- Progress shows filename and percentage complete

### US-004: Upload from External Shell
**As a** CLI user  
**I want to** invoke upload from an external shell (PowerShell, cmd, bash)  
**So that** I can script file transfers

**Acceptance Criteria:**
- Command works when invoked from external shell
- Glob patterns must be quoted to prevent shell expansion: `"*.txt"`
- Unquoted patterns may be expanded by shell, resulting in argument mismatch
- Single literal file paths work without quoting

### US-005: Skip Existing Files
**As a** CLI user  
**I want to** skip uploading files that already exist on the server  
**So that** I can perform non-destructive batch uploads

**Acceptance Criteria:**
- Optional `--skip-existing` / `-s` flag
- Files that exist on server are skipped with warning message
- Skipped files shown in summary
- Without flag, existing files are overwritten (default behavior)

## Functional Requirements

### FR-001: Command Registration
The `upload` command SHALL be registered under the `server` command group with the following signature:

```
server upload <source> <destination>
```

### FR-002: Positional Arguments
- `source` - Position 0, Required - Local file path or glob pattern
- `destination` - Position 1, Required - Remote directory or file path

### FR-003: Connection Prerequisite
The command SHALL verify an active server connection exists before attempting upload. If not connected, display error message and return failure.

### FR-004: Client-Side Glob Expansion
When running in the REPL (CommandLineApplication):
- The command SHALL expand glob patterns using `Microsoft.Extensions.FileSystemGlobbing`
- Supported wildcards: `*` (any characters), `?` (single character), `**` (recursive)
- Pattern matching uses the source argument's directory as the base
- Examples:
  - `*.txt` - All .txt files in current directory
  - `**/*.txt` - All .txt files recursively
  - `logs/**/*.log` - All .log files under logs/ recursively

### FR-005: Shell-Invoked Glob Handling
When invoked from an external shell:
- Glob patterns MUST be quoted to prevent shell expansion (e.g., `"*.txt"`)
- Without quotes, shell expands globs before passing arguments, causing argument count mismatch
- Primary use case is REPL where glob expansion is handled by the command
- Document quoting requirement in command help text

### FR-006: Concurrent Multi-File Upload
When source resolves to multiple files:
- Upload files concurrently with configurable degree of parallelism
- Concurrency level determined during planning phase based on research
- Destination is treated as a directory
- Each file retains its original filename at destination
- Continue uploading remaining files if one fails (track error, continue)

### FR-007: No Matches Handling
When a glob pattern matches zero files:
- Display warning: "No files matched pattern: {pattern}"
- Return success (not an error condition)

### FR-008: Progress Display Rules

**Single File Upload:**
- Files >= 1MB: Display Spectre.Console progress bar with filename
- Files < 1MB: Upload without progress display (success message only)

**Multi-File Upload:**
- Always display progress table regardless of file sizes
- Show all files with their status: "Pending" (0%), percentage during upload, "Completed" when done
- Missing/not-found files: Do NOT display in progress table; include in summary only (per FR-010)
- Use Spectre.Console Progress with multiple tasks for concurrent display

### FR-009: Success Output
On completion, display:
- Single file: "Uploaded {filename} to {destination}"
- Multiple files: "Uploaded {count} files to {destination}"

### FR-010: Error Handling
- Connection errors: Display connection-specific message
- File not found (single file): Display "File not found: {path}", return 1
- File not found (multi-file): Skip file, do not show in progress, include in summary
- Permission errors: Display underlying error message
- Partial failure (multi-file): Continue with remaining files, summarize failures at end
- Summary format: "Uploaded X of Y files. Z files not found: {list}"

### FR-011: File Exists Handling
**Default behavior**: Overwrite existing files on server (no flag required)

**With `--skip-existing` / `-s` flag**:
- Before upload, check which destination files already exist on server
- Use batch existence check (single request for all files)
- Skip files that exist, display warning: "Skipped (exists): {filename}"
- Include skipped count in summary: "Uploaded X files. Y skipped (already exist)."

### FR-012: Recursive Glob Pattern
When source contains `**` pattern:
- Match files recursively in subdirectories
- Preserve relative directory structure at destination
- Example: `src/**/*.cs` uploads all .cs files under src/, maintaining folder structure

## Screen Mockups

### Single File < 1MB (No Progress)
```
> server upload config.json /remote/settings/
Uploaded config.json to /remote/settings/
```

### Single File >= 1MB (Progress Bar)
```
> server upload database-backup.sql /remote/backups/
database-backup.sql [████████████░░░░░░░░] 60% ⠋
```

After completion:
```
> server upload database-backup.sql /remote/backups/
Uploaded database-backup.sql to /remote/backups/
```

### Multi-File Upload (Concurrent Progress)
```
> server upload "*.log" /remote/logs/
report-2024.log [████████████████████] 100% Completed
access.log      [████████████░░░░░░░░]  58% ⠋
error.log       [████░░░░░░░░░░░░░░░░]  20% ⠙
debug.log       [░░░░░░░░░░░░░░░░░░░░]   0% Pending
```

### Multi-File with Errors (Summary After)
```
> server upload "*.log" /remote/logs/
report-2024.log [████████████████████] 100% Completed
access.log      [████████████████████] 100% Completed
error.log       [████████████████████] 100% Failed
debug.log       [████████████████████] 100% Completed

⚠ error.log: Connection timeout
Uploaded 3 of 4 files to /remote/logs/
```

### Multi-File with Skip Existing
```
> server upload "*.txt" /remote/docs/ --skip-existing
report.txt    [████████████████████] 100% Completed
readme.txt    Skipped (exists)
notes.txt     [████████████████████] 100% Completed

Uploaded 2 files. 1 skipped (already exist).
```

## Technical Design

### Command Implementation

```csharp
[Command("upload", Description = "Uploads a file to the remote server")]
public class UploadCommand : CommandBase
{
    private readonly SignalRServerProxy _proxy;
    private readonly FileTransferService _fileTransferService;
    private readonly IConsoleService _console;

    [Argument(Position = 0, Name = "source", 
        Description = "Local file path or glob pattern (quote for shell use)", IsRequired = true)]
    public string Source { get; set; }

    [Argument(Position = 1, Name = "destination", 
        Description = "Remote destination path", IsRequired = true)]
    public string Destination { get; set; }

    [Argument]
    [Alias('s')]
    [Flag]
    [Description("Skip files that already exist on the server")]
    public bool SkipExisting { get; set; }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_proxy.IsConnected)
        {
            _console.WriteError("Not connected to server");
            return 1;
        }

        var (existingFiles, missingFiles) = ExpandSource(Source);
        
        if (existingFiles.Length == 0 && missingFiles.Length == 0)
        {
            _console.WriteWarning($"No files matched pattern: {Source}");
            return 0;
        }

        if (existingFiles.Length == 0 && missingFiles.Length > 0)
        {
            _console.WriteError($"File not found: {missingFiles[0]}");
            return 1;
        }

        int successCount = 0;
        int failureCount = 0;

        if (existingFiles.Length == 1 && missingFiles.Length == 0)
        {
            // Single file - use threshold-based progress
            await UploadSingleFileAsync(existingFiles[0], Destination, cancellationToken);
            successCount = 1;
        }
        else
        {
            // Multi-file - concurrent uploads with full progress display
            (successCount, failureCount) = await UploadMultipleFilesAsync(
                existingFiles, Destination, cancellationToken);
        }

        // Summary output
        if (existingFiles.Length == 1 && failureCount == 0)
            _console.WriteLine($"Uploaded {Path.GetFileName(existingFiles[0])} to {Destination}");
        else
        {
            _console.WriteLine($"Uploaded {successCount} of {existingFiles.Length} files to {Destination}");
            if (missingFiles.Length > 0)
                _console.WriteWarning($"{missingFiles.Length} files not found: {string.Join(", ", missingFiles)}");
        }

        return failureCount > 0 || missingFiles.Length > 0 ? 1 : 0;
    }

    private (string[] existing, string[] missing) ExpandSource(string source)
    {
        if (source.Contains('*') || source.Contains('?'))
        {
            var directory = Path.GetDirectoryName(source);
            if (string.IsNullOrEmpty(directory))
                directory = Directory.GetCurrentDirectory();
            
            var pattern = Path.GetFileName(source);
            var files = Directory.GetFiles(directory, pattern);
            return (files, Array.Empty<string>());
        }

        // Literal path - check existence
        if (File.Exists(source))
            return (new[] { source }, Array.Empty<string>());

        return (Array.Empty<string>(), new[] { source });
    }
}
```

### Multi-File Concurrent Upload with Progress

```csharp
private async Task<(int success, int failure)> UploadMultipleFilesAsync(
    string[] files, string destination, CancellationToken ct)
{
    int successCount = 0;
    int failureCount = 0;
    var semaphore = new SemaphoreSlim(MaxConcurrency); // Configurable concurrency

    await AnsiConsole.Progress()
        .Columns(new ProgressColumn[]
        {
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new SpinnerColumn()
        })
        .StartAsync(async ctx =>
        {
            // Create all tasks upfront showing "Pending"
            var uploadTasks = files.Select(file =>
            {
                var task = ctx.AddTask(Path.GetFileName(file), maxValue: 100);
                task.Description = $"{Path.GetFileName(file)} [grey]Pending[/]";
                return (file, task);
            }).ToList();

            var tasks = uploadTasks.Select(async item =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    item.task.Description = Path.GetFileName(item.file);
                    var destPath = Path.Combine(destination, Path.GetFileName(item.file));
                    
                    await _fileTransferService.UploadFile(
                        item.file,
                        destPath,
                        progress => item.task.Value = progress * 100,
                        ct);

                    item.task.Value = 100;
                    item.task.Description = $"{Path.GetFileName(item.file)} [green]Completed[/]";
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception)
                {
                    item.task.Description = $"{Path.GetFileName(item.file)} [red]Failed[/]";
                    Interlocked.Increment(ref failureCount);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        });

    return (successCount, failureCount);
}
```

### Single-File Progress Integration

Use existing `FileTransferService.UploadFile` with progress callback:

```csharp
private async Task UploadSingleFileAsync(string localPath, string remotePath, CancellationToken ct)
{
    var fileInfo = new FileInfo(localPath);
    var destPath = Directory.Exists(remotePath) 
        ? Path.Combine(remotePath, fileInfo.Name) 
        : remotePath;

    if (fileInfo.Length >= 1_000_000) // 1MB threshold
    {
        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            })
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(fileInfo.Name, maxValue: 100);
                await _fileTransferService.UploadFile(
                    localPath, 
                    destPath,
                    progress => task.Value = progress * 100,
                    ct);
                task.Value = 100;
            });
    }
    else
    {
        await _fileTransferService.UploadFile(localPath, destPath, null, ct);
    }
}
```

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Source file doesn't exist (single) | Error: "File not found: {path}", return 1 |
| Glob matches zero files | Warning: "No files matched pattern: {pattern}", return 0 |
| Not connected to server | Error: "Not connected to server", return 1 |
| Destination is directory | Append source filename to destination |
| Destination is file path | Use destination as-is (single file only) |
| Upload interrupted | Propagate cancellation, partial file may exist on server |
| Permission denied (local) | Error with underlying message |
| Permission denied (remote) | Error with underlying message |
| One of multiple files fails | Mark as Failed in progress, continue, summarize |
| Shell glob not quoted | Shell expands args, command receives wrong arg count, error |
| Missing files in multi-upload | Exclude from progress display, include in summary |
| File exists on server (default) | Overwrite existing file |
| File exists on server (--skip-existing) | Skip upload, show warning, include in summary |
| All files skipped (--skip-existing) | Success with message: "0 files uploaded. X skipped." |
| Recursive glob `**/*.txt` | Match files in all subdirectories |
| Recursive with no matches | Warning: "No files matched pattern: {pattern}", return 0 |

## Success Criteria

1. ✅ Single file upload works with literal path
2. ✅ Glob patterns expand correctly in REPL (including `**` recursive)
3. ✅ Progress bar displays for single files >= 1MB
4. ✅ No progress bar for single files < 1MB
5. ✅ Multi-file uploads always show progress table with all files
6. ✅ Multi-file progress shows Pending → percentage → Completed states
7. ✅ Missing files excluded from progress, included in summary
8. ✅ Concurrent uploads work correctly
9. ✅ Appropriate error messages for all failure cases
10. ✅ Multi-file upload continues on individual failures
11. ✅ Command registered under `server` group
12. ✅ Works when not connected (shows connection error)
13. ✅ `--skip-existing` flag skips files that exist on server
14. ✅ Default behavior overwrites existing server files

## Dependencies

- `SignalRServerProxy` - Connection state check
- `FileTransferService` - File upload with progress, batch exists check
- `Spectre.Console` - Progress bar display
- `ServerGroup` - Command group registration
- `Microsoft.Extensions.FileSystemGlobbing` - Glob pattern expansion

## Out of Scope

- Resume interrupted uploads
- Checksum verification
- Compression during transfer
