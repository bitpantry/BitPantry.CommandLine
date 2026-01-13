using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Integration tests for secure token handling in file transfers.
    /// These tests verify that tokens are transmitted in Authorization headers
    /// and that the server properly validates them.
    /// </summary>
    [TestClass]
    public class IntegrationTests_TokenSecurity
    {
        [TestMethod]
        public async Task Upload_TokenInHeader_Succeeds()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var localFilePath = env.FileSystem.CreateLocalFile("token-test.txt", "Token header test content");

            var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

            // Act - Upload file (token is in Authorization header by default)
            await fileTransferService.UploadFile(
                localFilePath,
                $"{env.FileSystem.ServerTestFolderPrefix}/token-test.txt",
                null,
                CancellationToken.None);

            // Assert - File should be uploaded successfully
            var expectedPath = Path.Combine(env.FileSystem.ServerTestDir, "token-test.txt");
            File.Exists(expectedPath).Should().BeTrue();
            File.ReadAllText(expectedPath).Should().Be("Token header test content");
        }

        [TestMethod]
        public async Task Upload_MissingAuthHeader_Returns401()
        {
            // Arrange
            using var env = new TestEnvironment();
            
            // Create HTTP client without going through the normal connect flow
            // This allows us to make raw HTTP requests without auth headers
            var httpClient = env.Server.CreateClient();
            
            var localFilePath = env.FileSystem.CreateLocalFile("no-auth-test.txt", "No auth header test");

            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Act - Make request WITHOUT Authorization header
            var response = await httpClient.PostAsync(
                $"/cli/fileupload?toFilePath=no-auth-test.txt&connectionId=fake&correlationId={Guid.NewGuid()}",
                content);

            // Assert - Should return 401 Unauthorized
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "server should reject requests without Authorization header");
        }

        [TestMethod]
        public async Task Upload_InvalidToken_Returns401()
        {
            // Arrange
            using var env = new TestEnvironment();
            var httpClient = env.Server.CreateClient();
            
            var localFilePath = env.FileSystem.CreateLocalFile("invalid-token-test.txt", "Invalid token test");

            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Create request with invalid token
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/cli/fileupload?toFilePath=invalid-token-test.txt&connectionId=fake&correlationId={Guid.NewGuid()}");
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

            // Act
            var response = await httpClient.SendAsync(request);

            // Assert - Should return 401 Unauthorized
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "server should reject requests with invalid tokens");
        }

        [TestMethod]
        public async Task ServerRequestLog_DoesNotContainToken()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var localFilePath = env.FileSystem.CreateLocalFile("log-test.txt", "Log safety test content");

            // Get the current token before upload
            var tokenMgr = env.Cli.Services.GetRequiredService<AccessTokenManager>();
            var currentToken = tokenMgr.CurrentToken?.Token;

            var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

            // Act - Upload file
            await fileTransferService.UploadFile(
                localFilePath,
                $"{env.FileSystem.ServerTestFolderPrefix}/log-test.txt",
                null,
                CancellationToken.None);

            // Assert - Check that the token doesn't appear in logged URLs
            // The FileTransferEndpointService logs request details
            var serverLogs = env.GetServerLogs<BitPantry.CommandLine.Remote.SignalR.Server.Files.FileTransferEndpointService>();
            
            foreach (var log in serverLogs)
            {
                log.Message.Should().NotContain("access_token=",
                    "URL logged should not contain token in query string");
                
                if (!string.IsNullOrEmpty(currentToken))
                {
                    log.Message.Should().NotContain(currentToken,
                        "Actual token value should not appear in logs");
                }
            }
        }
    }
}
