# File System Configuration

This document describes how to configure the server-side file system for remote command execution.

## FileTransferOptions

Configure file transfer and storage settings through `FileTransferOptions` when setting up the server:

```csharp
services.AddCommandLineServer(options =>
{
    options.FileTransferOptions = new FileTransferOptions
    {
        // Required: Root directory for all file operations
        StorageRootPath = "/var/app/storage",
        
        // Optional: Maximum file size (default: 100 MB)
        MaxFileSizeBytes = 100 * 1024 * 1024,
        
        // Optional: Allowed file extensions (null = all allowed)
        AllowedExtensions = new[] { ".txt", ".json", ".xml", ".csv" }
    };
});
```

## Configuration Options

### StorageRootPath (Required)

The root directory where all file operations are confined. All paths used by commands are resolved relative to this directory.

```csharp
StorageRootPath = "/var/app/storage"
```

**Requirements:**
- Must be a valid absolute path
- Directory must exist or be creatable
- Application must have read/write permissions

**Security:**
- All file operations are confined to this directory
- Path traversal attempts (e.g., `../`) are blocked

### MaxFileSizeBytes (Optional)

Maximum allowed file size for uploads and file write operations.

```csharp
MaxFileSizeBytes = 50 * 1024 * 1024  // 50 MB
```

**Default:** 100 MB (104,857,600 bytes)

**Behavior:**
- Pre-flight check: Validates `Content-Length` header before accepting upload
- Streaming check: Monitors bytes during transfer and aborts if exceeded
- Throws `FileSizeLimitExceededException` if limit is exceeded

### AllowedExtensions (Optional)

Whitelist of file extensions that can be written to the file system.

```csharp
AllowedExtensions = new[] { ".txt", ".json", ".xml" }
```

**Default:** `null` (all extensions allowed)

**Behavior:**
- Extensions are matched case-insensitively
- Include the leading dot (e.g., `.txt` not `txt`)
- Files without extensions can be blocked if an extension list is provided

**Example - Allow common document types:**
```csharp
AllowedExtensions = new[]
{
    ".txt", ".json", ".xml", ".csv",
    ".pdf", ".doc", ".docx",
    ".png", ".jpg", ".gif"
}
```

## Complete Server Setup Example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommandLineServer(options =>
{
    // Configure file storage
    options.FileTransferOptions = new FileTransferOptions
    {
        StorageRootPath = Path.Combine(Environment.CurrentDirectory, "cli-storage"),
        MaxFileSizeBytes = 50 * 1024 * 1024, // 50 MB
        AllowedExtensions = new[] { ".txt", ".json", ".log" }
    };

    // Register your command assemblies
    options.CommandRegistry.RegisterAssembly(typeof(MyCommands).Assembly);
    
    // Configure authentication
    options.UseJwtAuth(auth =>
    {
        auth.Secret = Configuration["Jwt:Secret"];
        auth.Issuer = "MyApp";
    });
});

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<CommandLineHub>("/cli");

app.Run();
```

## Validation

The framework validates configuration at startup:

- **StorageRootPath null/empty**: Throws `InvalidOperationException`
- **MaxFileSizeBytes <= 0**: Throws `ArgumentException`

## Security Considerations

1. **Directory Permissions**: Set restrictive permissions on `StorageRootPath`
2. **Extension Whitelist**: Use `AllowedExtensions` to prevent executable uploads
3. **Size Limits**: Set appropriate `MaxFileSizeBytes` for your use case
4. **Checksum Verification**: Uploads include SHA256 checksum verification

## Error Handling

| Error | HTTP Status | Condition |
|-------|-------------|-----------|
| Path Traversal | 403 Forbidden | Path attempts to escape storage root |
| Invalid Extension | 415 Unsupported | Extension not in allowed list |
| Size Exceeded | 413 Payload Too Large | File exceeds MaxFileSizeBytes |
| Checksum Mismatch | 400 Bad Request | File integrity verification failed |

## See Also

- [FileSystem.md](FileSystem.md) - Using IFileSystem in commands
- [CommandLineServer.md](CommandLineServer.md) - Server setup overview
