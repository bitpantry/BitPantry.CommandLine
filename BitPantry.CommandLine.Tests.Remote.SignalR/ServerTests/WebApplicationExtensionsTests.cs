using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Remote.SignalR.Server.Authentication;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using BitPantry.CommandLine.Tests.Infrastructure.Authentication;
using System.Net;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class WebApplicationExtensionsTests
    {
        #region MapCommandLineHub Conditional Endpoint Tests

        [TestMethod]
        public async Task MapCommandLineHub_FileTransferDisabled_DoesNotMapUploadEndpoint()
        {
            // Arrange
            using var host = await CreateTestHost(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            var client = host.GetTestClient();

            // Act - Try to hit the upload endpoint
            var response = await client.PostAsync("/cli/fileupload?toFilePath=test.txt&connectionId=conn&correlationId=corr", null);

            // Assert - Should return 404 because endpoint is not mapped
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "Upload endpoint should not be mapped when file transfer is disabled");
        }

        [TestMethod]
        public async Task MapCommandLineHub_FileTransferDisabled_DoesNotMapDownloadEndpoint()
        {
            // Arrange
            using var host = await CreateTestHost(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            var client = host.GetTestClient();

            // Act - Try to hit the download endpoint
            var response = await client.GetAsync("/cli/filedownload?filePath=test.txt");

            // Assert - Should return 404 because endpoint is not mapped
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "Download endpoint should not be mapped when file transfer is disabled");
        }

        [TestMethod]
        public async Task MapCommandLineHub_FileTransferDisabled_DoesNotMapFilesExistEndpoint()
        {
            // Arrange
            using var host = await CreateTestHost(opt =>
            {
                opt.FileTransferOptions.Disable();
            });

            var client = host.GetTestClient();

            // Act - Try to hit the files-exist endpoint
            var content = new StringContent("{\"filePaths\": []}", System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/cli/files/exists", content);

            // Assert - Should return 404 because endpoint is not mapped
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "Files-exist endpoint should not be mapped when file transfer is disabled");
        }

        [TestMethod]
        public async Task MapCommandLineHub_FileTransferEnabled_MapsUploadEndpoint()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                using var host = await CreateTestHost(opt =>
                {
                    opt.FileTransferOptions.StorageRootPath = tempDir;
                });

                var client = host.GetTestClient();

                // Act - Try to hit the upload endpoint (will fail validation but proves endpoint exists)
                var response = await client.PostAsync("/cli/fileupload?toFilePath=test.txt&connectionId=conn&correlationId=corr", null);

                // Assert - Should NOT return 404 (endpoint is mapped, may return other error due to missing data)
                response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
                    "Upload endpoint should be mapped when file transfer is enabled");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [TestMethod]
        public async Task MapCommandLineHub_FileTransferEnabled_MapsDownloadEndpoint()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                using var host = await CreateTestHost(opt =>
                {
                    opt.FileTransferOptions.StorageRootPath = tempDir;
                });

                var client = host.GetTestClient();

                // Act - Try to hit the download endpoint
                // Note: Returns 404 for file not found OR 403 for path traversal - both indicate endpoint is mapped
                var response = await client.GetAsync("/cli/filedownload?filePath=test.txt");

                // Assert - Should NOT return 404 from routing (endpoint is mapped)
                // The handler may return 404 for "file not found" or 403 for path violations
                // We accept 403 (Forbidden) or 500 (Internal Server Error) as proof the endpoint exists
                var acceptableStatuses = new[] { HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError, HttpStatusCode.OK };
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Check if it's a "file not found" (handler response) vs "route not found" (framework response)
                    var content = await response.Content.ReadAsStringAsync();
                    // If the response has meaningful content from the handler, the endpoint is mapped
                    content.Should().NotBeEmpty("Download endpoint should be mapped - 404 should be file not found, not route not found");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [TestMethod]
        public async Task MapCommandLineHub_FileTransferEnabled_MapsFilesExistEndpoint()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                using var host = await CreateTestHost(opt =>
                {
                    opt.FileTransferOptions.StorageRootPath = tempDir;
                });

                var client = host.GetTestClient();

                // Act - Try to hit the files-exist endpoint
                var content = new StringContent("{\"filePaths\": []}", System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/cli/files/exists", content);

                // Assert - Should NOT return 404 (endpoint is mapped)
                response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
                    "Files-exist endpoint should be mapped when file transfer is enabled");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [TestMethod]
        public async Task MapCommandLineHub_NoJwtAuth_DoesNotMapTokenRequestEndpoint()
        {
            // Arrange
            using var host = await CreateTestHost(opt =>
            {
                // Don't configure JWT auth - token endpoints should not be mapped
            });

            var client = host.GetTestClient();

            // Act - Try to hit the default token request endpoint
            var content = new StringContent("{\"apiKey\": \"test\"}", System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/cli-auth/token-request", content);

            // Assert - Should return 404 because endpoint is not mapped
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "Token request endpoint should not be mapped when JWT auth is not configured");
        }

        [TestMethod]
        public async Task MapCommandLineHub_NoJwtAuth_DoesNotMapTokenRefreshEndpoint()
        {
            // Arrange
            using var host = await CreateTestHost(opt =>
            {
                // Don't configure JWT auth - token endpoints should not be mapped
            });

            var client = host.GetTestClient();

            // Act - Try to hit the default token refresh endpoint
            var content = new StringContent("{\"refreshToken\": \"test\"}", System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/cli-auth/token-refresh", content);

            // Assert - Should return 404 because endpoint is not mapped
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "Token refresh endpoint should not be mapped when JWT auth is not configured");
        }

        [TestMethod]
        public async Task MapCommandLineHub_WithJwtAuth_MapsTokenRequestEndpoint()
        {
            // Arrange
            using var host = await CreateTestHost(opt =>
            {
                opt.AddJwtAuthentication<TestApiKeyStore, TestRefreshTokenStore>(
                    "somereallylongstringwithsomenumbersattheend1234567890-");
            });

            var client = host.GetTestClient();

            // Act - Try to hit the token request endpoint
            var content = new StringContent("{\"apiKey\": \"test\"}", System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/cli-auth/token-request", content);

            // Assert - Should NOT return 404 (endpoint is mapped, may return other error)
            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
                "Token request endpoint should be mapped when JWT auth is configured");
        }

        [TestMethod]
        public async Task MapCommandLineHub_WithJwtAuth_MapsTokenRefreshEndpoint()
        {
            // Arrange
            using var host = await CreateTestHost(opt =>
            {
                opt.AddJwtAuthentication<TestApiKeyStore, TestRefreshTokenStore>(
                    "somereallylongstringwithsomenumbersattheend1234567890-");
            });

            var client = host.GetTestClient();

            // Act - Try to hit the token refresh endpoint
            var content = new StringContent("{\"refreshToken\": \"test\"}", System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/cli-auth/token-refresh", content);

            // Assert - Should NOT return 404 (endpoint is mapped, may return other error)
            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
                "Token refresh endpoint should be mapped when JWT auth is configured");
        }

        #endregion

        #region UseCommandLineTokenValidation Conditional Middleware Tests

        [TestMethod]
        public void UseCommandLineTokenValidation_NoJwtAuth_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddCommandLineHub(opt =>
            {
                // Don't configure JWT auth
            });

            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act - should be a no-op, not throw
            var act = () => appBuilder.UseCommandLineTokenValidation();

            // Assert
            act.Should().NotThrow(
                "UseCommandLineTokenValidation should be a no-op when JWT auth is not configured");
        }

        [TestMethod]
        public void UseCommandLineTokenValidation_WithJwtAuth_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCommandLineHub(opt =>
            {
                opt.AddJwtAuthentication<TestApiKeyStore, TestRefreshTokenStore>(
                    "somereallylongstringwithsomenumbersattheend1234567890-");
            });

            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act - should add middleware without throwing
            var act = () => appBuilder.UseCommandLineTokenValidation();

            // Assert
            act.Should().NotThrow(
                "UseCommandLineTokenValidation should add middleware when JWT auth is configured");
        }

        [TestMethod]
        public async Task UseCommandLineTokenValidation_NoJwtAuth_RequestsPassThrough()
        {
            // Arrange - no JWT auth configured
            using var host = await CreateTestHost(opt =>
            {
                // Don't configure JWT auth
            }, useTokenValidation: true);

            var client = host.GetTestClient();

            // Act - Try to hit the hub negotiate endpoint (doesn't require auth when not configured)
            var response = await client.PostAsync("/cli/negotiate", null);

            // Assert - Request should pass through (not get 401 from middleware)
            // The response may be something other than 401 (e.g., 400 for missing parameters)
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
                "Requests should pass through when JWT auth is not configured");
        }

        [TestMethod]
        public async Task UseCommandLineTokenValidation_WithJwtAuth_UnauthorizedWithoutToken()
        {
            // Arrange - JWT auth configured
            using var host = await CreateTestHost(opt =>
            {
                opt.AddJwtAuthentication<TestApiKeyStore, TestRefreshTokenStore>(
                    "somereallylongstringwithsomenumbersattheend1234567890-");
            }, useTokenValidation: true);

            var client = host.GetTestClient();

            // Act - Try to hit the hub negotiate endpoint without a token
            var response = await client.PostAsync("/cli/negotiate", null);

            // Assert - Should get 401 from TokenValidationMiddleware
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "Requests to hub endpoints should require auth when JWT auth is configured");
        }

        #endregion

        #region Helper Methods

        private static async Task<IHost> CreateTestHost(
            Action<CommandLineServerOptions> configure,
            bool useTokenValidation = false)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddSignalR();
                        services.AddLogging();
                        services.AddCommandLineHub(configure);
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();

                        if (useTokenValidation)
                        {
                            app.UseCommandLineTokenValidation();
                        }

                        app.UseEndpoints(endpoints => endpoints.MapCommandLineHub());
                    });
                })
                .Build();

            await host.StartAsync();
            return host;
        }

        #endregion
    }
}
