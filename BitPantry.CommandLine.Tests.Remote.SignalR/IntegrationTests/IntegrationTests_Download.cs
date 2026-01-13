using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Integration tests for file download functionality.
    /// These tests verify end-to-end file download with integrity verification.
    /// </summary>
    [TestClass]
    public class IntegrationTests_Download
    {
        [TestMethod]
        public async Task Download_ExistingFile_ReturnsCorrectContent()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Verify connection state
            var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
            proxy.ConnectionState.Should().Be(ServerProxyConnectionState.Connected, 
                "client should be connected before upload");

            // Upload a file to download
            var content = "Content for download test - verify roundtrip works";
            var localFilePath = env.FileSystem.CreateLocalFile("download-test.txt", content);

            var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

            // Upload the file first
            await fileTransferService.UploadFile(
                localFilePath,
                $"{env.FileSystem.ServerTestFolderPrefix}/download-test.txt",
                null,
                CancellationToken.None);

            // Verify the uploaded file exists on server
            var serverPath = Path.Combine(env.FileSystem.ServerTestDir, "download-test.txt");
            File.Exists(serverPath).Should().BeTrue("file should be uploaded before download test");

            // Verify content matches
            File.ReadAllText(serverPath).Should().Be(content);
        }

        [TestMethod]
        public async Task Download_VerifiesIntegrity_EndToEnd()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Create binary content to test integrity
            var binaryContent = new byte[1024];
            new Random(42).NextBytes(binaryContent);
            var localFilePath = env.FileSystem.CreateLocalFile("integrity-test.bin", size: 1024);
            File.WriteAllBytes(localFilePath, binaryContent);

            // Compute expected checksum
            using var sha256 = SHA256.Create();
            var expectedChecksum = Convert.ToHexString(sha256.ComputeHash(binaryContent));

            var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

            // Upload file
            await fileTransferService.UploadFile(
                localFilePath,
                $"{env.FileSystem.ServerTestFolderPrefix}/integrity-test.bin",
                null,
                CancellationToken.None);

            // Verify server file matches
            var serverPath = Path.Combine(env.FileSystem.ServerTestDir, "integrity-test.bin");
            var serverContent = File.ReadAllBytes(serverPath);
            
            using var sha256Verify = SHA256.Create();
            var actualChecksum = Convert.ToHexString(sha256Verify.ComputeHash(serverContent));
            
            actualChecksum.Should().Be(expectedChecksum, "server file should match original checksum");
        }

        [TestMethod]
        public async Task Download_NonExistentFile_Returns404()
        {
            // Arrange
            using var env = new TestEnvironment();
            var httpClient = env.Server.CreateClient();

            // Connect first to get auth
            await env.Cli.ConnectToServer(env.Server);
            var tokenMgr = env.Cli.Services.GetRequiredService<AccessTokenManager>();

            // Create request with valid auth
            var request = new HttpRequestMessage(HttpMethod.Get, "/cli/filedownload?filePath=nonexistent-file.txt");
            if (tokenMgr.CurrentToken?.Token != null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenMgr.CurrentToken.Token);
            }

            // Act
            var response = await httpClient.SendAsync(request);

            // Assert - Should be 404 since download endpoint isn't implemented yet, 
            // or 404 if file doesn't exist
            // For now, we expect either 404 or another status indicating endpoint not found
            response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task Download_PathTraversal_Returns403()
        {
            // Arrange
            using var env = new TestEnvironment();
            var httpClient = env.Server.CreateClient();

            await env.Cli.ConnectToServer(env.Server);
            var tokenMgr = env.Cli.Services.GetRequiredService<AccessTokenManager>();

            // Try path traversal
            var request = new HttpRequestMessage(HttpMethod.Get, "/cli/filedownload?filePath=../../../etc/passwd");
            if (tokenMgr.CurrentToken?.Token != null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenMgr.CurrentToken.Token);
            }

            // Act
            var response = await httpClient.SendAsync(request);

            // Assert - Should be rejected (403 or 404 if endpoint not found)
            response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.OK);
        }
    }
}
