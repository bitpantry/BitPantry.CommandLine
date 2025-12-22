# Quickstart: Unified File System Abstraction

**Feature**: 001-unified-file-system

## Overview

This guide covers the unified `IFileSystem` abstraction that enables commands to work identically whether running locally or connected to a remote server.

---

## Breaking Changes

### Removed: Custom IFileService

**Before** (no longer supported):
```csharp
public class MyCommand : CommandBase
{
    private readonly IFileService _fileService;
    
    public MyCommand(IFileService fileService)
    {
        _fileService = fileService;
    }
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        var content = _fileService.ReadAllText("data.txt");  // ❌ Removed
    }
}
```

**After** (use IFileSystem):
```csharp
using System.IO.Abstractions;

public class MyCommand : CommandBase
{
    private readonly IFileSystem _fileSystem;
    
    public MyCommand(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        var content = _fileSystem.File.ReadAllText("data.txt");  // ✅
    }
}
```

### Removed: Static Extension Methods for File Transfer

**Before** (no longer supported):
```csharp
public class UploadCommand : CommandBase
{
    public async Task Execute(CommandExecutionContext ctx)
    {
        await this.UploadFile("local.txt", "remote.txt");  // ❌ Removed
    }
}
```

**After** (use IFileSystem):
```csharp
public class UploadCommand : CommandBase
{
    private readonly IFileSystem _fileSystem;
    
    public UploadCommand(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        var localBytes = File.ReadAllBytes("local.txt");  // Read locally
        _fileSystem.File.WriteAllBytes("remote.txt", localBytes);  // Write to remote
    }
}
```

---

## Command Usage

### Reading Files

```csharp
using System.IO.Abstractions;

public class ReadDataCommand : CommandBase
{
    private readonly IFileSystem _fileSystem;
    
    public ReadDataCommand(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    [Argument]
    public string FilePath { get; set; }
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        // Works locally OR remotely - same code!
        if (_fileSystem.File.Exists(FilePath))
        {
            var content = _fileSystem.File.ReadAllText(FilePath);
            Console.WriteLine(content);
        }
        else
        {
            Console.WriteLine($"File not found: {FilePath}");
        }
    }
}
```

### Writing Files

```csharp
public class SaveReportCommand : CommandBase
{
    private readonly IFileSystem _fileSystem;
    
    public SaveReportCommand(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    [Argument]
    public string OutputPath { get; set; }
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        var report = GenerateReport();
        
        // Automatically uploads to server if connected remotely
        // Writes to local disk if running locally
        _fileSystem.File.WriteAllText(OutputPath, report);
        
        Console.WriteLine($"Report saved to: {OutputPath}");
    }
}
```

### Directory Operations

```csharp
public class ListFilesCommand : CommandBase
{
    private readonly IFileSystem _fileSystem;
    
    public ListFilesCommand(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    [Argument]
    public string DirectoryPath { get; set; } = ".";
    
    [Option]
    public string Pattern { get; set; } = "*";
    
    [Option]
    public bool Recursive { get; set; } = false;
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        var searchOption = Recursive 
            ? SearchOption.AllDirectories 
            : SearchOption.TopDirectoryOnly;
        
        var files = _fileSystem.Directory.EnumerateFiles(
            DirectoryPath, 
            Pattern, 
            searchOption);
        
        foreach (var file in files)
        {
            var info = _fileSystem.FileInfo.New(file);
            Console.WriteLine($"{info.Name,-30} {info.Length,15:N0} bytes");
        }
    }
}
```

### Creating Directory Structure

```csharp
public class InitProjectCommand : CommandBase
{
    private readonly IFileSystem _fileSystem;
    
    public InitProjectCommand(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    [Argument]
    public string ProjectName { get; set; }
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        // Create directory structure
        _fileSystem.Directory.CreateDirectory($"{ProjectName}/src");
        _fileSystem.Directory.CreateDirectory($"{ProjectName}/tests");
        _fileSystem.Directory.CreateDirectory($"{ProjectName}/docs");
        
        // Create initial files
        _fileSystem.File.WriteAllText(
            $"{ProjectName}/README.md", 
            $"# {ProjectName}\n\nProject description here.");
        
        Console.WriteLine($"Project '{ProjectName}' initialized.");
    }
}
```

