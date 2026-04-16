using System.IO.Abstractions.TestingHelpers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server;
using BitPantry.CommandLine.Remote.SignalR.Server.ClientFileAccess;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

using HubInvocationContext = BitPantry.CommandLine.Remote.SignalR.Server.HubInvocationContext;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests;

/// <summary>
/// Unit tests for RemoteClientFileAccess.
/// Uses HubInvocationContext + auto-responding test client proxy to simulate the push/response round-trip.
/// </summary>
[TestClass]
public class RemoteClientFileAccessTests
{
    private RpcMessageRegistry _rpcMsgReg;
    private HubInvocationContext _invocationContext;
    private MockFileSystem _fileSystem;
    private FileTransferOptions _fileTransferOptions;
    private Mock<ILogger<RemoteClientFileAccess>> _loggerMock;

    [TestInitialize]
    public void Setup()
    {
        var scopeMock = new Mock<IRpcScope>();
        scopeMock.Setup(s => s.GetIdentifier()).Returns("test-scope");
        _rpcMsgReg = new RpcMessageRegistry(scopeMock.Object);
        _invocationContext = new HubInvocationContext();
        _fileSystem = new MockFileSystem();
        _fileTransferOptions = new FileTransferOptions
        {
            MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
            StorageRootPath = "/storage"
        };
        _loggerMock = new Mock<ILogger<RemoteClientFileAccess>>();
    }

    private RemoteClientFileAccess CreateSut()
    {
        return new RemoteClientFileAccess(
            _invocationContext,
            _fileSystem,
            _fileTransferOptions,
            _loggerMock.Object);
    }

    private void SetupContext(PushMessageAutoRespondingClientProxy proxy)
    {
        _invocationContext.Current = new HubInvocationContextData
        {
            ClientProxy = proxy,
            RpcMessageRegistry = _rpcMsgReg,
            Theme = new Theme()
        };
    }

    // ──────────────────────────────────────────────────────
    // GetFileAsync tests
    // ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetFileAsync_SendsPushMessage_WithCorrectClientPath()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls GetFileAsync
        //   Breakage detection: YES - verifies push message sent with correct path
        //   Not a tautology: YES

