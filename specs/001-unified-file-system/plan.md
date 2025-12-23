# Implementation Plan: Unified File System Abstraction

**Branch**: `001-unified-file-system` | **Date**: 2024-12-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-unified-file-system/spec.md`

## Summary

Adopt `System.IO.Abstractions` (`IFileSystem`) as the unified file abstraction for all command file operations. 

**Architecture (Corrected):**
- **Client-side**: Always uses unrestricted `FileSystem` wrapper. The client NEVER swaps `IFileSystem` based on connection state.
- **Server-side command execution**: When commands are executed on the server on behalf of remote clients, the server's DI resolves `IFileSystem` to `SandboxedFileSystem`, which wraps local file operations with path validation to confine access to `StorageRootPath`.
- **Explicit file transfers**: The existing `FileTransferService` (client) and `FileTransferEndpointService` (server) handle explicit file upload/download operations via HTTP with SignalR progress updates.

Includes deletion of custom `IFileService`/`LocalDiskFileService`, security hardening with SHA256 checksums, proper DI integration, and cancellation support with partial file cleanup.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: 
- `TestableIO.System.IO.Abstractions.Wrappers` (NEW - file system abstraction)
- `Microsoft.AspNetCore.SignalR` (existing - RPC and progress)
- `System.Security.Cryptography` (existing - IncrementalHash for SHA256)
- `Microsoft.Extensions.DependencyInjection` (existing)

**Testing**: MSTest with FluentAssertions and Moq; `TestableIO.System.IO.Abstractions.TestingHelpers` for MockFileSystem  
**Test Approach**: TDD - Write failing tests FIRST, then implement to make them pass  
**Target Platform**: Windows/Linux server (ASP.NET Core) + .NET client applications  
**Project Type**: Multi-project library solution

**Testing Infrastructure**:
- Unit tests: `BitPantry.CommandLine.Tests/` and `BitPantry.CommandLine.Tests.Remote.SignalR/`
- Integration tests: `BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/`
- Test environment: `TestEnvironment` class with `TestServer` for client-server testing
- Pattern: Arrange/Act/Assert with explicit happy path AND exception path coverage
- Existing examples: `IntegrationTests_FileTransferService.cs`, `RpcMessageRegistryTests.cs`

**Existing Patterns to Follow**:
- `RpcMessageRegistry` + `RpcMessageContext` for SignalR RPC request/response correlation
- `MessageBase` + derived envelope classes in `Envelopes/` for RPC message structure
- `SignalRMethodNames` constants for method name strings
- `FileUploadProgressMessage` pattern for progress updates via SignalR
- HTTP endpoints registered via `IEndpointRouteBuilder.MapPost/MapGet`

**Constraints**: 
- Must use existing RPC infrastructure (no new RPC patterns)
- Must follow existing envelope/message patterns
- Breaking change acceptable: delete static extension methods and custom IFileService

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Per `.specify/memory/constitution.md`:

- ✅ **TDD (NON-NEGOTIABLE)**: Tests written first in each phase; tasks.md has 🧪 test tasks before implementation
- ✅ **Dependency Injection**: IFileSystem injected via constructor; no static methods for file operations
- ✅ **Security by Design**: Tokens in headers, path validation, size limits, security event logging
- ✅ **Follow Existing Patterns**: Uses RpcMessageRegistry, MessageBase, existing HTTP endpoint patterns
- ✅ **Integration Testing**: IntegrationTests/ folder with client-server tests
- ✅ Breaking changes are documented (IFileService deletion, static method removal)
- ✅ Follows existing DI patterns in the codebase

## Project Structure

### Documentation (this feature spec)

```text
specs/001-unified-file-system/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── file-transfer-api.md
└── checklists/          # Phase 2 output
```

### User Documentation (Phase 7 output)

```text
Docs/
├── readme.md                           # UPDATE: add file system link
└── Remote/
    ├── CommandLineServer.md            # UPDATE: add file config section
    ├── FileSystem.md                   # NEW: main IFileSystem documentation
    └── FileSystemConfiguration.md      # NEW: server-side configuration
```

### Source Code Changes

```text
BitPantry.CommandLine/
├── IFileService.cs                    # DELETE
├── LocalDiskFileService.cs            # DELETE
├── ServiceCollectionExtensions.cs     # MODIFY: register IFileSystem instead

