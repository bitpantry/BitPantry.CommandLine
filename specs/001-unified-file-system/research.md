# Research: Unified File System Abstraction

**Date**: 2024-12-22  
**Feature**: 001-unified-file-system

## Overview

Research to support implementation decisions for unified file system abstraction and file transfer security hardening.

---

## Decision 1: File System Abstraction Library

**Decision**: Use `System.IO.Abstractions` (TestableIO) as the unified file abstraction.

**Rationale**:
- Well-established library with 1.6k GitHub stars and 3.9k dependents
- Provides complete `IFileSystem` interface matching `System.IO` API exactly
- Includes `MockFileSystem` for unit testing (in TestingHelpers package)
- Interface design allows custom implementations (our `SandboxedFileSystem`)
- Active maintenance with .NET 8/9/10 support
- MIT licensed

**Package Names**:
- `TestableIO.System.IO.Abstractions.Wrappers` - runtime implementation
- `TestableIO.System.IO.Abstractions.TestingHelpers` - test mocking

**Alternatives Considered**:
- Custom `IFileService` (current): Limited API surface, no directory operations, not standard
- `Microsoft.Extensions.FileProviders`: Read-only, designed for static content serving
- Build custom `IFileSystem`: Reinventing the wheel; testing burden

---

## Decision 2: Local vs Remote Access Model

**Decision**: Local execution has unrestricted access; remote execution is sandboxed to `StorageRootPath`.

**Rationale**:
- Commands should work identically in both modes from a code perspective
- Local execution needs full disk access for legitimate use cases
- Remote execution must be sandboxed for security (untrusted client input)
- Path validation happens server-side, not client-side
- Client paths are always relative; server resolves to absolute

**Implementation**:
- Local: Register `FileSystem` (library default) - wraps `System.IO` directly
- Remote: Register `SandboxedFileSystem` (custom) - routes to server

---

## Decision 3: Checksum Algorithm and Implementation

**Decision**: Use SHA256 with `IncrementalHash.CreateHash(HashAlgorithmName.SHA256)` for incremental hashing during streaming.

**Rationale**:
- SHA256 is cryptographically secure and widely accepted for integrity verification
- `IncrementalHash` is built into .NET (`System.Security.Cryptography`) - no external dependencies
- Incremental hashing allows computing hash chunk-by-chunk during streaming without buffering entire file
- Matches existing 80KB chunk size used in `FileTransferEndpointService`

**Alternatives Considered**:
- MD5: Faster but cryptographically broken; rejected for security reasons
- SHA1: Also considered insecure; rejected
- Pre-compute hash before upload: Would require reading file twice; rejected for performance
- Third-party library (xxHash, Blake3): Faster but adds dependency; rejected for simplicity

---

## Decision 4: HTTP vs SignalR for Operations

**Decision**: HTTP for streaming (file content), SignalR RPC for metadata operations.

**Rationale**:
- SignalR has 32KB default message buffer limit; increasing it "may reduce concurrent connections" per Microsoft docs
- HTTP streaming is purpose-built for large binary transfers with proper backpressure
- Metadata operations (Exists, GetInfo, Enumerate) are small payloads - SignalR RPC is efficient
- Existing infrastructure supports both: HTTP endpoints + SignalR hub
- Progress updates via SignalR work well (already implemented)

**Transport Mapping**:
| Operation Type | Transport | Reason |
|----------------|-----------|--------|
| File read/write content | HTTP | Streaming, large payloads |
| File exists/delete/copy | SignalR RPC | Small payloads, simple request/response |
| Directory operations | SignalR RPC | Small payloads |
| Progress updates | SignalR push | Real-time, already implemented |

---

## Decision 5: Path Validation Strategy

**Decision**: Use `Path.GetFullPath()` comparison to validate destination paths stay within storage root.

**Rationale**:
- `Path.GetFullPath(Path.Combine(root, userPath))` resolves all `..` sequences
- Compare result with `Path.GetFullPath(root)` using `StartsWith()` check
- Handles both relative and absolute path injection attempts
- Cross-platform (Windows/Linux path handling)
- Per OWASP guidance: "normalize the input before using in file io API's"

