using BitPantry.CommandLine.Remote.SignalR.Client;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

/// <summary>
/// Tests for FileDownloadProgressUpdateFunctionRegistry.
/// Implements: CV-018, CV-019, CV-020, DF-009, T083, T084, T085, T087
/// </summary>
[TestClass]
public class FileDownloadProgressRegistryTests
{
    private Mock<ILogger<FileDownloadProgressUpdateFunctionRegistry>> _loggerMock = null!;
    private FileDownloadProgressUpdateFunctionRegistry _registry = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<FileDownloadProgressUpdateFunctionRegistry>>();
        _registry = new FileDownloadProgressUpdateFunctionRegistry(_loggerMock.Object);
    }

    #region CV-018: Register returns correlationId (T083)

    /// <summary>
    /// Implements: CV-018, T083
    /// When: Callback provided to Register
    /// Then: Returns correlationId, callback stored
    /// </summary>
    [TestMethod]
    public async Task Register_WithCallback_ReturnsCorrelationId()
    {
        // Arrange
        Func<FileDownloadProgress, Task> callback = _ => Task.CompletedTask;

        // Act
        var correlationId = await _registry.Register(callback);

        // Assert
        correlationId.Should().NotBeNullOrEmpty("Register should return a correlationId");
        Guid.TryParse(correlationId, out _).Should().BeTrue("correlationId should be a valid GUID");
    }

    /// <summary>
    /// Implements: CV-018, T083
    /// When: Multiple callbacks registered
    /// Then: Each returns unique correlationId
    /// </summary>
    [TestMethod]
    public async Task Register_MultipleCallbacks_ReturnsUniqueCorrelationIds()
    {
        // Arrange
        Func<FileDownloadProgress, Task> callback1 = _ => Task.CompletedTask;
        Func<FileDownloadProgress, Task> callback2 = _ => Task.CompletedTask;

        // Act
        var correlationId1 = await _registry.Register(callback1);
        var correlationId2 = await _registry.Register(callback2);

        // Assert
        correlationId1.Should().NotBe(correlationId2, "each registration should return a unique correlationId");
    }

    #endregion

    #region CV-019: Unregister removes callback (T084)

    /// <summary>
    /// Implements: CV-019, T084
    /// When: Valid correlationId passed to Unregister
    /// Then: Callback removed from registry (UpdateProgress no longer invokes it)
    /// </summary>
    [TestMethod]
    public async Task Unregister_ValidCorrelationId_RemovesCallback()
    {
        // Arrange
        var wasInvoked = false;
        Func<FileDownloadProgress, Task> callback = _ => 
        {
            wasInvoked = true;
            return Task.CompletedTask;
        };
        var correlationId = await _registry.Register(callback);

        // Act
        await _registry.Unregister(correlationId);
        await _registry.UpdateProgress(correlationId, new FileDownloadProgress(100, 1000, correlationId));

        // Assert
        wasInvoked.Should().BeFalse("callback should NOT be invoked after unregister");
    }

    /// <summary>
    /// Implements: CV-019, T084
    /// When: Null or empty correlationId passed to Unregister
    /// Then: No exception thrown (graceful handling)
    /// </summary>
    [TestMethod]
    public async Task Unregister_NullOrEmptyCorrelationId_NoException()
    {
        // Act & Assert - should not throw
        await _registry.Invoking(r => r.Unregister(null!))
            .Should().NotThrowAsync();
        await _registry.Invoking(r => r.Unregister(string.Empty))
            .Should().NotThrowAsync();
    }

    #endregion

    #region CV-020, DF-009: Progress message invokes callback (T085, T087)

    /// <summary>
    /// Implements: CV-020, DF-009, T085, T087
    /// When: Progress message received (UpdateProgress called)
    /// Then: Matching callback invoked with progress data
    /// </summary>
    [TestMethod]
    public async Task UpdateProgress_MatchingCorrelationId_InvokesCallback()
    {
        // Arrange
        FileDownloadProgress? receivedProgress = null;
        Func<FileDownloadProgress, Task> callback = progress =>
        {
            receivedProgress = progress;
            return Task.CompletedTask;
        };
        var correlationId = await _registry.Register(callback);
        var expectedProgress = new FileDownloadProgress(512, 1024, correlationId);

        // Act
        await _registry.UpdateProgress(correlationId, expectedProgress);

        // Assert
        receivedProgress.Should().NotBeNull("callback should be invoked");
        receivedProgress!.TotalRead.Should().Be(512);
        receivedProgress.TotalSize.Should().Be(1024);
        receivedProgress.PercentComplete.Should().Be(50);
    }

    /// <summary>
    /// Implements: CV-020, DF-009, T085, T087
    /// When: UpdateProgress called with non-matching correlationId
    /// Then: Registered callback NOT invoked
    /// </summary>
    [TestMethod]
    public async Task UpdateProgress_NonMatchingCorrelationId_DoesNotInvokeCallback()
    {
        // Arrange
        var wasInvoked = false;
        Func<FileDownloadProgress, Task> callback = _ =>
        {
            wasInvoked = true;
            return Task.CompletedTask;
        };
        await _registry.Register(callback);
        var nonMatchingId = Guid.NewGuid().ToString();

        // Act
        await _registry.UpdateProgress(nonMatchingId, new FileDownloadProgress(100, 1000, nonMatchingId));

        // Assert
        wasInvoked.Should().BeFalse("callback should NOT be invoked for non-matching correlationId");
    }

    /// <summary>
    /// Implements: CV-020, DF-009, T085, T087
    /// When: Multiple callbacks registered, progress sent to one
    /// Then: Only matching callback invoked
    /// </summary>
    [TestMethod]
    public async Task UpdateProgress_MultipleCallbacks_OnlyMatchingInvoked()
    {
        // Arrange
        var callback1Invoked = false;
        var callback2Invoked = false;
        
        Func<FileDownloadProgress, Task> callback1 = _ => { callback1Invoked = true; return Task.CompletedTask; };
        Func<FileDownloadProgress, Task> callback2 = _ => { callback2Invoked = true; return Task.CompletedTask; };
        
        var correlationId1 = await _registry.Register(callback1);
        var correlationId2 = await _registry.Register(callback2);

        // Act - send progress only to callback2
        await _registry.UpdateProgress(correlationId2, new FileDownloadProgress(100, 1000, correlationId2));

        // Assert
        callback1Invoked.Should().BeFalse("callback1 should NOT be invoked");
        callback2Invoked.Should().BeTrue("callback2 should be invoked for its correlationId");
    }

    #endregion
}