BitPantry.CommandLine.Remote.SignalR/
├── SignalRMethodNames.cs              # MODIFY: add file system RPC methods
├── Envelopes/
│   ├── FileUploadProgressMessage.cs   # MODIFY: int → long for TotalRead
│   ├── FileExistsRequest.cs           # NEW
│   ├── FileExistsResponse.cs          # NEW
│   ├── DirectoryExistsRequest.cs      # NEW
│   ├── DirectoryExistsResponse.cs     # NEW
│   ├── GetFileInfoRequest.cs          # NEW
│   ├── GetFileInfoResponse.cs         # NEW
│   ├── EnumerateFilesRequest.cs       # NEW
│   ├── EnumerateFilesResponse.cs      # NEW
│   ├── EnumerateDirectoriesRequest.cs # NEW
│   ├── EnumerateDirectoriesResponse.cs# NEW
│   ├── CreateDirectoryRequest.cs      # NEW
│   ├── CreateDirectoryResponse.cs     # NEW
│   ├── DeleteFileRequest.cs           # NEW
│   ├── DeleteFileResponse.cs          # NEW
│   ├── DeleteDirectoryRequest.cs      # NEW
│   └── DeleteDirectoryResponse.cs     # NEW

BitPantry.CommandLine.Remote.SignalR.Client/
├── FileTransferService.cs             # MODIFY: internal, add checksum, auth header, download
├── FileUploadProgress.cs              # MODIFY: int → long
├── CommandBaseExtensions_FileTransfer.cs  # DELETE
├── FileSystemProvider.cs              # DELETE (incorrect client-side swap pattern)
├── SandboxedFileSystem.cs             # DELETE (move to server)
├── SandboxedFile.cs                   # DELETE (move to server)
├── SandboxedDirectory.cs              # DELETE (move to server)
├── SandboxedPath.cs                   # DELETE (move to server)
├── SandboxedFileInfoFactory.cs        # DELETE (move to server)
├── SandboxedDirectoryInfoFactory.cs   # DELETE (move to server)
├── ConnectCommand.cs                  # MODIFY: remove IFileSystem swap logic
├── DisconnectCommand.cs               # MODIFY: remove IFileSystem swap logic
├── CommandLineApplicationBuilderExtensions.cs  # MODIFY: simple IFileSystem registration (no swap)

BitPantry.CommandLine.Remote.SignalR.Server/
├── Files/
│   ├── FileTransferEndpointService.cs # MODIFY: add security, checksum, download
│   ├── FileTransferOptions.cs         # NEW: configuration class
│   ├── PathValidator.cs               # NEW: path traversal protection
│   ├── FileSizeValidator.cs           # NEW: size limit enforcement
│   ├── ExtensionValidator.cs          # NEW: extension whitelist enforcement
│   ├── SandboxedFileSystem.cs         # NEW: server-side IFileSystem for command execution
│   ├── SandboxedFile.cs               # NEW: server-side IFile with validation
│   ├── SandboxedDirectory.cs          # NEW: server-side IDirectory with validation
│   ├── SandboxedPath.cs               # NEW: delegates to local Path
│   ├── SandboxedFileInfoFactory.cs    # NEW: factory with path validation
│   └── SandboxedDirectoryInfoFactory.cs # NEW: factory with path validation
├── CommandLineHub.cs                  # MODIFY: route file system RPC messages
├── ServerLogic.cs                     # MODIFY: handle file system RPC, register SandboxedFileSystem in DI

