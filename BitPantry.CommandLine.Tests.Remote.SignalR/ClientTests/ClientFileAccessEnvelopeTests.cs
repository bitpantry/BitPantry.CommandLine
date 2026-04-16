using System;
using System.Collections.Generic;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

[TestClass]
public class ClientFileAccessEnvelopeTests
{
    #region PushMessageType Enum

    [TestMethod]
    public void PushMessageType_HasClientFileUploadRequest()
    {
        var msg = new ClientFileUploadRequestMessage("/local/file.txt", "/tmp/upload-123");
        msg.MessageType.Should().Be(PushMessageType.ClientFileUploadRequest);
    }

    [TestMethod]
    public void PushMessageType_HasClientFileDownloadRequest()
    {
        var msg = new ClientFileDownloadRequestMessage("/server/file.txt", "/local/file.txt", 1024);
        msg.MessageType.Should().Be(PushMessageType.ClientFileDownloadRequest);
    }

    [TestMethod]
    public void PushMessageType_HasClientFileEnumerateRequest()
    {
        var msg = new ClientFileEnumerateRequestMessage("*.txt");
        msg.MessageType.Should().Be(PushMessageType.ClientFileEnumerateRequest);
    }

    #endregion

    #region ServerRequestType Enum

    [TestMethod]
    public void ServerRequestType_HasClientFileAccessResponse()
    {
        var msg = new ClientFileAccessResponseMessage(true);
        msg.RequestType.Should().Be(ServerRequestType.ClientFileAccessResponse);
    }

    [TestMethod]
    public void ServerRequestType_ClientFileAccessResponse_HasValue6()
    {
        ((int)ServerRequestType.ClientFileAccessResponse).Should().Be(6);
    }

    #endregion

    #region ClientFileUploadRequestMessage

    [TestMethod]
    public void UploadRequest_RoundTrips_Properties()
    {
        var msg = new ClientFileUploadRequestMessage("/local/docs/report.pdf", "/tmp/upload-abc");
        var reconstructed = new ClientFileUploadRequestMessage(msg.Data);

        reconstructed.ClientPath.Should().Be("/local/docs/report.pdf");
        reconstructed.ServerTempPath.Should().Be("/tmp/upload-abc");
        reconstructed.MessageType.Should().Be(PushMessageType.ClientFileUploadRequest);
    }

    [TestMethod]
    public void UploadRequest_NullPaths_RoundTrip()
    {
        var msg = new ClientFileUploadRequestMessage(null, null);
        var reconstructed = new ClientFileUploadRequestMessage(msg.Data);

        reconstructed.ClientPath.Should().BeNull();
        reconstructed.ServerTempPath.Should().BeNull();
    }

    #endregion

    #region ClientFileDownloadRequestMessage

    [TestMethod]
    public void DownloadRequest_RoundTrips_Properties()
    {
        var msg = new ClientFileDownloadRequestMessage("/server/data.zip", "/local/downloads/data.zip", 5242880);
        var reconstructed = new ClientFileDownloadRequestMessage(msg.Data);

        reconstructed.ServerPath.Should().Be("/server/data.zip");
        reconstructed.ClientPath.Should().Be("/local/downloads/data.zip");
        reconstructed.FileSize.Should().Be(5242880);
        reconstructed.MessageType.Should().Be(PushMessageType.ClientFileDownloadRequest);
    }

    [TestMethod]
    public void DownloadRequest_ZeroFileSize_RoundTrips()
    {
        var msg = new ClientFileDownloadRequestMessage("/server/empty.txt", "/local/empty.txt", 0);
        var reconstructed = new ClientFileDownloadRequestMessage(msg.Data);

        reconstructed.FileSize.Should().Be(0);
    }

    #endregion

    #region ClientFileEnumerateRequestMessage

    [TestMethod]
    public void EnumerateRequest_RoundTrips_GlobPattern()
    {
        var msg = new ClientFileEnumerateRequestMessage("**/*.log");
        var reconstructed = new ClientFileEnumerateRequestMessage(msg.Data);

        reconstructed.GlobPattern.Should().Be("**/*.log");
        reconstructed.MessageType.Should().Be(PushMessageType.ClientFileEnumerateRequest);
    }

    [TestMethod]
    public void EnumerateRequest_EmptyPattern_RoundTrips()
    {
        var msg = new ClientFileEnumerateRequestMessage("");
        var reconstructed = new ClientFileEnumerateRequestMessage(msg.Data);

        reconstructed.GlobPattern.Should().BeEmpty();
    }

    #endregion

    #region ClientFileAccessResponseMessage

    [TestMethod]
    public void Response_Success_RoundTrips()
    {
        var msg = new ClientFileAccessResponseMessage(true);
        var reconstructed = new ClientFileAccessResponseMessage(msg.Data);

        reconstructed.Success.Should().BeTrue();
        reconstructed.Error.Should().BeNull();
        reconstructed.FileInfoEntries.Should().BeEmpty();
        reconstructed.RequestType.Should().Be(ServerRequestType.ClientFileAccessResponse);
    }

    [TestMethod]
    public void Response_Error_RoundTrips()
    {
        var msg = new ClientFileAccessResponseMessage(false, "File not found");
        var reconstructed = new ClientFileAccessResponseMessage(msg.Data);

        reconstructed.Success.Should().BeFalse();
        reconstructed.Error.Should().Be("File not found");
    }

    [TestMethod]
    public void Response_FileInfoEntries_RoundTrips()
    {
        var entries = new[]
        {
            new FileInfoEntry("docs/readme.md", 1024, new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc)),
            new FileInfoEntry("data/report.csv", 51200, new DateTime(2025, 3, 20, 14, 0, 0, DateTimeKind.Utc)),
        };

        var msg = new ClientFileAccessResponseMessage(true, null, entries);
        var reconstructed = new ClientFileAccessResponseMessage(msg.Data);

        reconstructed.FileInfoEntries.Should().HaveCount(2);
        reconstructed.FileInfoEntries[0].Path.Should().Be("docs/readme.md");
        reconstructed.FileInfoEntries[0].Size.Should().Be(1024);
        reconstructed.FileInfoEntries[1].Path.Should().Be("data/report.csv");
        reconstructed.FileInfoEntries[1].Size.Should().Be(51200);
    }

    [TestMethod]
    public void Response_EmptyFileInfoEntries_RoundTrips()
    {
        var msg = new ClientFileAccessResponseMessage(true, null, Array.Empty<FileInfoEntry>());
        var reconstructed = new ClientFileAccessResponseMessage(msg.Data);

        reconstructed.FileInfoEntries.Should().BeEmpty();
    }

    #endregion
}
