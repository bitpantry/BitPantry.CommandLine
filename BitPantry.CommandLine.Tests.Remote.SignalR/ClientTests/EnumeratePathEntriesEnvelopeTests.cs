using System.Collections.Generic;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

[TestClass]
public class EnumeratePathEntriesEnvelopeTests
{
    #region ServerRequest (Client→Server)

    [TestMethod]
    public void Request_HasCorrectRequestType()
    {
        var req = new EnumeratePathEntriesRequest("/some/dir", true);
        req.RequestType.Should().Be(ServerRequestType.EnumeratePathEntries);
    }

    [TestMethod]
    public void Request_RoundTrips_DirectoryPath_And_IncludeFiles()
    {
        var req = new EnumeratePathEntriesRequest("/some/dir", true);

        // Simulate serialization round-trip via Data dictionary
        var reconstructed = new EnumeratePathEntriesRequest(req.Data);

        reconstructed.DirectoryPath.Should().Be("/some/dir");
        reconstructed.IncludeFiles.Should().BeTrue();
    }

    [TestMethod]
    public void Request_RoundTrips_IncludeFiles_False()
    {
        var req = new EnumeratePathEntriesRequest("/dir", false);
        var reconstructed = new EnumeratePathEntriesRequest(req.Data);

        reconstructed.IncludeFiles.Should().BeFalse();
    }

    [TestMethod]
    public void Request_EmptyDirectoryPath()
    {
        var req = new EnumeratePathEntriesRequest("", true);
        var reconstructed = new EnumeratePathEntriesRequest(req.Data);

        reconstructed.DirectoryPath.Should().BeEmpty();
    }

    #endregion

    #region Response

    [TestMethod]
    public void Response_RoundTrips_PathEntries()
    {
        var entries = new[]
        {
            new PathEntry("docs", true),
            new PathEntry("file.txt", false),
        };

        var resp = new EnumeratePathEntriesResponse("corr-1", entries);
        var reconstructed = new EnumeratePathEntriesResponse(resp.Data);

        reconstructed.CorrelationId.Should().Be("corr-1");
        reconstructed.Entries.Should().HaveCount(2);
        reconstructed.Entries[0].Name.Should().Be("docs");
        reconstructed.Entries[0].IsDirectory.Should().BeTrue();
        reconstructed.Entries[1].Name.Should().Be("file.txt");
        reconstructed.Entries[1].IsDirectory.Should().BeFalse();
    }

    [TestMethod]
    public void Response_RoundTrips_Error()
    {
        var resp = new EnumeratePathEntriesResponse("corr-2", null, "Something went wrong");
        var reconstructed = new EnumeratePathEntriesResponse(resp.Data);

        reconstructed.Error.Should().Be("Something went wrong");
        reconstructed.Entries.Should().BeEmpty();
    }

    [TestMethod]
    public void Response_EmptyEntries_RoundTrips()
    {
        var resp = new EnumeratePathEntriesResponse("corr-3", Array.Empty<PathEntry>());
        var reconstructed = new EnumeratePathEntriesResponse(resp.Data);

        reconstructed.Entries.Should().BeEmpty();
        reconstructed.Error.Should().BeNull();
    }

    #endregion

    #region ClientRequest (Server→Client)

    [TestMethod]
    public void ClientRequest_HasCorrectRequestType()
    {
        var req = new ClientEnumeratePathEntriesRequest("/client/dir", true);
        req.RequestType.Should().Be(ClientRequestType.EnumeratePathEntries);
    }

    [TestMethod]
    public void ClientRequest_RoundTrips_DirectoryPath_And_IncludeFiles()
    {
        var req = new ClientEnumeratePathEntriesRequest("/client/dir", false);
        var reconstructed = new ClientEnumeratePathEntriesRequest(req.Data);

        reconstructed.DirectoryPath.Should().Be("/client/dir");
        reconstructed.IncludeFiles.Should().BeFalse();
    }

    #endregion
}
