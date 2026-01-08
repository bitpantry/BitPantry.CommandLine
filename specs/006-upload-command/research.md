# Research: Upload Command

> Phase 0 output - resolving technical decisions and best practices

## Research Tasks

### RT-001: Optimal Concurrent Upload Parallelism

**Question**: What is the optimal degree of parallelism for concurrent file uploads?

**Research Findings**:
- Network I/O is typically the bottleneck, not CPU
- Too many concurrent connections can saturate bandwidth and cause timeouts
- Cloud storage providers (AWS S3, Azure Blob) recommend 4-8 concurrent operations
- Local network transfers may benefit from higher parallelism
- HTTP/2 multiplexing reduces connection overhead but server must support it

**Decision**: Default to **4 concurrent uploads**
**Rationale**: Conservative default that works well across network conditions; prevents overwhelming server resources; can be made configurable in future if needed
**Alternatives Considered**: 
- 1 (sequential): Too slow for batch operations
- 8-16: Risk of timeouts on slower connections
- Configurable parameter: Added complexity for v1; defer to future enhancement

---

### RT-002: Spectre.Console Progress Pattern for Concurrent Tasks

**Question**: Best practice for displaying multiple concurrent progress bars with Spectre.Console?

**Research Findings**:
- `AnsiConsole.Progress()` supports multiple `ProgressTask` instances
- Tasks can be added with `ctx.AddTask()` before or during execution
- Task value can be updated from any thread safely
- Task description can include Spectre markup for status coloring
- `IsIndeterminate` property available for tasks without known size

**Decision**: Create all tasks upfront with "Pending" state, update concurrently
**Rationale**: Provides immediate visual feedback of full workload; user sees total file count before transfers begin
**Alternatives Considered**:
- Add tasks dynamically as uploads start: Less predictable UI, harder to estimate completion

**Implementation Pattern**:
```csharp
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
        var tasks = files.Select(f => (file: f, task: ctx.AddTask($"{f} [grey]Pending[/]"))).ToList();
        // Execute with semaphore for concurrency control
    });
```

---

### RT-003: Glob Pattern Expansion in C#

**Question**: Best approach for client-side glob pattern expansion, including recursive patterns?

**Research Findings**:
- `Directory.GetFiles(path, pattern)` supports `*` and `?` wildcards (single-level only)
- `**` recursive pattern NOT supported by `Directory.GetFiles`
- `Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories)` supports recursion but limited pattern syntax
- **Microsoft.Extensions.FileSystemGlobbing** NuGet provides full glob syntax

**Options Evaluated**:

| Option | Complexity | Pros | Cons |
|--------|------------|------|------|
| A. `Directory.GetFiles` | Low | Built-in, no dependencies | No `**` support |
| B. `Microsoft.Extensions.FileSystemGlobbing` | Medium | Full glob syntax, battle-tested | New NuGet dependency |
| C. Custom recursive walker | High | Full control | Reinventing the wheel |

**Decision**: Use **Microsoft.Extensions.FileSystemGlobbing** for full glob support
**Rationale**: Enables `**` recursive patterns; Microsoft-maintained; small dependency; standard glob syntax users expect

**Implementation Pattern**:
```csharp
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

private string[] ExpandGlob(string source)
{
    // Detect if pattern contains glob characters
    if (!source.Contains('*') && !source.Contains('?'))
    {
        return File.Exists(source) ? new[] { source } : Array.Empty<string>();
    }

    // Parse base directory and pattern
    var (baseDir, pattern) = ParseGlobPattern(source);
    
    var matcher = new Matcher();
    matcher.AddInclude(pattern);
    
    var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDir)));
    return result.Files.Select(f => Path.Combine(baseDir, f.Path)).ToArray();
}

private (string baseDir, string pattern) ParseGlobPattern(string source)
{
    // Find the first segment with wildcards
    var segments = source.Replace('\\', '/').Split('/');
    var baseSegments = new List<string>();
    var patternSegments = new List<string>();
    bool inPattern = false;
    
    foreach (var seg in segments)
    {
        if (inPattern || seg.Contains('*') || seg.Contains('?'))
        {
            inPattern = true;
            patternSegments.Add(seg);
        }
        else
        {
            baseSegments.Add(seg);
        }
    }
    
    var baseDir = baseSegments.Count > 0 
        ? string.Join(Path.DirectorySeparatorChar.ToString(), baseSegments)
        : Directory.GetCurrentDirectory();
    var pattern = string.Join("/", patternSegments);
    
    return (baseDir, pattern);
}
```

**Supported Patterns**:
- `*.txt` - All .txt files in current directory
- `**/*.txt` - All .txt files recursively
- `logs/**/*.log` - All .log files under logs/ recursively
- `data?.json` - data1.json, dataA.json, etc.

---

### RT-004: FileTransferService Integration

**Question**: How does the existing FileTransferService support progress callbacks?

**Research Findings** (from codebase analysis):
- `FileTransferService.UploadFile()` accepts `Func<FileUploadProgress, Task>` callback
- `FileUploadProgress` is a record: `record FileUploadProgress(long TotalRead, string Error = null)`
- Progress is reported as bytes read, not percentage
- Must calculate percentage from `fileSize` vs `TotalRead`

**Decision**: Use existing FileTransferService API; calculate percentage in command
**Rationale**: Leverages proven infrastructure; no changes to core service needed
**Alternatives Considered**:
- Modify FileTransferService to report percentage: Breaking change; unnecessary coupling

**Implementation Pattern**:
```csharp
var fileSize = new FileInfo(filePath).Length;
await _fileTransferService.UploadFile(
    filePath,
    destPath,
    async progress => 
    {
        var percentage = (double)progress.TotalRead / fileSize * 100;
        progressTask.Value = percentage;
    },
    cancellationToken);
```