BitPantry.CommandLine.Tests.Remote.SignalR/
├── IntegrationTests/
│   └── FileSystemIntegrationTests.cs  # NEW: IFileSystem local/remote tests
├── SecurityTests/
│   └── PathTraversalTests.cs          # NEW: security validation tests
```

## Implementation Phases

### Phase 1: Core Infrastructure

**1.1 Add NuGet Package**
- Add `TestableIO.System.IO.Abstractions.Wrappers` to:
  - `BitPantry.CommandLine.csproj`
  - `BitPantry.CommandLine.Remote.SignalR.Client.csproj`
- Add `TestableIO.System.IO.Abstractions.TestingHelpers` to test projects

**1.2 Delete Custom Abstractions**
- Delete `BitPantry.CommandLine/IFileService.cs`
- Delete `BitPantry.CommandLine/LocalDiskFileService.cs`
- Update `ServiceCollectionExtensions.cs` to register `IFileSystem` → `FileSystem`

**1.3 Delete Static Extension Methods**
- Delete `BitPantry.CommandLine.Remote.SignalR.Client/CommandBaseExtensions_FileTransfer.cs`

### Phase 2: Server-Side Security Hardening

**2.1 Configuration**
- Create `FileTransferOptions.cs` with:
  - `StorageRootPath` (required, no default - server fails to start if not configured)
  - `MaxFileSizeBytes` (default: 100MB / 104,857,600 bytes)
  - `AllowedExtensions` (default: null - allows all extensions; opt-in restriction)
- Wire into DI via options pattern
- Add startup validation: throw `InvalidOperationException` if `StorageRootPath` is null/empty

**2.2 Path Validation**
- Implement `ValidatePath()` in `FileTransferEndpointService`:
  ```csharp
  var fullPath = Path.GetFullPath(Path.Combine(StorageRootPath, userPath));
  var normalizedRoot = Path.GetFullPath(StorageRootPath);
  if (!fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar))
      throw new UnauthorizedAccessException("Path traversal attempt");
  ```

**2.3 Upload Endpoint Hardening**
- Move token from query string to `Authorization: Bearer` header
- Add `X-File-Checksum` header handling
- Implement incremental SHA256 verification during streaming
- Add size limit checking (pre-flight + streaming)
- Add extension validation
- Add partial file cleanup on error/cancellation
- Change `TotalRead` from `int` to `long`

**2.4 Download Endpoint**
- Create `GET /cli/filedownload` endpoint
- Include `X-File-Checksum` response header
- Stream file content with path validation

### Phase 3: SignalR RPC for Metadata Operations

**3.1 Message Envelopes**
Create request/response envelope classes following `MessageBase` pattern:
- `FileExistsRequest/Response`
- `DirectoryExistsRequest/Response`
- `GetFileInfoRequest/Response`
- `EnumerateFilesRequest/Response` (with path, searchPattern, searchOption)
- `EnumerateDirectoriesRequest/Response`
- `CreateDirectoryRequest/Response`
- `DeleteFileRequest/Response`
- `DeleteDirectoryRequest/Response` (with recursive flag)

**3.2 Server-Side RPC Handler**
- Create `FileSystemRpcHandler.cs` to process RPC requests
- Use same path validation as HTTP endpoints
- Route through existing `ServerLogic.cs` dispatch pattern

**3.3 Hub Integration**
- Add method names to `SignalRMethodNames.cs`
- Route messages in `CommandLineHub.cs` to handler

### Phase 4: Server-Side SandboxedFileSystem

**4.1 Core Implementation**
- Create `SandboxedFileSystem : FileSystemBase` in `BitPantry.CommandLine.Remote.SignalR.Server/Files/`
- Wraps local file operations with path validation using `PathValidator`
- Confines all access to `StorageRootPath` configured via `FileTransferOptions`

**4.2 SandboxedFile : IFile**
- Validates all paths using `PathValidator` before delegating to local file system
- Uses `ExtensionValidator` for write operations
- Uses `FileSizeValidator` for write operations
- Delegates actual I/O to underlying `FileSystem` after validation

**4.3 SandboxedDirectory : IDirectory**
- Validates all paths using `PathValidator` before delegating to local file system
- All operations work on local file system within `StorageRootPath`

**4.4 SandboxedPath : IPath**
- Delegate to local `Path` class (path manipulation is string operations)

**4.5 Factory Classes**
- `SandboxedFileInfoFactory`, `SandboxedDirectoryInfoFactory` - validate paths and return info for files within sandbox

### Phase 5: DI Registration

**5.1 Client-Side Registration**
- Register `IFileSystem` → `FileSystem` (singleton) in `ServiceCollectionExtensions.cs`
- Client NEVER changes this registration based on connection state
- Remove `FileSystemProvider` swap pattern

**5.2 Server-Side Registration**
- Register `IFileSystem` → `SandboxedFileSystem` (scoped) in server DI for command execution context
- `SandboxedFileSystem` receives `FileTransferOptions` for `StorageRootPath`
- `SandboxedFileSystem` receives validators (`PathValidator`, `FileSizeValidator`, `ExtensionValidator`)

### Phase 6: Testing Infrastructure Reference

> **NOTE**: This phase documents the testing approach and test inventory. Per TDD principles, 
> tests are written FIRST within each implementation phase (see tasks.md where 🧪 test tasks 
> precede implementation tasks). This section serves as a reference for test organization and 
> coverage expectations, not as a sequential phase that occurs after implementation.

**TDD Workflow**: For each feature, write failing tests FIRST, then implement code to make them pass.

**6.1 Unit Tests - Server Side** (`BitPantry.CommandLine.Tests.Remote.SignalR/ServerTests/`)

Path Validation Tests (`PathValidationTests.cs`):
- `ValidatePath_RelativePathWithinRoot_ReturnsFullPath` - happy path
- `ValidatePath_PathTraversalWithDotDot_ThrowsUnauthorizedAccessException`
- `ValidatePath_AbsolutePathOutsideRoot_ThrowsUnauthorizedAccessException`
- `ValidatePath_EncodedTraversalSequence_ThrowsUnauthorizedAccessException`
- `ValidatePath_PathWithSpacesAndUnicode_ReturnsValidPath`
- `ValidatePath_NullOrEmptyPath_ThrowsArgumentException`

FileTransferOptions Validation Tests (`FileTransferOptionsTests.cs`):
- `Validate_StorageRootPathNull_ThrowsInvalidOperationException`
- `Validate_StorageRootPathEmpty_ThrowsInvalidOperationException`
- `Validate_MaxFileSizeBytesZero_ThrowsArgumentException`
- `Validate_ValidConfiguration_Succeeds`

Checksum Tests (`ChecksumTests.cs`):
- `ComputeIncrementalHash_ValidStream_ReturnsCorrectSha256`
- `ComputeIncrementalHash_EmptyStream_ReturnsEmptyFileHash`
- `VerifyChecksum_MatchingHash_ReturnsTrue`
- `VerifyChecksum_MismatchedHash_ReturnsFalse`

FileSystemRpcHandler Tests (`FileSystemRpcHandlerTests.cs`):
- `HandleFileExistsRequest_FileExists_ReturnsTrue`
- `HandleFileExistsRequest_FileNotExists_ReturnsFalse`
- `HandleFileExistsRequest_PathTraversal_ReturnsError`
- `HandleDirectoryExistsRequest_DirectoryExists_ReturnsTrue`
- `HandleEnumerateFilesRequest_WithPattern_ReturnsMatchingFiles`
- `HandleEnumerateFilesRequest_RecursiveSearch_ReturnsAllFiles`
- `HandleCreateDirectoryRequest_ValidPath_CreatesDirectory`
- `HandleDeleteFileRequest_FileExists_DeletesFile`
- `HandleDeleteFileRequest_FileNotExists_ReturnsError`
- `HandleDeleteDirectoryRequest_NonRecursive_FailsIfNotEmpty`
- `HandleDeleteDirectoryRequest_Recursive_DeletesAll`

**6.2 Unit Tests - Client Side** (`BitPantry.CommandLine.Tests.Remote.SignalR/ClientTests/`)

SandboxedFile Tests (`SandboxedFileTests.cs`):
- `Exists_FileExists_ReturnsTrue` - with mocked RPC
- `Exists_FileNotExists_ReturnsFalse`
- `Exists_RpcTimeout_ThrowsTimeoutException`
- `ReadAllText_FileExists_ReturnsContent` - with mocked HTTP
- `ReadAllText_FileNotExists_ThrowsFileNotFoundException`
- `ReadAllText_ChecksumMismatch_ThrowsIntegrityException`
- `WriteAllBytes_ValidContent_UploadsWithChecksum`
- `WriteAllBytes_SizeExceedsLimit_ThrowsException`
- `WriteAllBytes_CancellationRequested_Cancels`
- `Delete_FileExists_DeletesViaRpc`

SandboxedDirectory Tests (`SandboxedDirectoryTests.cs`):
- `Exists_DirectoryExists_ReturnsTrue`
- `CreateDirectory_ValidPath_CreatesViaRpc`
- `CreateDirectory_PathTraversal_ServerRejectsWithError`
- `EnumerateFiles_WithPattern_ReturnsMatchingFiles`
- `EnumerateFiles_Recursive_ReturnsAllFiles`
- `Delete_EmptyDirectory_Succeeds`
- `Delete_NonEmptyNonRecursive_ThrowsIOException`
- `Delete_NonEmptyRecursive_Succeeds`

FileTransferService Tests (`FileTransferServiceTests.cs`):
- `UploadFile_ValidFile_SendsChecksumHeader`
- `UploadFile_ValidFile_SendsAuthorizationHeader`
- `UploadFile_LargeFile_ReportsProgress`
- `UploadFile_ServerRejectsChecksum_ThrowsIntegrityException`
- `UploadFile_Cancelled_ThrowsTaskCancelledException`
- `DownloadFile_ValidFile_VerifiesChecksum`
- `DownloadFile_ChecksumMismatch_ThrowsIntegrityException`
- `DownloadFile_FileNotFound_ThrowsFileNotFoundException`

**6.3 Integration Tests** (`BitPantry.CommandLine.Tests.Remote.SignalR/IntegrationTests/`)

FileSystem Integration Tests (`IntegrationTests_FileSystem.cs`):
- `IFileSystem_LocalExecution_HasUnrestrictedAccess`
- `IFileSystem_RemoteExecution_UsesSandboxedFileSystem`
- `IFileSystem_AfterDisconnect_RevertsToLocalFileSystem`
- `File_WriteAndRead_RoundTrip_VerifiesIntegrity`
- `File_Upload_ProgressCallback_ReportsProgress`
- `File_LargeFile_ProgressUsesLongType`
- `Directory_CreateEnumerateDelete_FullCycle`
- `Directory_EnumerateRecursive_ReturnsAllFiles`

Security Integration Tests (`IntegrationTests_Security.cs`):
- `PathTraversal_DotDotSlash_Rejected`
- `PathTraversal_AbsolutePath_Rejected`
- `PathTraversal_EncodedSequence_Rejected`
- `ExtensionRestriction_DisallowedExtension_Rejected`
- `ExtensionRestriction_AllowedExtension_Accepted`
- `ExtensionRestriction_NullAllowList_AllAccepted`
- `SizeLimit_ExceedsLimit_Rejected`
- `SizeLimit_WithinLimit_Accepted`
- `Token_SentInHeader_NotInUrl`
- `Checksum_Mismatch_FileDeleted`
- `Checksum_Match_FilePreserved`

Cancellation Integration Tests (`IntegrationTests_Cancellation.cs`):
- `Upload_CancelledMidTransfer_PartialFileDeleted`
- `Upload_ClientDisconnects_PartialFileDeleted`
- `Upload_DiskExhausted_PartialFileDeleted`

### Phase 7: Documentation

Update existing documentation to cover the new unified file system abstraction.

**7.1 New Documentation Files**

Create `Docs/Remote/FileSystem.md`:
- Overview of IFileSystem abstraction
- Explain transparent local/remote behavior
- Show how commands inject and use IFileSystem
- Document SandboxedFileSystem for remote operations
- Include code examples for common patterns:
  - Reading/writing files
  - Checking file/directory existence
  - Enumerating files and directories
  - Creating and deleting directories
- Document progress callbacks for large file transfers
- Document checksum verification behavior

Create `Docs/Remote/FileSystemConfiguration.md`:
- Server-side `FileTransferOptions` configuration
- `MaxFileSizeBytes` - size limits
- `AllowedExtensions` - extension whitelist
- `StorageRootPath` - sandboxed root directory
- Security considerations and path validation
- Example configuration snippets

**7.2 Update Existing Documentation**

Update `Docs/Remote/CommandLineServer.md`:
- Add "Configuring File System Access" section
- Document `ConfigureFileTransfer` extension method
- Link to new `FileSystemConfiguration.md`

Update `Docs/readme.md`:
- Add "File System Access" subsection under Remote section
- Brief overview with link to `Docs/Remote/FileSystem.md`

**7.3 Documentation Structure**

```text
Docs/
├── readme.md                           # UPDATE: add file system link
├── CommandLine/
│   └── (existing files unchanged)
└── Remote/
    ├── Client.md                       # (existing)
    ├── CommandLineServer.md            # UPDATE: add file config section
    ├── FileSystem.md                   # NEW: main file system docs
    ├── FileSystemConfiguration.md      # NEW: server configuration
    ├── IApiKeyStore.md                 # (existing)
    ├── IRefreshTokenStore.md           # (existing)
    └── JwtAuthOptions.md               # (existing)