        ClientFileUploadRequestMessage capturedMsg = null;

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            capturedMsg = msg as ClientFileUploadRequestMessage;
            // Simulate the client uploading the file to the temp path
            if (capturedMsg != null)
                _fileSystem.AddFile(capturedMsg.ServerTempPath, new MockFileData("test content"));

            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);
        var sut = CreateSut();

        await using var result = await sut.GetFileAsync("/client/docs/readme.txt");

        capturedMsg.Should().NotBeNull();
        capturedMsg!.ClientPath.Should().Be("/client/docs/readme.txt");
        capturedMsg.ServerTempPath.Should().Contain(".client-file-staging");
    }

    [TestMethod]
    public async Task GetFileAsync_SuccessfulUpload_ReturnsClientFileWithStream()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls GetFileAsync
        //   Breakage detection: YES - verifies returned ClientFile has correct stream/metadata
        //   Not a tautology: YES

        var fileContent = "hello world";

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            var uploadReq = msg as ClientFileUploadRequestMessage;
            if (uploadReq != null)
                _fileSystem.AddFile(uploadReq.ServerTempPath, new MockFileData(fileContent));

            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);
        var sut = CreateSut();

        await using var result = await sut.GetFileAsync("/client/file.txt");

        result.Should().NotBeNull();
        result.FileName.Should().Be("file.txt");
        result.Length.Should().Be(fileContent.Length);
        result.Stream.Should().NotBeNull();

        using var reader = new StreamReader(result.Stream);
        var content = await reader.ReadToEndAsync();
        content.Should().Be(fileContent);
    }

    [TestMethod]
    public async Task GetFileAsync_ClientError_ThrowsException()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls GetFileAsync
        //   Breakage detection: YES - verifies exception thrown on error response
        //   Not a tautology: YES

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            return new ClientFileAccessResponseMessage(false, "File not found on client");
        });

        SetupContext(proxy);
        var sut = CreateSut();

        var act = async () => await sut.GetFileAsync("/client/missing.txt");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [TestMethod]
    public async Task GetFileAsync_Dispose_DeletesTempFile()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls GetFileAsync and disposes result
        //   Breakage detection: YES - verifies temp file is deleted on dispose
        //   Not a tautology: YES

        string tempFilePath = null;

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            var uploadReq = msg as ClientFileUploadRequestMessage;
            if (uploadReq != null)
            {
                tempFilePath = uploadReq.ServerTempPath;
                _fileSystem.AddFile(uploadReq.ServerTempPath, new MockFileData("temp content"));
            }

            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);
        var sut = CreateSut();

        var result = await sut.GetFileAsync("/client/file.txt");
        _fileSystem.File.Exists(tempFilePath).Should().BeTrue("temp file should exist before dispose");

        await result.DisposeAsync();
        _fileSystem.File.Exists(tempFilePath).Should().BeFalse("temp file should be deleted after dispose");
    }

    // ──────────────────────────────────────────────────────
    // SaveFileAsync(Stream) tests
    // ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task SaveFileAsync_Stream_WritesTempAndSendsPush()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls SaveFileAsync(Stream)
        //   Breakage detection: YES - verifies push message sent with correct paths
        //   Not a tautology: YES

        ClientFileDownloadRequestMessage capturedMsg = null;
        string capturedTempContent = null;

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            capturedMsg = msg as ClientFileDownloadRequestMessage;
            if (capturedMsg != null && _fileSystem.File.Exists(capturedMsg.ServerPath))
                capturedTempContent = _fileSystem.File.ReadAllText(capturedMsg.ServerPath);

            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);
        var sut = CreateSut();

        using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("file data"));
        await sut.SaveFileAsync(content, "/client/output.txt");

        capturedMsg.Should().NotBeNull();
        capturedMsg!.ClientPath.Should().Be("/client/output.txt");
        capturedMsg.ServerPath.Should().Contain(".client-file-staging");
        capturedTempContent.Should().Be("file data");
    }

    [TestMethod]
    public async Task SaveFileAsync_Stream_SuccessfulDownload_DeletesTemp()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls SaveFileAsync(Stream)
        //   Breakage detection: YES - verifies temp file is cleaned up after success
        //   Not a tautology: YES

        string tempPath = null;

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            var dlReq = msg as ClientFileDownloadRequestMessage;
            if (dlReq != null)
                tempPath = dlReq.ServerPath;

            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);
        var sut = CreateSut();

        using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("data"));
        await sut.SaveFileAsync(content, "/client/dest.txt");

        tempPath.Should().NotBeNull();
        _fileSystem.File.Exists(tempPath!).Should().BeFalse("staging temp file should be deleted after successful save");
    }

    // ──────────────────────────────────────────────────────
    // SaveFileAsync(string) tests
    // ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task SaveFileAsync_Path_SendsPushWithSourcePath()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls SaveFileAsync(string)
        //   Breakage detection: YES - verifies push message references source directly
        //   Not a tautology: YES

        ClientFileDownloadRequestMessage capturedMsg = null;

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            capturedMsg = msg as ClientFileDownloadRequestMessage;
            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);

        // Create the source file in the mock file system
        _fileSystem.AddFile("source.dat", new MockFileData("source content"));

        var sut = CreateSut();

        await sut.SaveFileAsync("source.dat", "/client/dest.dat");

        capturedMsg.Should().NotBeNull();
        capturedMsg!.ServerPath.Should().Be("source.dat");
        capturedMsg.ClientPath.Should().Be("/client/dest.dat");
        capturedMsg.FileSize.Should().Be("source content".Length);
    }

    // ──────────────────────────────────────────────────────
    // File size validation tests
    // ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task SaveFileAsync_Stream_ExceedsMaxFileSize_ThrowsBeforeTransfer()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls SaveFileAsync(Stream)
        //   Breakage detection: YES - verifies exception when exceeding size limit
        //   Not a tautology: YES

        _fileTransferOptions.MaxFileSizeBytes = 10; // 10 bytes max

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);
        var sut = CreateSut();

        using var content = new MemoryStream(new byte[20]); // 20 bytes, exceeds 10
        var act = async () => await sut.SaveFileAsync(content, "/client/big.dat");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds*maximum*");

        // Verify no push message was sent
        proxy.SentMessages.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SaveFileAsync_Path_ExceedsMaxFileSize_ThrowsBeforeTransfer()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls SaveFileAsync(string)
        //   Breakage detection: YES - verifies exception when exceeding size limit
        //   Not a tautology: YES

        _fileTransferOptions.MaxFileSizeBytes = 5; // 5 bytes max

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);

        _fileSystem.AddFile("large.dat", new MockFileData(new byte[20]));

        var sut = CreateSut();
        var act = async () => await sut.SaveFileAsync("large.dat", "/client/large.dat");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds*maximum*");

        proxy.SentMessages.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────
    // Error / edge case tests
    // ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetFileAsync_NoHubContext_ThrowsInvalidOperation()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls GetFileAsync without context
        //   Breakage detection: YES - verifies context validation
        //   Not a tautology: YES

        // Don't set any HubInvocationContext
        var sut = CreateSut();

        var act = async () => await sut.GetFileAsync("/client/file.txt");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*hub invocation context*");
    }

    [TestMethod]
    public async Task SaveFileAsync_Stream_NoHubContext_ThrowsInvalidOperation()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls SaveFileAsync(Stream) without context
        //   Breakage detection: YES - verifies context validation
        //   Not a tautology: YES

        var sut = CreateSut();
        using var content = new MemoryStream(new byte[5]);

        var act = async () => await sut.SaveFileAsync(content, "/client/file.txt");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*hub invocation context*");
    }

    [TestMethod]
    public async Task SaveFileAsync_Path_NoHubContext_ThrowsInvalidOperation()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls SaveFileAsync(string) without context
        //   Breakage detection: YES - verifies context validation
        //   Not a tautology: YES

        _fileSystem.AddFile("src.txt", new MockFileData("data"));
        var sut = CreateSut();

        var act = async () => await sut.SaveFileAsync("src.txt", "/client/dest.txt");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*hub invocation context*");
    }

    [TestMethod]
    public async Task SaveFileAsync_Path_SourceNotFound_ThrowsFileNotFoundException()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls SaveFileAsync(string) with missing source
        //   Breakage detection: YES - verifies FileNotFoundException for missing files
        //   Not a tautology: YES

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);
        var sut = CreateSut();

        var act = async () => await sut.SaveFileAsync("nonexistent.txt", "/client/dest.txt");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [TestMethod]
    public async Task GetFileAsync_ClientDeniedError_ThrowsFileAccessDeniedException()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls GetFileAsync
        //   Breakage detection: YES - verifies error mapping to FileAccessDeniedException
        //   Not a tautology: YES

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            return new ClientFileAccessResponseMessage(false, "Access denied by client policy");
        });

        SetupContext(proxy);
        var sut = CreateSut();

        var act = async () => await sut.GetFileAsync("/client/secret.dat");

        await act.Should().ThrowAsync<FileAccessDeniedException>();
    }

    [TestMethod]
    public async Task GetFileAsync_ReportsProgress()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - calls GetFileAsync with progress
        //   Breakage detection: YES - verifies progress is reported
        //   Not a tautology: YES

        var fileContent = "progress test data";
        FileTransferProgress lastProgress = null;
        var progress = new SyncProgress<FileTransferProgress>(p => lastProgress = p);

        var proxy = new PushMessageAutoRespondingClientProxy(_rpcMsgReg, (correlationId, msg) =>
        {
            var uploadReq = msg as ClientFileUploadRequestMessage;
            if (uploadReq != null)
                _fileSystem.AddFile(uploadReq.ServerTempPath, new MockFileData(fileContent));

            return new ClientFileAccessResponseMessage(true);
        });

        SetupContext(proxy);
        var sut = CreateSut();

        await using var result = await sut.GetFileAsync("/client/file.txt", progress);

        lastProgress.Should().NotBeNull();
        lastProgress!.BytesTransferred.Should().Be(fileContent.Length);
        lastProgress.TotalBytes.Should().Be(fileContent.Length);
    }

    // ──────────────────────────────────────────────────────
    // Hub routing test
    // ──────────────────────────────────────────────────────

    [TestMethod]
    public void HubReceiveRequest_ClientFileAccessResponse_SetsRpcResponse()
    {
        // Test Validity Check:
        //   Invokes code under test: YES - tests RpcMessageRegistry.SetResponse via the response message
        //   Breakage detection: YES - verifies the RPC context receives the response
        //   Not a tautology: YES

        // Register an RPC context to await a response
        var rpcCtx = _rpcMsgReg.Register();

        // Create the response message (simulating what the hub routes)
        var responseMsg = new ClientFileAccessResponseMessage(true);
        responseMsg.CorrelationId = rpcCtx.CorrelationId;

        // Route it through RpcMessageRegistry (same as hub does)
        _rpcMsgReg.SetResponse(responseMsg);

        // The RPC context should complete
        var result = rpcCtx.WaitForCompletion<ClientFileAccessResponseMessage>();
        result.IsCompleted.Should().BeTrue();
        result.Result.Success.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────
    // Test helpers
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Synchronous progress reporter for tests (avoids Progress&lt;T&gt; thread pool race).
    /// </summary>
    private class SyncProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        public SyncProgress(Action<T> handler) => _handler = handler;
        public void Report(T value) => _handler(value);
    }

    /// <summary>
    /// A test client proxy that auto-responds to push messages (ReceiveMessage) by
    /// calling RpcMessageRegistry.SetResponse with a fabricated response.
    /// This simulates the client receiving a push message and sending back a response.
    /// </summary>
    private class PushMessageAutoRespondingClientProxy : IClientProxy, ISingleClientProxy
    {
        private readonly RpcMessageRegistry _rpcMsgReg;
        private readonly Func<string, PushMessage, MessageBase> _responseFactory;

        public List<(string Method, object[] Args, CancellationToken Token)> SentMessages { get; } = new();

        public PushMessageAutoRespondingClientProxy(
            RpcMessageRegistry rpcMsgReg,
            Func<string, PushMessage, MessageBase> responseFactory)
        {
            _rpcMsgReg = rpcMsgReg;
            _responseFactory = responseFactory;
        }

        public Task<T> InvokeCoreAsync<T>(string method, object[] args, CancellationToken cancellationToken = default)
        {
            SentMessages.Add((method, args, cancellationToken));
            return Task.FromResult(default(T)!);
        }

        public Task SendAsync(string method, object arg1, CancellationToken cancellationToken = default)
        {
            return SendCoreAsync(method, new object[] { arg1 }, cancellationToken);
        }

        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default)
        {
            SentMessages.Add((method, args, cancellationToken));

            // If this is a push message, auto-respond
            if (method == SignalRMethodNames.ReceiveMessage && args.Length > 0 && args[0] is PushMessage pushMsg)
            {
                var correlationId = pushMsg.CorrelationId;
                var response = _responseFactory(correlationId, pushMsg);
                response.CorrelationId = correlationId;

                // Use Task.Run to simulate async response arrival
                _ = Task.Run(() => _rpcMsgReg.SetResponse(response));
            }

            return Task.CompletedTask;
        }
    }
}
