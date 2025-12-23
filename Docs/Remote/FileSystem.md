# IFileSystem Abstraction

The BitPantry.CommandLine framework uses `System.IO.Abstractions.IFileSystem` for all file operations. This abstraction enables seamless file access whether commands run locally or remotely.

## Overview

When commands execute:
- **Locally**: `IFileSystem` resolves to `FileSystem` with unrestricted access
- **Remotely (on server)**: `IFileSystem` resolves to `SandboxedFileSystem` confined to the configured storage root

## Usage in Commands

Inject `IFileSystem` into your command class:

```csharp
using System.IO.Abstractions;

[Command]
public class FileListCommand
{
    private readonly IFileSystem _fileSystem;

    public FileListCommand(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    [Execute]
    public void Execute(string directory = ".")
    {
        var files = _fileSystem.Directory.GetFiles(directory);
        foreach (var file in files)
        {
            Console.WriteLine(_fileSystem.Path.GetFileName(file));
        }
    }
}
```

## Available Operations

### IFile Operations

```csharp
// Reading
string content = _fileSystem.File.ReadAllText("config.json");
byte[] bytes = _fileSystem.File.ReadAllBytes("data.bin");
string[] lines = _fileSystem.File.ReadAllLines("log.txt");

// Writing
_fileSystem.File.WriteAllText("output.txt", "Hello World");
_fileSystem.File.WriteAllBytes("output.bin", bytes);
_fileSystem.File.WriteAllLines("output.txt", lines);

// Existence and attributes
bool exists = _fileSystem.File.Exists("config.json");
FileAttributes attrs = _fileSystem.File.GetAttributes("file.txt");

// Copy, Move, Delete
_fileSystem.File.Copy("source.txt", "dest.txt");
_fileSystem.File.Move("old.txt", "new.txt");
_fileSystem.File.Delete("temp.txt");
```

### IDirectory Operations

```csharp
// Creation and existence
_fileSystem.Directory.CreateDirectory("logs");
bool exists = _fileSystem.Directory.Exists("logs");

// Enumeration
var files = _fileSystem.Directory.GetFiles("logs", "*.log");
var dirs = _fileSystem.Directory.GetDirectories(".");
var allFiles = _fileSystem.Directory.EnumerateFiles(".", "*", SearchOption.AllDirectories);

// Delete
_fileSystem.Directory.Delete("temp", recursive: true);
```

### IPath Operations

```csharp
string combined = _fileSystem.Path.Combine("dir", "file.txt");
string filename = _fileSystem.Path.GetFileName("/path/to/file.txt");
string extension = _fileSystem.Path.GetExtension("file.txt");
```

## Async Operations

All operations have async variants:

```csharp
string content = await _fileSystem.File.ReadAllTextAsync("config.json");
await _fileSystem.File.WriteAllTextAsync("output.txt", "Hello World");
```

## Server-Side Behavior

When executing on the server, the `SandboxedFileSystem`:

1. **Validates all paths** - Prevents path traversal attacks (e.g., `../../../etc/passwd`)
2. **Confines access** - All operations are restricted to the configured `StorageRootPath`
3. **Resolves relative paths** - Paths like `data/file.txt` become `{StorageRootPath}/data/file.txt`
4. **Enforces extension restrictions** - Only configured file extensions are allowed (if configured)
5. **Enforces size limits** - File write operations are limited to configured maximum size

### Security Features

- **Path traversal protection**: Attempts to access `../outside` throw `UnauthorizedAccessException`
- **Extension whitelist**: Configurable allowed extensions (e.g., `.txt`, `.json`)
- **Size limits**: Maximum file size enforced for write operations

## Testing

Use `MockFileSystem` from `TestableIO.System.IO.Abstractions.TestingHelpers` in unit tests:

```csharp
using System.IO.Abstractions.TestingHelpers;

[TestMethod]
public void MyCommand_ReadsFile()
{
    var mockFileSystem = new MockFileSystem();
    mockFileSystem.File.WriteAllText("/data/config.json", "{}");

    var command = new MyCommand(mockFileSystem);
    command.Execute();
    
    // Assert results
}
```

## See Also

- [FileSystemConfiguration.md](FileSystemConfiguration.md) - Server configuration options
- [CommandLineServer.md](CommandLineServer.md) - Server setup overview