```

**7.4 Documentation Content Requirements**

Each new documentation file should follow the existing style:
- Use triple-backtick markdown code fences
- Include NuGet package references where applicable
- Cross-link to related documentation
- Include "See also" section at the end
- Provide complete, runnable code examples

Key topics to cover in `FileSystem.md`:
1. **Overview**: Explain that `IFileSystem` from `System.IO.Abstractions` is the unified abstraction
2. **Injection**: Show how to inject `IFileSystem` into command classes
3. **Local Execution**: Local commands get unrestricted `FileSystem` access
4. **Remote Execution**: Connected clients automatically get `SandboxedFileSystem`
5. **Transparent Usage**: Commands don't need to know if running local or remote
6. **File Transfer**: Large file operations use HTTP with progress callbacks
7. **Checksums**: SHA256 verification for data integrity
8. **Cancellation**: Support for cancellation tokens and partial file cleanup

Key topics to cover in `FileSystemConfiguration.md`:
1. **Server Setup**: How to configure file transfer endpoints
2. **Security Options**: Path validation, size limits, extension filtering
3. **Storage Root**: Configuring the sandboxed root directory
4. **Authentication**: File transfers use same JWT auth as SignalR connection
5. **Error Handling**: Common error scenarios and troubleshooting

## Existing Patterns Reference

### RPC Message Pattern (to follow)
```csharp
// Request envelope (Envelopes/FileExistsRequest.cs)
public class FileExistsRequest : ClientRequest
{
    [JsonIgnore]
    public string Path
    {
        get { return TryGetValue(MessageArgNames.FileSystem.Path); }
        set { Data[MessageArgNames.FileSystem.Path] = value; }
    }