---

### RT-005: Error Handling Strategy for Multi-File Uploads

**Question**: How to handle partial failures in concurrent multi-file uploads?

**Research Findings**:
- Fail-fast approach abandons remaining files on first error
- Continue-on-error allows maximum files to transfer
- User needs visibility into which files failed and why
- Cloud CLI tools (az, aws, gcloud) typically continue on individual failures

**Decision**: Continue-on-error with failure summary
**Rationale**: Maximizes successful transfers; matches user expectations from cloud CLIs; provides actionable error information
**Alternatives Considered**:
- Fail-fast: Frustrating for users with large batches; one bad file stops everything

**Implementation Pattern**:
```csharp
try 
{
    await UploadSingleFile(file, destination, progress, ct);
    Interlocked.Increment(ref successCount);
    task.Description = $"{fileName} [green]Completed[/]";
}
catch (Exception ex)
{
    Interlocked.Increment(ref failureCount);
    failedFiles.Add((file, ex.Message));
    task.Description = $"{fileName} [red]Failed[/]";
}
```

---

## Summary

All technical decisions resolved. No NEEDS CLARIFICATION items remain.

| Topic | Decision | Rationale |
|-------|----------|-----------|
| Parallelism | 4 concurrent uploads | Conservative default for network I/O |
| Progress UI | Upfront task creation, concurrent updates | Immediate visibility of workload |
| Glob expansion | Microsoft.Extensions.FileSystemGlobbing | Full `**` recursive support |
| FileTransferService | Use existing API | Proven infrastructure |
| Error handling | Continue-on-error | Maximize successful transfers |
| Skip existing | Batch exists check via REST | Efficient, saves bandwidth |

---

### RT-006: Skip Existing Files - Server Check Strategy

**Question**: How to implement `--skip-existing` flag to avoid overwriting server files?

**Research Findings**:
- Need to check if destination file exists on server before uploading
- Options: per-file HEAD request, batch POST check, or server-side reject

**Options Evaluated**:

| Approach | How It Works | Pros | Cons |
|----------|--------------|------|------|
| A. Per-file HEAD request | HEAD to download endpoint | Reuses existing endpoint | N round-trips for N files |
| B. Batch POST check | POST list of paths, get existence map | Single round-trip | New endpoint needed |
| C. Server-side reject | Upload starts, server rejects if exists | Single request | Bandwidth wasted on rejection |

**Decision**: **Option B** - Batch existence check via REST endpoint
**Rationale**: Most efficient for multi-file uploads; single round-trip regardless of file count; saves bandwidth by not sending file data for skipped files. 

### Batch Chunking Strategy

**Threshold**: 100 files per batch request (configurable constant)
**Rationale**: Aligns with industry practices (AWS S3: 1000, Azure Blob: 5000). Conservative value balances:
- Request size limits (~8KB per request assuming 80-char filenames)
- Server memory pressure
- Timeout risk on large batches

**Implementation**:
```csharp
private const int BATCH_EXISTS_CHUNK_SIZE = 100;

private async Task<Dictionary<string, bool>> CheckFilesExistBatched(
    string directory, string[] filenames, CancellationToken ct)
{
    var result = new Dictionary<string, bool>();
    
    foreach (var chunk in filenames.Chunk(BATCH_EXISTS_CHUNK_SIZE))
    {
        var chunkResult = await _fileTransferService.CheckFilesExist(
            directory, chunk.ToArray(), ct);
        foreach (var kvp in chunkResult)
            result[kvp.Key] = kvp.Value;
    }
    
    return result;
}
```

### Server-Side skipIfExists Enforcement (TOCTOU Mitigation)

**Problem**: Race condition between batch check and upload - another client could upload the same file.

**Solution**: Pass `skipIfExists` parameter with each upload request. Server becomes final arbiter:

| Scenario | skipIfExists=true | skipIfExists=false |
|----------|-------------------|--------------------|
| File exists on server | Skip, return success with "skipped" status | Overwrite file |
| File does not exist | Upload normally | Upload normally |

**Upload Endpoint Enhancement**:
```
POST /{ServiceEndpointNames.FileUpload}?...&skipIfExists=true
```

**Server Logic**:
```csharp
if (skipIfExists && File.Exists(destinationPath))
{
    // File appeared after client's batch check - honor skipIfExists
    return Results.Ok(new { Status = "skipped", Reason = "File already exists" });
}
// Proceed with upload/overwrite
```

**Benefits**:
- Mitigates TOCTOU race condition at server level
- Consistent semantics regardless of timing
- No wasted bandwidth (file data already in flight)
- Graceful handling vs. error

**Endpoint Design**:
```
POST /files/exists
Content-Type: application/json
Authorization: Bearer {token}

{
  "directory": "/remote/path",
  "filenames": ["file1.txt", "file2.txt", "file3.txt"]
}

Response 200:
{
  "exists": {
    "file1.txt": true,
    "file2.txt": false,
    "file3.txt": true
  }
}
```

**Client Flow**:
```csharp
if (skipExisting && files.Length > 0)
{
    var existsMap = await _fileTransferService.CheckFilesExist(destination, files);
    var toSkip = files.Where(f => existsMap[Path.GetFileName(f)]).ToList();
    var toUpload = files.Where(f => !existsMap[Path.GetFileName(f)]).ToArray();
    
    foreach (var skipped in toSkip)
        _console.WriteWarning($"Skipped (exists): {Path.GetFileName(skipped)}");
    
    files = toUpload;
}
```