**Implementation**:
```csharp
private string ValidatePath(string relativePath)
{
    var fullPath = Path.GetFullPath(Path.Combine(_options.StorageRootPath, relativePath));
    var normalizedRoot = Path.GetFullPath(_options.StorageRootPath);
    
    // Ensure path separator at end to prevent "C:\storage" matching "C:\storage-other"
    if (!fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar) 
        && fullPath != normalizedRoot)
    {
        throw new UnauthorizedAccessException("Path traversal attempt detected");
    }
    
    return fullPath;
}
```

**Alternatives Considered**:
- Regex-based `../` detection: Can be bypassed with URL encoding or alternate sequences
- Chroot/sandbox: OS-level; too heavy for this use case
- Blacklist approach: Always incomplete; allowlist (StartsWith) is more secure

---

## Decision 6: Token Transmission

**Decision**: Move access token from URL query string to `Authorization: Bearer` header.

**Rationale**:
- Query string tokens are logged by web servers (security risk per Microsoft SignalR security docs)
- `Authorization` header is standard for Bearer token transmission
- Server endpoint can read from `HttpContext.Request.Headers.Authorization`
- Client uses `HttpClient.DefaultRequestHeaders.Authorization`

**Alternatives Considered**:
- Custom header (X-Access-Token): Non-standard; `Authorization` is preferred
- Cookie-based: Adds CSRF concerns; current approach is simpler

---

## Decision 7: RPC Pattern for Metadata Operations

**Decision**: Use existing `RpcMessageRegistry` + `MessageBase` envelope pattern.

**Rationale**:
- Already implemented and tested in codebase
- Provides correlation ID management for request/response matching
- Handles timeouts and errors consistently
- No new infrastructure needed

**Existing Pattern (from codebase)**:
```csharp
// Client side
var ctx = _rpcRegistry.Register();
var request = new SomeRequest { CorrelationId = ctx.CorrelationId };
await _connection.SendAsync(SignalRMethodNames.ReceiveRequest, request);
var response = await ctx.WaitForCompletion<SomeResponse>();

// Server side (in ServerLogic.cs)
case "SomeRequest":
    var result = ProcessRequest(request);
    await proxy.SendAsync(SignalRMethodNames.ReceiveResponse, new SomeResponse(request.CorrelationId, result));
```

---

## Decision 8: Async/Sync Method Handling

**Decision**: `SandboxedFileSystem` blocks on async operations for sync method implementations.

**Rationale**:
- `IFile` interface has both sync (`ReadAllText`) and async (`ReadAllTextAsync`) methods
- All remote operations are inherently async (HTTP/SignalR)
- Sync methods must block; use `.GetAwaiter().GetResult()` pattern
- Async methods pass through directly
- This matches how library's `FileSystem` works (wraps sync `System.IO` for both)

**Consideration**:
- May cause deadlocks if called from UI thread with synchronization context
- Command execution is typically on background threads; acceptable trade-off
- Document that async methods are preferred for remote operations

---

## Decision 9: File Size Validation Approach

**Decision**: Dual validation - pre-flight Content-Length check plus streaming byte count.

**Rationale**:
- Pre-flight rejection saves bandwidth by aborting before transfer begins
- Streaming validation catches truncated or falsified Content-Length headers
- Both checks needed for defense in depth

**Implementation**:
- Check `HttpContext.Request.ContentLength` against `MaxFileSizeBytes` before streaming
- Track `totalBytesRead` during stream loop; abort if exceeds limit

---

## Decision 10: IFileSystem Registration Lifecycle

**Decision**: Swap `IFileSystem` registration in DI container on connect/disconnect.

**Rationale**:
- Commands get `IFileSystem` via constructor injection
- Need to switch between `FileSystem` (local) and `SandboxedFileSystem` (remote)
- DI container supports replacing registrations at runtime

**Implementation Approach**:
- Use scoped registration with factory that checks connection state
- Or: Re-register on connect/disconnect (if container supports)
- Or: Wrapper that delegates to current implementation

**Preferred**: Factory approach
```csharp
services.AddScoped<IFileSystem>(sp =>
{
    var serverProxy = sp.GetRequiredService<IServerProxy>();
    if (serverProxy.IsConnected)
        return sp.GetRequiredService<SandboxedFileSystem>();
    return new FileSystem();
});
```
