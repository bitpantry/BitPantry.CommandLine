# Server File System & Sandboxing

The server confines all file operations to a configured storage root, preventing path traversal and enforcing size and extension restrictions.

---

## SandboxedFileSystem

`SandboxedFileSystem` implements `System.IO.Abstractions.IFileSystem` and restricts all paths to the configured `StorageRootPath`:

```csharp
builder.Services.AddCommandLineHub(opt =>
{
    opt.FileTransferOptions.StorageRootPath = "./storage";
});
```

Any file operation that attempts to access a path outside the storage root is rejected. This applies to both server commands using `IFileSystem` and file transfer endpoints.

---

## FileTransferOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StorageRootPath` | `string` | _(required)_ | Root directory for all file operations |
| `MaxFileSizeBytes` | `long` | `104857600` (100 MB) | Maximum upload file size |
| `AllowedExtensions` | `string[]` | `null` (all allowed) | Whitelist of permitted file extensions |

```csharp
opt.FileTransferOptions.StorageRootPath = "/data/sandbox";
opt.FileTransferOptions.MaxFileSizeBytes = 50 * 1024 * 1024;  // 50 MB
opt.FileTransferOptions.AllowedExtensions = new[] { ".txt", ".csv", ".json", ".pdf" };
```

---

## Validators

Three validators enforce the sandboxing rules:

| Validator | Purpose |
|-----------|---------|
| `PathValidator` | Rejects paths that escape the storage root (path traversal: `../`, symlinks) |
| `FileSizeValidator` | Rejects uploads exceeding `MaxFileSizeBytes` |
| `ExtensionValidator` | Rejects files with extensions not in `AllowedExtensions` (when configured) |

---

## Commands and IFileSystem

Server-side commands that inject `IFileSystem` receive the `SandboxedFileSystem`, so they operate within the sandbox automatically:

```csharp
[Command(Name = "list-files")]
public class ListFilesCommand : CommandBase
{
    private readonly IFileSystem _fs;

    public ListFilesCommand(IFileSystem fs) => _fs = fs;

    public void Execute(CommandExecutionContext ctx)
    {
        foreach (var file in _fs.Directory.GetFiles("."))
            Console.MarkupLine(file);
    }
}
```

This command will only see files within the storage root.

---

## See Also

- [Setting Up the Server](index.md)
- [File Transfers](../client/file-transfers.md)
