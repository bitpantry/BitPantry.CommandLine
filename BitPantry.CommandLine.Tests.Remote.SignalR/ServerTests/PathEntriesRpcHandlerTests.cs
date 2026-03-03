using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using BitPantry.CommandLine.Remote.SignalR.Server.Rpc;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests;

/// <summary>
/// Unit tests for PathEntriesRpcHandler.
/// Uses real file system (consistent with FileSystemRpcHandlerTests pattern).
/// </summary>
[TestClass]
public class PathEntriesRpcHandlerTests
{
    private string _storageRoot = null!;
    private IFileSystem _fileSystem = null!;
    private Mock<ILogger<PathEntriesRpcHandler>> _loggerMock = null!;
    private FileTransferOptions _options = null!;
    private PathEntriesRpcHandler _handler = null!;
    private TestClientProxy _clientProxy = null!;

    [TestInitialize]
    public void Setup()
    {
        _storageRoot = Path.Combine(Path.GetTempPath(), $"PathEntriesRpcHandlerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_storageRoot);

        _fileSystem = new FileSystem();
        _loggerMock = new Mock<ILogger<PathEntriesRpcHandler>>();
        _options = new FileTransferOptions { StorageRootPath = _storageRoot };
        _handler = new PathEntriesRpcHandler(_loggerMock.Object, _fileSystem, _options);
        _clientProxy = new TestClientProxy();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_storageRoot))
        {
            try { Directory.Delete(_storageRoot, recursive: true); }
            catch { /* best effort */ }
        }
    }

    [TestMethod]
    public async Task HandleRequest_ValidDirectory_IncludeFiles_ReturnsAll()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_storageRoot, "subdir"));
        File.WriteAllText(Path.Combine(_storageRoot, "file1.txt"), "content");

        var request = new EnumeratePathEntriesRequest("", true) { CorrelationId = Guid.NewGuid().ToString() };

        // Act
        await _handler.HandleEnumeratePathEntries(_clientProxy, request);

        // Assert
        var response = ExtractResponse();
        response.Error.Should().BeNull();
        response.Entries.Should().Contain(e => e.Name == "subdir" && e.IsDirectory);
        response.Entries.Should().Contain(e => e.Name == "file1.txt" && !e.IsDirectory);
    }

    [TestMethod]
    public async Task HandleRequest_ValidDirectory_IncludeFilesFalse_ReturnsOnlyDirs()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_storageRoot, "subdir"));
        File.WriteAllText(Path.Combine(_storageRoot, "file1.txt"), "content");

        var request = new EnumeratePathEntriesRequest("", false) { CorrelationId = Guid.NewGuid().ToString() };

        // Act
        await _handler.HandleEnumeratePathEntries(_clientProxy, request);

        // Assert
        var response = ExtractResponse();
        response.Error.Should().BeNull();
        response.Entries.Should().HaveCount(1);
        response.Entries[0].Name.Should().Be("subdir");
        response.Entries[0].IsDirectory.Should().BeTrue();
    }

    [TestMethod]
    public async Task HandleRequest_NonExistentDirectory_ReturnsError()
    {
        var request = new EnumeratePathEntriesRequest("nonexistent", true) { CorrelationId = Guid.NewGuid().ToString() };

        await _handler.HandleEnumeratePathEntries(_clientProxy, request);

        var response = ExtractResponse();
        response.Error.Should().Contain("Directory not found");
    }

    [TestMethod]
    public async Task HandleRequest_PathTraversalAttempt_ReturnsError()
    {
        var request = new EnumeratePathEntriesRequest("../../etc", true) { CorrelationId = Guid.NewGuid().ToString() };

        await _handler.HandleEnumeratePathEntries(_clientProxy, request);

        var response = ExtractResponse();
        response.Error.Should().Contain("Path traversal");
    }

    [TestMethod]
    public async Task HandleRequest_EmptyDirectory_ReturnsEmptyList()
    {
        var emptyDir = Path.Combine(_storageRoot, "empty");
        Directory.CreateDirectory(emptyDir);

        var request = new EnumeratePathEntriesRequest("empty", true) { CorrelationId = Guid.NewGuid().ToString() };

        await _handler.HandleEnumeratePathEntries(_clientProxy, request);

        var response = ExtractResponse();
        response.Error.Should().BeNull();
        response.Entries.Should().BeEmpty();
    }

    [TestMethod]
    public async Task HandleRequest_EmptyDirectoryPath_EnumeratesStorageRoot()
    {
        // Empty string should resolve to the storage root
        Directory.CreateDirectory(Path.Combine(_storageRoot, "rootdir"));
        File.WriteAllText(Path.Combine(_storageRoot, "rootfile.txt"), "content");

        var request = new EnumeratePathEntriesRequest("", true) { CorrelationId = Guid.NewGuid().ToString() };

        await _handler.HandleEnumeratePathEntries(_clientProxy, request);

        var response = ExtractResponse();
        response.Error.Should().BeNull();
        response.Entries.Should().Contain(e => e.Name == "rootdir" && e.IsDirectory);
        response.Entries.Should().Contain(e => e.Name == "rootfile.txt" && !e.IsDirectory);
    }

    private EnumeratePathEntriesResponse ExtractResponse()
    {
        _clientProxy.SentMessages.Should().HaveCount(1);
        var (method, args, _) = _clientProxy.SentMessages[0];
        method.Should().Be(SignalRMethodNames.ReceiveResponse);
        args.Should().HaveCount(1);
        var respMsg = args[0] as EnumeratePathEntriesResponse;
        respMsg.Should().NotBeNull();
        return respMsg!;
    }
}
