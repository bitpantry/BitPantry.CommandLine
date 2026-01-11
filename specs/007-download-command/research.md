# Research: Download Command

**Date**: 2026-01-10  
**Feature**: 007-download-command

## Research Tasks

### RT-001: Validate Infrastructure Gap Analysis

**Question**: Do the identified gaps (GAP-001 through GAP-005) accurately represent what needs to be built?

**Finding**: Yes, validated by code review:

| Gap | Validation |
|-----|------------|
| GAP-001 | `EnumerateFilesResponse` confirmed to return `string[]` only - no size info |
| GAP-002 | `FileTransferService.DownloadFile()` has no progress callback parameter |
| GAP-003 | Server reads entire file with `ReadAllBytesAsync()` before sending - no streaming |
| GAP-004 | No server-side glob expansion - client must enumerate and filter |
| GAP-005 | `GetFileInfoRequest` is single-file only - confirmed in contract |

**Decision**: All gaps are valid and require implementation.

---

### RT-002: Streaming Download with Progress - Best Practices

**Question**: How should large file downloads be streamed with progress updates?

**Alternatives Considered**:

| Option | Pros | Cons |
|--------|------|------|
| A. SignalR progress via separate channel | Consistent with upload pattern | Requires registry coordination |
| B. HTTP chunked transfer with Content-Length | Simple, standard HTTP | No real-time progress callbacks |
| C. Server-Sent Events (SSE) | Real-time, one-way | Separate connection, complexity |

**Decision**: Option A - SignalR progress via separate channel

**Rationale**: 
- Mirrors the existing upload progress pattern exactly
- Reuses `FileUploadProgressUpdateFunctionRegistry` pattern
- Client already has SignalR connection established
- Consistent developer experience across upload/download

**Implementation Pattern**:
```csharp
// Server-side: Stream file in chunks, send progress via SignalR
const int ChunkSize = 81920; // 80KB chunks
var buffer = new byte[ChunkSize];
int bytesRead;
long totalRead = 0;

while ((bytesRead = await fileStream.ReadAsync(buffer, 0, ChunkSize, token)) > 0)
{
    await responseStream.WriteAsync(buffer, 0, bytesRead, token);
    totalRead += bytesRead;
    
    // Send progress via SignalR (throttled to ~1/second)
    if (ShouldSendProgress(lastProgressTime))
    {
        await SendProgressMessage(client, correlationId, totalRead, fileSize);
    }
}
```

---

### RT-003: Batch File Enumeration with Size Information

**Question**: Should batch file info be a new RPC message or extend existing?

**Alternatives Considered**:

| Option | Pros | Cons |
|--------|------|------|
| A. Replace `EnumerateFilesRequest/Response` entirely | Single flow, simpler API | Must remove old version |
| B. Add `includeInfo` flag to existing `EnumerateFilesRequest` | Fewer message types | Mixed return types, backward compat complexity |
| C. Client calls `GetFileInfo` for each file after enumerate | No server changes | N+1 problem, slow for large file sets |

**Decision**: Option A - Replace existing `EnumerateFilesRequest/Response` entirely

**Rationale**:
- **No backward compatibility required** - clean replacement is preferred
- Single enumeration flow that always returns full file info
- Simpler API surface (one flow instead of two)
- The old `EnumerateFilesResponse` returning `string[]` only is insufficient - remove it

**Actions**:
- **REMOVE**: Existing `EnumerateFilesRequest` and `EnumerateFilesResponse` that return `string[]`
- **CREATE**: New `EnumerateFilesRequest` and `EnumerateFilesResponse` with file info
- **KEEP**: Same message names for simplicity (`EnumerateFilesRequest`/`EnumerateFilesResponse`)
- **KEEP**: `GetFileInfoRequest` for single-file lookups (different use case)

**Response Structure**:
```csharp
public record FileInfoEntry(
    string Path,           // Relative path from search root
    long Size,             // File size in bytes
    DateTime LastModified  // Last write time UTC
);

// Replaces existing EnumerateFilesResponse - same name, enhanced content
public record EnumerateFilesResponse(
    FileInfoEntry[]? Files,
    string? Error
) : MessageBase;
```

---

### RT-004: Glob Pattern Expansion Location

**Question**: Should glob patterns be expanded client-side or server-side?

**Alternatives Considered**:

| Option | Pros | Cons |
|--------|------|------|
| A. Client-side (call enumerate, then filter) | Simpler server | Multiple RPC calls, inefficient |
| B. Server-side (server applies glob) | Single RPC call | Server must understand glob syntax |

**Decision**: Option B - Server-side glob expansion

