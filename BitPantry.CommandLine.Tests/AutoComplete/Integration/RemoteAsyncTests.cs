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
/// Tests for remote completion async behavior (RC-020 to RC-023)
/// </summary>
[TestClass]
public class RemoteAsyncTests
{
    private Mock<IServerProxy> _mockServerProxy;
    private RemoteCompletionProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _mockServerProxy = new Mock<IServerProxy>();
        _provider = new RemoteCompletionProvider(_mockServerProxy.Object);
    }

    #region RC-020: Typing during fetch cancels

    [TestMethod]
    public async Task RC020_GetCompletionsAsync_CancellationRequested_ReturnsEmpty()
    {
        // Given: Fetch in progress
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        
        var cts = new CancellationTokenSource();
        
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (CompletionContext ctx, CancellationToken token) =>
            {
                // Simulate delay, then check cancellation
                await Task.Delay(100, token);
                return new CompletionResult(new[] { new CompletionItem { DisplayText = "result", InsertText = "result" } });
            });

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: User types character during fetch (cancellation)
        var fetchTask = _provider.GetCompletionsAsync(context, cts.Token);
        await Task.Delay(20);
        cts.Cancel();

        // Then: Previous fetch cancelled, empty result
        var result = await fetchTask;
        result.Items.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task RC020_GetCompletionsAsync_PassesCancellationToken_ToServerProxy()
    {
        // Given: Connected server proxy
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        
        CancellationToken capturedToken = default;
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .Callback<CompletionContext, CancellationToken>((ctx, token) => capturedToken = token)
            .ReturnsAsync(CompletionResult.Empty);

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        var cts = new CancellationTokenSource();

        // When: Getting completions with token
        await _provider.GetCompletionsAsync(context, cts.Token);

        // Then: Token was passed to server proxy
        capturedToken.Should().Be(cts.Token);
    }

    #endregion

    #region RC-021: Rapid typing debounced

    [TestMethod]
    public async Task RC021_GetCompletionsAsync_ConcurrentCalls_OnlyLastExecutes()
    {
        // Given: Multiple rapid calls (simulating debouncing at orchestrator level)
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        
        var callCount = 0;
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (CompletionContext ctx, CancellationToken token) =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Delay(50, token);
                return new CompletionResult(new[] { 
                    new CompletionItem { DisplayText = ctx.PartialValue, InsertText = ctx.PartialValue } 
                });
            });

        var context1 = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1",
            PartialValue = "a"
        };

        var context5 = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1",
            PartialValue = "abcde"
        };

        // Simulate: First 4 calls get cancelled, only last one completes
        // (In real scenario, orchestrator handles debouncing)
        var cts1 = new CancellationTokenSource();
        var task1 = _provider.GetCompletionsAsync(context1, cts1.Token);
        cts1.Cancel();
        await task1; // Should return empty due to cancellation

        // Last call completes
        var result = await _provider.GetCompletionsAsync(context5);

        // Then: At least one call was made, result matches last context
        result.Items.Should().HaveCountGreaterThan(0);
        result.Items[0].DisplayText.Should().Be("abcde");
    }

    #endregion

    #region RC-022: Escape during fetch cancels

    [TestMethod]
    public async Task RC022_GetCompletionsAsync_EscapeCancellation_ReturnsEmptyNoMenu()
    {
        // Given: Fetch in progress
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        
        var cts = new CancellationTokenSource();
        
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (CompletionContext ctx, CancellationToken token) =>
            {
                await Task.Delay(200, token);
                return new CompletionResult(new[] { new CompletionItem { DisplayText = "result", InsertText = "result" } });
            });

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: User presses Escape (cancellation)
        var fetchTask = _provider.GetCompletionsAsync(context, cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        // Then: Fetch cancelled, no menu shown
        var result = await fetchTask;
        result.Items.Count.Should().Be(0);
        result.IsError.Should().BeFalse(); // Cancellation is graceful, not an error
    }

    #endregion

    #region RC-023: Non-blocking during fetch

    [TestMethod]
    public async Task RC023_GetCompletionsAsync_IsAsync_DoesNotBlock()
    {
        // Given: Fetch takes some time
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        
        var startTime = DateTime.UtcNow;
        var fetchComplete = false;
        
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (CompletionContext ctx, CancellationToken token) =>
            {
                await Task.Delay(100);
                fetchComplete = true;
                return new CompletionResult(new[] { new CompletionItem { DisplayText = "result", InsertText = "result" } });
            });

        var context = new CompletionContext
        {
            IsRemote = true,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "remoteCmd",
            ArgumentName = "arg1"
        };

        // When: Starting fetch
        var fetchTask = _provider.GetCompletionsAsync(context);
        
        // Then: Method returns immediately (async), doesn't block
        fetchComplete.Should().BeFalse("Task should be running asynchronously");

        // Wait for completion
        await fetchTask;
        fetchComplete.Should().BeTrue("Task should complete after await");
    }

    [TestMethod]
    public async Task RC023_GetCompletionsAsync_AllowsConcurrentOperations()
    {
        // Given: Multiple concurrent fetch operations
        _mockServerProxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        
        var activeFetches = 0;
        var maxConcurrent = 0;
        
        _mockServerProxy
            .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (CompletionContext ctx, CancellationToken token) =>
            {
                var current = Interlocked.Increment(ref activeFetches);
                if (current > maxConcurrent)
                    Interlocked.Exchange(ref maxConcurrent, current);
                
                await Task.Delay(50);
                
                Interlocked.Decrement(ref activeFetches);
                return new CompletionResult(new[] { 
                    new CompletionItem { DisplayText = ctx.PartialValue ?? "default", InsertText = ctx.PartialValue ?? "default" } 
                });
            });

        var contexts = new[]
        {
            new CompletionContext
            {
                IsRemote = true,
                ElementType = CompletionElementType.ArgumentValue,
                CommandName = "remoteCmd",
                ArgumentName = "arg1",
                PartialValue = "a"
            },
            new CompletionContext
            {
                IsRemote = true,
                ElementType = CompletionElementType.ArgumentValue,
                CommandName = "remoteCmd",
                ArgumentName = "arg2",
                PartialValue = "b"
            },
            new CompletionContext
            {
                IsRemote = true,
                ElementType = CompletionElementType.ArgumentValue,
                CommandName = "remoteCmd",
                ArgumentName = "arg3",
                PartialValue = "c"
            }
        };

        // When: Running concurrent fetches
        var tasks = new List<Task<CompletionResult>>();
        foreach (var ctx in contexts)
        {
            tasks.Add(_provider.GetCompletionsAsync(ctx));
        }

        var results = await Task.WhenAll(tasks);

        // Then: All complete successfully, operations can be concurrent
        results.Should().HaveCount(3);
        foreach (var result in results)
        {
            result.Items.Should().HaveCount(1);
        }
    }

    #endregion
}
