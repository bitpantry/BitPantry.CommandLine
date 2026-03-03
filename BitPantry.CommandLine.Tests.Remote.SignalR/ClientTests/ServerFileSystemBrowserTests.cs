using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

[TestClass]
public class ServerFileSystemBrowserTests
{
    private Mock<IServerProxy> _proxyMock;
    private Mock<ILogger<ServerFileSystemBrowser>> _loggerMock;
    private ServerFileSystemBrowser _browser;

    [TestInitialize]
    public void Setup()
    {
        _proxyMock = TestServerProxyFactory.CreateConnected();
        _loggerMock = new Mock<ILogger<ServerFileSystemBrowser>>();
        _browser = new ServerFileSystemBrowser(_proxyMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task EnumeratePathEntries_WhenConnected_SendsRpcAndReturnsEntries()
    {
        var expectedEntries = new[]
        {
            new PathEntry("docs", true),
            new PathEntry("readme.txt", false),
        };

        _proxyMock
            .Setup(p => p.SendRpcRequest<EnumeratePathEntriesResponse>(
                It.Is<EnumeratePathEntriesRequest>(r =>
                    r.DirectoryPath == "/data" && r.IncludeFiles),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnumeratePathEntriesResponse("corr-1", expectedEntries));

        var result = await _browser.EnumeratePathEntriesAsync("/data", includeFiles: true);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("docs");
        result[0].IsDirectory.Should().BeTrue();
        result[1].Name.Should().Be("readme.txt");
        result[1].IsDirectory.Should().BeFalse();
    }

    [TestMethod]
    public async Task EnumeratePathEntries_WhenDisconnected_ProxyThrows()
    {
        _proxyMock = TestServerProxyFactory.CreateDisconnected();
        _proxyMock
            .Setup(p => p.SendRpcRequest<EnumeratePathEntriesResponse>(
                It.IsAny<EnumeratePathEntriesRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Not connected"));
        _browser = new ServerFileSystemBrowser(_proxyMock.Object, _loggerMock.Object);

        var act = () => _browser.EnumeratePathEntriesAsync("/data", includeFiles: true);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public async Task EnumeratePathEntries_WhenServerReturnsError_ReturnsEmptyList()
    {
        _proxyMock
            .Setup(p => p.SendRpcRequest<EnumeratePathEntriesResponse>(
                It.IsAny<EnumeratePathEntriesRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnumeratePathEntriesResponse("corr-1", Array.Empty<PathEntry>(), "Access denied"));

        var result = await _browser.EnumeratePathEntriesAsync("/secret", includeFiles: true);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task EnumeratePathEntries_DirectoriesOnly_SetsIncludeFilesFalse()
    {
        _proxyMock
            .Setup(p => p.SendRpcRequest<EnumeratePathEntriesResponse>(
                It.Is<EnumeratePathEntriesRequest>(r => !r.IncludeFiles),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnumeratePathEntriesResponse("corr-1", new[]
            {
                new PathEntry("subdir", true),
            }));

        var result = await _browser.EnumeratePathEntriesAsync("/root", includeFiles: false);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("subdir");
        result[0].IsDirectory.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetCurrentDirectoryAsync_ReturnsEmptyString()
    {
        var cwd = await _browser.GetCurrentDirectoryAsync();

        cwd.Should().BeEmpty();
    }
}