---

## Local vs Remote Behavior

| Operation | Local | Remote |
|-----------|-------|--------|
| File paths | Absolute or relative to current directory | Relative to server's StorageRootPath |
| Access restrictions | None (full disk access) | Sandboxed to StorageRootPath |
| Path traversal (`../`) | Allowed | Blocked (security) |
| Large files | Direct disk I/O | HTTP streaming with checksum verification |

### Example: Same Command, Different Contexts

```csharp
// This command works identically in both modes:
var content = _fileSystem.File.ReadAllText("reports/2024/q1.csv");

// LOCAL: Reads from C:\Users\me\reports\2024\q1.csv (if that's current dir)
// REMOTE: Reads from {StorageRootPath}/reports/2024/q1.csv on server
```

---

## Unit Testing with MockFileSystem

```csharp
using System.IO.Abstractions.TestingHelpers;

[TestClass]
public class ReadDataCommandTests
{
    [TestMethod]
    public async Task Execute_WhenFileExists_PrintsContent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"c:\data\test.txt", new MockFileData("Hello, World!") }
        });
        
        var command = new ReadDataCommand(mockFileSystem)
        {
            FilePath = @"c:\data\test.txt"
        };
        
        // Act
        await command.Execute(new CommandExecutionContext());
        
        // Assert - verify console output or other side effects
    }
    
    [TestMethod]
    public async Task Execute_WhenFileDoesNotExist_PrintsNotFound()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var command = new ReadDataCommand(mockFileSystem)
        {
            FilePath = @"c:\nonexistent.txt"
        };
        
        // Act
        await command.Execute(new CommandExecutionContext());
        
        // Assert
    }
}
```

---

## Server Configuration

Configure file transfer options in server startup:

```csharp
// In ASP.NET Core startup
services.Configure<FileTransferOptions>(options =>
{
    options.StorageRootPath = "/var/cli-storage";  // Base directory
    options.MaxFileSizeBytes = 100 * 1024 * 1024;  // 100 MB
    options.AllowedExtensions = new HashSet<string> 
    { 
        ".txt", ".csv", ".json", ".xml", ".pdf" 
    };
});
```

Or via configuration:

```json
{
  "FileTransfer": {
    "StorageRootPath": "/var/cli-storage",
    "MaxFileSizeBytes": 104857600,
    "AllowedExtensions": [".txt", ".csv", ".json", ".xml", ".pdf"]
  }
}
```

---

## Common Patterns

### Check Before Write

```csharp
var targetPath = "output/results.csv";

if (_fileSystem.File.Exists(targetPath))
{
    Console.WriteLine("File already exists. Overwrite? (y/n)");
    if (Console.ReadLine()?.ToLower() != "y")
        return;
}

_fileSystem.File.WriteAllText(targetPath, csvContent);
```

### Ensure Directory Exists

```csharp
var outputDir = _fileSystem.Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(outputDir))
{
    _fileSystem.Directory.CreateDirectory(outputDir);  // No-op if exists
}
_fileSystem.File.WriteAllText(outputPath, content);
```

### Path Manipulation (Always Local)

```csharp
// Path operations work locally even when connected remotely
var combined = _fileSystem.Path.Combine("reports", "2024", "q1.csv");
var extension = _fileSystem.Path.GetExtension(filePath);
var fileName = _fileSystem.Path.GetFileName(filePath);
```

---

## Error Handling

```csharp
try
{
    var content = _fileSystem.File.ReadAllText(path);
}
catch (FileNotFoundException)
{
    Console.WriteLine("File not found");
}
catch (UnauthorizedAccessException)
{
    // Remote: path traversal attempt blocked
    // Local: permission denied
    Console.WriteLine("Access denied");
}
catch (IOException ex) when (ex.Message.Contains("size limit"))
{
    Console.WriteLine("File exceeds server size limit");
}
```
