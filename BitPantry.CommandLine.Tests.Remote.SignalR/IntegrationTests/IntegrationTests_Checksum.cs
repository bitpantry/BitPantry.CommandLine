using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Integration tests for file upload checksum verification.
    /// These tests verify that file integrity is maintained during uploads.
    /// </summary>
    [TestClass]
    public class IntegrationTests_Checksum
    {
        [TestMethod]
        public async Task Upload_ValidChecksum_FilePreserved()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            var uniqueId = Guid.NewGuid().ToString("N");
            var content = "Valid checksum test content - this should be preserved exactly.";
            File.WriteAllText(tempFilePath, content);

            // Calculate expected checksum
            using var sha256 = SHA256.Create();
            var expectedHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(content)));

            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

                // Act - Upload file (checksum should be computed and verified)
                await fileTransferService.UploadFile(
                    tempFilePath,
                    $"checksum-valid-{uniqueId}.txt",
                    null,
                    CancellationToken.None);

                // Assert - File should exist and content should match exactly
                var expectedPath = Path.Combine("./cli-storage", $"checksum-valid-{uniqueId}.txt");
                File.Exists(expectedPath).Should().BeTrue();
                
                var actualContent = File.ReadAllText(expectedPath);
                actualContent.Should().Be(content, "file content should be preserved exactly");

                // Verify checksum of saved file matches original
                using var sha256Verify = SHA256.Create();
                var actualHash = Convert.ToHexString(sha256Verify.ComputeHash(Encoding.UTF8.GetBytes(actualContent)));
                actualHash.Should().Be(expectedHash, "checksum of saved file should match original");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        [TestMethod]
        public async Task Upload_BinaryFile_PreservesIntegrity()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            var uniqueId = Guid.NewGuid().ToString("N");
            
            // Create binary content with various byte values
            var binaryContent = new byte[1024];
            new Random(42).NextBytes(binaryContent); // Seeded for reproducibility
            File.WriteAllBytes(tempFilePath, binaryContent);

            // Calculate expected checksum
            using var sha256 = SHA256.Create();
            var expectedHash = Convert.ToHexString(sha256.ComputeHash(binaryContent));

            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

                // Act
                await fileTransferService.UploadFile(
                    tempFilePath,
                    $"checksum-binary-{uniqueId}.bin",
                    null,
                    CancellationToken.None);

                // Assert
                var expectedPath = Path.Combine("./cli-storage", $"checksum-binary-{uniqueId}.bin");
                File.Exists(expectedPath).Should().BeTrue();
                
                var actualContent = File.ReadAllBytes(expectedPath);
                actualContent.Should().BeEquivalentTo(binaryContent, "binary content should be preserved exactly");

                // Verify checksum
                using var sha256Verify = SHA256.Create();
                var actualHash = Convert.ToHexString(sha256Verify.ComputeHash(actualContent));
                actualHash.Should().Be(expectedHash);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        [TestMethod]
        public async Task Upload_LargeFile_ChecksumComputedCorrectly()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            var uniqueId = Guid.NewGuid().ToString("N");
            
            // Create a larger file to test incremental hash computation
            var largeContent = new string('X', 500000); // 500KB
            File.WriteAllText(tempFilePath, largeContent);

            // Calculate expected checksum
            using var sha256 = SHA256.Create();
            var expectedHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(largeContent)));

            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

                // Act
                await fileTransferService.UploadFile(
                    tempFilePath,
                    $"checksum-large-{uniqueId}.txt",
                    null,
                    CancellationToken.None);

                // Assert
                var expectedPath = Path.Combine("./cli-storage", $"checksum-large-{uniqueId}.txt");
                File.Exists(expectedPath).Should().BeTrue();
                
                var actualContent = File.ReadAllText(expectedPath);
                
                // Verify checksum
                using var sha256Verify = SHA256.Create();
                var actualHash = Convert.ToHexString(sha256Verify.ComputeHash(Encoding.UTF8.GetBytes(actualContent)));
                actualHash.Should().Be(expectedHash, "checksum should match for large file");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        [TestMethod]
        public async Task Upload_EmptyFile_ChecksumComputedCorrectly()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            var uniqueId = Guid.NewGuid().ToString("N");
            
            // Create empty file
            File.WriteAllText(tempFilePath, "");

            // Expected hash for empty content
            var expectedHash = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";

            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

                // Act
                await fileTransferService.UploadFile(
                    tempFilePath,
                    $"checksum-empty-{uniqueId}.txt",
                    null,
                    CancellationToken.None);

                // Assert
                var expectedPath = Path.Combine("./cli-storage", $"checksum-empty-{uniqueId}.txt");
                File.Exists(expectedPath).Should().BeTrue();
                
                var actualContent = File.ReadAllBytes(expectedPath);
                actualContent.Should().BeEmpty();

                // Verify checksum of empty file
                using var sha256Verify = SHA256.Create();
                var actualHash = Convert.ToHexString(sha256Verify.ComputeHash(actualContent));
                actualHash.Should().Be(expectedHash);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }
    }
}
