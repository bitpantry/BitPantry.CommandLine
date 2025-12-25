using System;
using System.Collections.Generic;
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
/// Tests for remote completion scenarios (RC-001 to RC-003)
/// </summary>
[TestClass]
public class RemoteCompletionTests
{
    private Mock<IServerProxy> _mockServerProxy;
    private RemoteCompletionProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _mockServerProxy = new Mock<IServerProxy>();
        _provider = new RemoteCompletionProvider(_mockServerProxy.Object);
    }

    #region RC-001: Loading indicator shown

    [TestMethod]
    public void RC001_CanHandle_RemoteCommand_WithConnectedProxy_ReturnsTrue()
    {
        // Given: Connected to remote, arg needs server fetch
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Provider checks if it can handle
        var canHandle = _provider.CanHandle(context);

        // Then: Provider can handle remote context
        canHandle.Should().BeTrue();
    }

    [TestMethod]
    public void RC001_CanHandle_LocalCommand_ReturnsFalse()
    {
        // Given: Local command (not remote)
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        var context = new CompletionContext
        {
            IsRemote = false,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "localCmd",
            ArgumentName = "arg1"
        };

        // When: Provider checks if it can handle
        var canHandle = _provider.CanHandle(context);

        // Then: Provider cannot handle local context
        canHandle.Should().BeFalse();
    }

    [TestMethod]
    public void RC001_Priority_IsHigherThanLocalProviders()
    {
        // Given: Remote completion provider
        // When: Checking priority
        var priority = _provider.Priority;

        // Then: Priority should be higher than standard providers (100+)
        priority.Should().BeGreaterThanOrEqualTo(200);
    }

    #endregion

    #region RC-002: Results replace loading

    [TestMethod]
    public async Task RC002_GetCompletionsAsync_ServerResponds_ReturnsItems()
    {
        // Given: Server responds with 5 items
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        var expectedItems = new List<CompletionItem>
        {
            new() { DisplayText = "item1", InsertText = "item1" },
            new() { DisplayText = "item2", InsertText = "item2" },
            new() { DisplayText = "item3", InsertText = "item3" },
            new() { DisplayText = "item4", InsertText = "item4" },
            new() { DisplayText = "item5", InsertText = "item5" }
        };
        var serverResult = new CompletionResult(expectedItems);
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serverResult);

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Getting completions
        var result = await _provider.GetCompletionsAsync(context);

        // Then: Menu shows 5 items
        result.Items.Count.Should().Be(5);
        result.IsError.Should().BeFalse();
        result.IsTimedOut.Should().BeFalse();
    }

    [TestMethod]
    public async Task RC002_GetCompletionsAsync_EmptyResult_ReturnsEmpty()
    {
        // Given: Server responds with empty result
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompletionResult.Empty);

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Getting completions
        var result = await _provider.GetCompletionsAsync(context);

        // Then: Result is empty
        result.Items.Count.Should().Be(0);
    }

    #endregion

    #region RC-003: Remote values filtered locally

    [TestMethod]
    public async Task RC003_GetCompletionsAsync_WithPartialValue_FiltersLocally()
    {
        // Given: Cached remote results exist (simulated by server response)
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        var allItems = new List<CompletionItem>
        {
            new() { DisplayText = "dev", InsertText = "dev" },
            new() { DisplayText = "staging", InsertText = "staging" },
            new() { DisplayText = "production", InsertText = "production" }
        };
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(allItems));

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "env",
            PartialValue = "de"  // User types partial match
        };

        // When: Getting completions
        var result = await _provider.GetCompletionsAsync(context);

        // Then: Filtering happens locally (matches "dev")
        result.Items.Count.Should().Be(1);
        result.Items[0].DisplayText.Should().Be("dev");
    }

    [TestMethod]
    public async Task RC003_GetCompletionsAsync_CaseInsensitiveFilter()
    {
        // Given: Remote results with mixed case
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        var allItems = new List<CompletionItem>
        {
            new() { DisplayText = "Development", InsertText = "Development" },
            new() { DisplayText = "Staging", InsertText = "Staging" },
            new() { DisplayText = "Production", InsertText = "Production" }
        };
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(allItems));

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "env",
            PartialValue = "dev"  // lowercase filter
        };

        // When: Getting completions
        var result = await _provider.GetCompletionsAsync(context);

        // Then: Case-insensitive match works
        result.Items.Count.Should().Be(1);
        result.Items[0].DisplayText.Should().Be("Development");
    }

    [TestMethod]
    public async Task RC003_GetCompletionsAsync_NoMatchingPrefix_ReturnsEmpty()
    {
        // Given: Remote results that don't match prefix
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        var allItems = new List<CompletionItem>
        {
            new() { DisplayText = "dev", InsertText = "dev" },
            new() { DisplayText = "staging", InsertText = "staging" }
        };
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(allItems));

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "env",
            PartialValue = "prod"  // No match
        };

        // When: Getting completions
        var result = await _provider.GetCompletionsAsync(context);

        // Then: Empty result (no matches)
        result.Items.Count.Should().Be(0);
    }

    #endregion
}
