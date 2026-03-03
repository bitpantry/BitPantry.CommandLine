using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

[TestClass]
public class RemotePathEntryProviderTests
{
    private Mock<IRemoteFileSystemBrowser> _browserMock;
    private RemotePathEntryProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _browserMock = new Mock<IRemoteFileSystemBrowser>();
        _provider = new RemotePathEntryProvider(_browserMock.Object);
    }

    [TestMethod]
    public async Task EnumerateAsync_DelegatesToBrowser()
    {
        var expected = new List<PathEntry>
        {
            new("docs", true),
            new("file.txt", false),
        };
        _browserMock
            .Setup(b => b.EnumeratePathEntriesAsync("/some/dir", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _provider.EnumerateAsync("/some/dir", true);

        result.Should().BeEquivalentTo(expected);
        _browserMock.Verify(b => b.EnumeratePathEntriesAsync("/some/dir", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]

    public async Task EnumerateAsync_BrowserThrows_ReturnsEmpty()
    {
        _browserMock
            .Setup(b => b.EnumeratePathEntriesAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        var result = await _provider.EnumerateAsync("/some/dir", true);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task EnumerateAsync_Cancelled_ReturnsEmpty()
    {
        _browserMock
            .Setup(b => b.EnumeratePathEntriesAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await _provider.EnumerateAsync("/some/dir", true);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetCurrentDirectory_ReturnsEmptyString()
    {
        // Remote provider returns empty string — the remote side resolves to its CWD
        _provider.GetCurrentDirectory().Should().BeEmpty();
    }

}