    public FileExistsRequest(string path) : base()
    {
        Path = path;
    }
}

// Response envelope (Envelopes/FileExistsResponse.cs)
public class FileExistsResponse : ResponseMessage
{
    [JsonIgnore]
    public bool Exists
    {
        get { return ParseString<bool>(MessageArgNames.FileSystem.Exists); }
        set { Data[MessageArgNames.FileSystem.Exists] = value.ToString(); }
    }

    public FileExistsResponse(string correlationId, bool exists) 
        : base(correlationId)
    {
        Exists = exists;
    }
}
```

### RPC Call Pattern (to follow)
```csharp
// In SandboxedFile.cs
public bool Exists(string path)
{
    var ctx = _rpcRegistry.Register();
    var request = new FileExistsRequest(path) { CorrelationId = ctx.CorrelationId };
    
    _connection.SendAsync(SignalRMethodNames.ReceiveRequest, request);
    
    var response = ctx.WaitForCompletion<FileExistsResponse>().GetAwaiter().GetResult();
    return response.Exists;
}
```

### Server Handler Pattern (to follow)
```csharp
// In FileSystemRpcHandler.cs
public async Task HandleFileExistsRequest(FileExistsRequest request, IClientProxy caller)
{
    var validatedPath = ValidatePath(request.Path);
    var exists = System.IO.File.Exists(validatedPath);
    
    var response = new FileExistsResponse(request.CorrelationId, exists);
    await caller.SendAsync(SignalRMethodNames.ReceiveResponse, response);
}
```

## Complexity Tracking

No constitution violations identified. Changes follow existing patterns:
- ✅ RPC uses existing `RpcMessageRegistry` infrastructure
- ✅ Envelopes follow existing `MessageBase` pattern
- ✅ HTTP endpoints follow existing `MapPost`/`MapGet` pattern
- ✅ Progress updates use existing `FileUploadProgressMessage` pattern

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| IFileSystem API surface is large | Implement only methods actually used; throw NotImplementedException for others initially |
| Breaking change for existing commands | Document migration path; provide clear error messages |
| Async/sync mismatch (IFileSystem has both) | SandboxedFileSystem blocks on async calls for sync methods |
| Performance overhead of RPC for simple operations | Batch operations where possible; consider caching for repeated Exists checks |
