using System;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Client;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BitPantry.CommandLine.Tests.AutoComplete.Integration;

/// <summary>
/// Tests for remote completion error handling scenarios (RC-010 to RC-015)
/// </summary>
[TestClass]
public class RemoteErrorTests
{
    private Mock<IServerProxy> _mockServerProxy;
    private RemoteCompletionProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _mockServerProxy = new Mock<IServerProxy>();
        _provider = new RemoteCompletionProvider(_mockServerProxy.Object);
    }

    #region RC-010: Network timeout

    [TestMethod]
    public async Task RC010_GetCompletionsAsync_Timeout_ReturnsTimedOutResult()
    {
        // Given: Server doesn't respond within timeout
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Server did not respond within 3 seconds"));

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Getting completions
        var result = await _provider.GetCompletionsAsync(context);

        // Then: Timeout error shown, user can continue
        result.IsTimedOut.Should().BeTrue();
    }

    #endregion

    #region RC-011: Network error

    [TestMethod]
    public async Task RC011_GetCompletionsAsync_NetworkError_ReturnsErrorResult()
    {
        // Given: Connection fails
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network connection failed"));

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Getting completions
        var result = await _provider.GetCompletionsAsync(context);

        // Then: Brief error message shown, input still usable
        result.IsError.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Remote completion error");
    }

    #endregion

    #region RC-012: Server returns error

    [TestMethod]
    public async Task RC012_GetCompletionsAsync_ServerError_ReturnsErrorResult()
    {
        // Given: Server returns 500 error
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        var errorResult = CompletionResult.Error("Internal server error");
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Getting completions
        var result = await _provider.GetCompletionsAsync(context);

        // Then: Error message shown
        result.IsError.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Internal server error");
    }

    #endregion

    #region RC-013: Disconnected state

    [TestMethod]
    public void RC013_CanHandle_Disconnected_ReturnsFalse()
    {
        // Given: Not connected to server
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Checking if can handle
        var canHandle = _provider.CanHandle(context);

        // Then: Cannot handle when disconnected
        canHandle.Should().BeFalse();
    }

    [TestMethod]
    public async Task RC013_GetCompletionsAsync_Disconnected_ReturnsOfflineIndicator()
    {
        // Given: Not connected to server (forced call to GetCompletionsAsync despite CanHandle=false)
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Getting completions while disconnected
        var result = await _provider.GetCompletionsAsync(context);

        // Then: "(offline)" indicator shown
        result.IsError.Should().BeTrue();
        result.ErrorMessage.Should().Contain("offline");
    }

    [TestMethod]
    public void RC013_CanHandle_Reconnecting_ReturnsFalse()
    {
        // Given: Currently reconnecting
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Reconnecting);

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Checking if can handle
        var canHandle = _provider.CanHandle(context);

        // Then: Cannot handle while reconnecting
        canHandle.Should().BeFalse();
    }

    #endregion

    #region RC-014: Reconnection during fetch

    [TestMethod]
    public async Task RC014_GetCompletionsAsync_DisconnectedDuringFetch_ReturnsOffline()
    {
        // Given: Connection drops mid-fetch
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The connection to the server is disconnected"));

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Connection drops during fetch
        var result = await _provider.GetCompletionsAsync(context);

        // Then: Offline indicator shown
        result.IsError.Should().BeTrue();
        result.ErrorMessage.Should().Contain("offline");
    }

    #endregion

    #region RC-015: Manual retry after error

    [TestMethod]
    public async Task RC015_GetCompletionsAsync_AfterError_CanRetry()
    {
        // Given: Previous Tab failed, now successful
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        
        // First call fails
        _mockServerProxy
            .SetupSequence(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"))
            .ReturnsAsync(new CompletionResult(new[] { 
                new CompletionItem { DisplayText = "success", InsertText = "success" } 
            }));

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: First attempt fails
        var firstResult = await _provider.GetCompletionsAsync(context);
        firstResult.IsError.Should().BeTrue();

        // When: User presses Tab again (retry)
        var retryResult = await _provider.GetCompletionsAsync(context);

        // Then: New fetch attempted and successful
        retryResult.IsError.Should().BeFalse();
        retryResult.Items.Count.Should().Be(1);
        retryResult.Items[0].DisplayText.Should().Be("success");
    }

    #endregion

    #region Additional edge cases

    [TestMethod]
    public async Task GetCompletionsAsync_NullServerProxy_ReturnsError()
    {
        // Given: Null server proxy (constructor allows null)
        var provider = new RemoteCompletionProvider(null);

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Getting completions
        var result = await provider.GetCompletionsAsync(context);

        // Then: Error returned
        result.IsError.Should().BeTrue();
        result.ErrorMessage.Should().Contain("not available");
    }

    [TestMethod]
    public async Task GetCompletionsAsync_Cancelled_ReturnsEmpty()
    {
        // Given: Cancellation requested
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Getting completions with cancelled token
        var result = await _provider.GetCompletionsAsync(context, cts.Token);

        // Then: Empty result returned (graceful handling)
        result.Items.Count.Should().Be(0);
        result.IsError.Should().BeFalse();
    }

    #endregion
}