**Rationale**:
- Single RPC call for pattern + file info
- Server already has `Microsoft.Extensions.FileSystemGlobbing` available
- Reduces network round trips
- Consistent with how upload command expands patterns locally

**Server Implementation**:
```csharp
var matcher = new Matcher();
matcher.AddInclude(searchPattern);
var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(basePath)));

var files = result.Files
    .Select(f => Path.Combine(basePath, f.Path))
    .Select(p => new FileInfoEntry(
        Path: f.Path,
        Size: new FileInfo(p).Length,
        LastModified: File.GetLastWriteTimeUtc(p)
    ))
    .ToArray();
```

---

### RT-005: Filename Collision Detection Strategy

**Question**: How should filename collisions be detected efficiently?

**Finding**: Since files are flattened to destination directory, collision detection is straightforward:

```csharp
var filenames = files.Select(f => Path.GetFileName(f.Path));
var duplicates = filenames
    .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key)
    .ToList();

if (duplicates.Any())
{
    // Find all paths that would collide
    var collisions = files
        .Where(f => duplicates.Contains(Path.GetFileName(f.Path), StringComparer.OrdinalIgnoreCase))
        .GroupBy(f => Path.GetFileName(f.Path), StringComparer.OrdinalIgnoreCase);
    
    // Report all colliding paths
    foreach (var group in collisions)
    {
        _console.MarkupLine($"[red]Collision: {group.Key}[/]");
        foreach (var file in group)
            _console.MarkupLine($"  - {file.Path}");
    }
}
```

**Decision**: Detect collisions BEFORE any downloads start by grouping by filename.

**Cross-Platform Note**: Collision detection uses `StringComparer.OrdinalIgnoreCase` intentionally for safety across all platform combinations:
- If server is Linux (case-sensitive) and client is Windows (case-insensitive), files `File.txt` and `file.txt` would collide on Windows
- Case-insensitive detection prevents data loss regardless of client/server platform combination
- This is a conservative approach that may flag "false" collisions on Linux clients, but ensures safety

---

### RT-006: Progress Display Threshold

**Question**: What threshold should trigger progress display?

**Finding**: Upload command uses `UploadConstants.ProgressDisplayThreshold`:

```csharp
// From UploadConstants.cs
public const long ProgressDisplayThreshold = 25 * 1024 * 1024; // 25 MB
```

**Decision**: Use same threshold via `DownloadConstants.ProgressDisplayThreshold` for consistency.

**Note**: All documentation references the constant name, not the hardcoded value. If the threshold changes, only the constant definition needs updating.

---

### RT-007: Concurrent Download Limit

**Question**: What should the default concurrency limit be?

**Finding**: Upload command uses:
```csharp
// From UploadConstants.cs
public const int MaxConcurrentUploads = 4;
```

**Decision**: Use same limit via `DownloadConstants.MaxConcurrentDownloads` for consistency.

**Note**: All documentation references the constant name, not the hardcoded value. If the limit changes, only the constant definition needs updating.

**Rationale**:
- Consistent user experience with upload command
- Balances throughput with server load
- Prevents overwhelming local disk I/O

---

### RT-008: Partial File Cleanup on Failure

**Question**: How should partial files be handled when download fails mid-transfer?

**Alternatives Considered**:

| Option | Pros | Cons |
|--------|------|------|
| A. Delete partial file immediately | Clean state, no confusion | User loses partial data |
| B. Keep partial file with .partial extension | Resume possible in future | Clutters filesystem |
| C. Keep partial file as-is | Simple | Confusing, appears complete |

**Decision**: Option A - Delete partial file immediately

**Rationale**:
- Matches user expectation (no resume feature specified)
- Clean filesystem state
- Prevents confusion about incomplete files
- Simple implementation

**Implementation**:
```csharp
try
{
    await DownloadFileAsync(remotePath, localPath, progress, token);
}
catch
{
    // Clean up partial file on any failure
    if (_fileSystem.File.Exists(localPath))
        _fileSystem.File.Delete(localPath);
    throw;
}
```

## Summary

All research questions resolved. Key decisions:

1. **Progress mechanism**: SignalR callbacks mirroring upload pattern
2. **Batch enumeration**: Enhanced `EnumerateFilesRequest/Response` (replaces existing, same name)
3. **Glob expansion**: Server-side using existing Matcher library
4. **Collision detection**: Pre-flight check before any downloads
5. **Thresholds**: Reference `DownloadConstants` (mirrors `UploadConstants` values)
6. **Partial files**: Delete on failure (no resume support)
7. **No backward compatibility**: Replace existing flows entirely, remove deprecated code

> **Important**: Documentation references constant names (e.g., `DownloadConstants.ProgressDisplayThreshold`) rather than hardcoded values. This ensures docs remain accurate if thresholds change.
