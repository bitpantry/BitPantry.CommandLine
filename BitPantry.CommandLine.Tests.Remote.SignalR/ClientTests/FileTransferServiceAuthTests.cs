using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using FluentAssertions;
using Moq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Tests to verify that FileTransferService sends tokens in Authorization headers,
    /// not in URL query strings.
    /// </summary>
    [TestClass]
    public class FileTransferServiceAuthTests
    {
        private Mock<IServerProxy> _proxyMock;
        private FileTransferServiceTestContext _context;

        [TestInitialize]
        public void Setup()
        {
            _proxyMock = TestServerProxyFactory.CreateConnected();
            _context = TestFileTransferServiceFactory.CreateWithContext(_proxyMock);
        }

        /// <summary>
        /// Implements: Security requirement from contracts - token in Authorization header, not URL.
        /// When UploadFile called, request includes Authorization Bearer header.
        /// </summary>
        [TestMethod]
        public async Task UploadFile_SendsAuthorizationBearerHeader()
        {
            // Arrange
            using var tempFile = new TempFileScope("test content");
            var testToken = await _context.SetupAuthenticatedTokenAsync();
            
            var capturedRequests = new List<CapturedHttpRequest>();
            _context.SetupHttpWithRequestCapture(capturedRequests);

            // Act
            await _context.Service.UploadFile(tempFile.Path, "test.txt", null, CancellationToken.None);

            // Assert
            capturedRequests.Should().HaveCount(1);
            var request = capturedRequests[0];
            request.HasBearerAuth.Should().BeTrue("Authorization Bearer header should be present");
            request.AuthParameter.Should().Be(testToken.Token);
        }

        /// <summary>
        /// Implements: Security requirement from contracts - token never in URL query string.
        /// When UploadFile called, token is not exposed in URL.
        /// </summary>
        [TestMethod]
        public async Task UploadFile_DoesNotIncludeTokenInQueryString()
        {
            // Arrange
            using var tempFile = new TempFileScope("test content");
            var testToken = await _context.SetupAuthenticatedTokenAsync();
            
            var capturedRequests = new List<CapturedHttpRequest>();
            _context.SetupHttpWithRequestCapture(capturedRequests);

            // Act
            await _context.Service.UploadFile(tempFile.Path, "test.txt", null, CancellationToken.None);

            // Assert
            capturedRequests.Should().HaveCount(1);
            var request = capturedRequests[0];
            request.HasTokenInQueryString(testToken.Token).Should().BeFalse("Token should not appear in URL");
        }

        /// <summary>
        /// Implements: DF-013
        /// When DownloadFile called, request sent with Authorization Bearer header.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_SendsAuthorizationBearerHeader()
        {
            // Arrange
            var testToken = await _context.SetupAuthenticatedTokenAsync();
            
            // Use factory helper to setup HTTP with request capture
            var capturedRequests = new List<CapturedHttpRequest>();
            _context.SetupHttpWithRequestCapture(capturedRequests, "test content");

            using var tempFile = new TempFileScope();

            // Act
            await _context.Service.DownloadFile("test-file.txt", tempFile.Path, CancellationToken.None);

            // Assert
            capturedRequests.Should().HaveCount(1);
            var request = capturedRequests[0];
            request.HasBearerAuth.Should().BeTrue("Authorization Bearer header should be present");
            request.AuthParameter.Should().Be(testToken.Token);
        }

        /// <summary>
        /// Implements: DF-013 (negative case), Security requirement from contracts/download-api.md.
        /// When DownloadFile called, token is not exposed in URL query string.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_DoesNotIncludeTokenInQueryString()
        {
            // Arrange
            var testToken = await _context.SetupAuthenticatedTokenAsync();
            
            // Use factory helper to setup HTTP with request capture
            var capturedRequests = new List<CapturedHttpRequest>();
            _context.SetupHttpWithRequestCapture(capturedRequests, "test content");

            using var tempFile = new TempFileScope();

            // Act
            await _context.Service.DownloadFile("test-file.txt", tempFile.Path, CancellationToken.None);

            // Assert - token should NOT be in query string
            capturedRequests.Should().HaveCount(1);
            var request = capturedRequests[0];
            request.HasTokenInQueryString(testToken.Token).Should().BeFalse("Token should not appear in URL");
        }
    }
}

