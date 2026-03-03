using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

using HubInvocationContext = BitPantry.CommandLine.Remote.SignalR.Server.HubInvocationContext;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests;

/// <summary>
/// Unit tests for ClientFileSystemBrowser.
/// Uses HubInvocationContext + auto-responding test client proxy to simulate the RPC round-trip.
/// </summary>
[TestClass]
public class ClientFileSystemBrowserTests
{
    private RpcMessageRegistry _rpcMsgReg;
    private Mock<ILogger<ClientFileSystemBrowser>> _loggerMock;
    private HubInvocationContext _invocationContext;

    [TestInitialize]
    public void Setup()
    {
        var scopeMock = new Mock<IRpcScope>();
        scopeMock.Setup(s => s.GetIdentifier()).Returns("test-scope");
        _rpcMsgReg = new RpcMessageRegistry(scopeMock.Object);
        _loggerMock = new Mock<ILogger<ClientFileSystemBrowser>>();
        _invocationContext = new HubInvocationContext();
    }

    [TestMethod]
    public async Task GetCurrentDirectoryAsync_ReturnsEmptyString()
    {
        var browser = new ClientFileSystemBrowser(_invocationContext, _loggerMock.Object);

        var result = await browser.GetCurrentDirectoryAsync();

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task EnumeratePathEntries_SuccessfulResponse_ReturnsEntries()
    {
        var expectedEntries = new[]
        {
            new PathEntry("docs", true),
            new PathEntry("readme.txt", false),
        };

        var clientProxy = new AutoRespondingClientProxy(_rpcMsgReg, (correlationId) =>
            new EnumeratePathEntriesResponse(correlationId, expectedEntries));

        _invocationContext.Current = new HubInvocationContextData
        {
            ClientProxy = clientProxy,
            RpcMessageRegistry = _rpcMsgReg,
            Theme = new Theme()
        };

        var browser = new ClientFileSystemBrowser(_invocationContext, _loggerMock.Object);

        var result = await browser.EnumeratePathEntriesAsync("/data", includeFiles: true);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("docs");
        result[0].IsDirectory.Should().BeTrue();
        result[1].Name.Should().Be("readme.txt");
        result[1].IsDirectory.Should().BeFalse();
    }

    [TestMethod]
    public async Task EnumeratePathEntries_ErrorResponse_ReturnsEmptyList()
    {
        var clientProxy = new AutoRespondingClientProxy(_rpcMsgReg, (correlationId) =>
            new EnumeratePathEntriesResponse(correlationId, Array.Empty<PathEntry>(), "Access denied"));

        _invocationContext.Current = new HubInvocationContextData
        {
            ClientProxy = clientProxy,
            RpcMessageRegistry = _rpcMsgReg,
            Theme = new Theme()
        };

        var browser = new ClientFileSystemBrowser(_invocationContext, _loggerMock.Object);

        var result = await browser.EnumeratePathEntriesAsync("/secret", includeFiles: true);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task EnumeratePathEntries_NoContext_ThrowsInvalidOperationException()
    {
        // No HubInvocationContext.Current set → should throw
        var browser = new ClientFileSystemBrowser(_invocationContext, _loggerMock.Object);

        var act = async () => await browser.EnumeratePathEntriesAsync("/data", includeFiles: true);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*hub invocation context*");
    }

    [TestMethod]
    public async Task EnumeratePathEntries_SendsCorrectRequest()
    {
        var capturedRequest = (ClientEnumeratePathEntriesRequest)null;
        AutoRespondingClientProxy clientProxy = null!;
        clientProxy = new AutoRespondingClientProxy(_rpcMsgReg, (correlationId) =>
        {
            // Capture the request that was sent
            var msg = clientProxy.SentMessages.Last();
            var req = msg.Args[0] as ClientEnumeratePathEntriesRequest;
            if (req != null)
                capturedRequest = req;
            return new EnumeratePathEntriesResponse(correlationId, Array.Empty<PathEntry>());
        });

        _invocationContext.Current = new HubInvocationContextData
        {
            ClientProxy = clientProxy,
            RpcMessageRegistry = _rpcMsgReg,
            Theme = new Theme()
        };

        var browser = new ClientFileSystemBrowser(_invocationContext, _loggerMock.Object);

        await browser.EnumeratePathEntriesAsync("/mydir", includeFiles: false);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.DirectoryPath.Should().Be("/mydir");
        capturedRequest.IncludeFiles.Should().BeFalse();
    }

    /// <summary>
    /// A simple test client proxy for basic availability tests.
    /// </summary>
    private class TestClientProxy : IClientProxy, ISingleClientProxy
    {
        public Task<T> InvokeCoreAsync<T>(string method, object[] args, CancellationToken cancellationToken = default)
            => Task.FromResult(default(T)!);

        public Task SendAsync(string method, object arg1, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    /// <summary>
    /// A test client proxy that automatically responds to RPC requests by calling
    /// RpcMessageRegistry.SetResponse with a fabricated response.
    /// </summary>
    private class AutoRespondingClientProxy : IClientProxy, ISingleClientProxy
    {
        private readonly RpcMessageRegistry _rpcMsgReg;
        private readonly Func<string, MessageBase> _responseFactory;

        public List<(string Method, object[] Args, CancellationToken Token)> SentMessages { get; } = new();

        public AutoRespondingClientProxy(RpcMessageRegistry rpcMsgReg, Func<string, MessageBase> responseFactory)
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

            // If this is an RPC request, auto-respond
            if (method == SignalRMethodNames.ReceiveRequest && args.Length > 0 && args[0] is ClientRequest req)
            {
                var correlationId = req.CorrelationId;
                var response = _responseFactory(correlationId);
                response.CorrelationId = correlationId;

                // Use Task.Run to simulate async response arrival
                _ = Task.Run(() => _rpcMsgReg.SetResponse(response));
            }

            return Task.CompletedTask;
        }
    }
}
