using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class FileSizeValidationTests
    {
        [TestMethod]
        public void ValidateSize_ContentLengthWithinLimit_Succeeds()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                MaxFileSizeBytes = 1024 * 1024 // 1MB
            };
            var validator = new FileSizeValidator(options);
            long contentLength = 512 * 1024; // 512KB

            // Act
            var act = () => validator.ValidateContentLength(contentLength);

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateSize_ContentLengthExceedsLimit_ThrowsException()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                MaxFileSizeBytes = 1024 * 1024 // 1MB
            };
            var validator = new FileSizeValidator(options);
            long contentLength = 2 * 1024 * 1024; // 2MB

            // Act
            var act = () => validator.ValidateContentLength(contentLength);

            // Assert
            act.Should().Throw<FileSizeLimitExceededException>()
                .WithMessage("*exceeds*limit*");
        }

        [TestMethod]
        public void ValidateSize_ContentLengthExactlyAtLimit_Succeeds()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                MaxFileSizeBytes = 1024 * 1024 // 1MB
            };
            var validator = new FileSizeValidator(options);
            long contentLength = 1024 * 1024; // Exactly 1MB

            // Act
            var act = () => validator.ValidateContentLength(contentLength);

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateSize_NoContentLengthProvided_DoesNotThrow()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                MaxFileSizeBytes = 1024 * 1024 // 1MB
            };
            var validator = new FileSizeValidator(options);
            long? contentLength = null;

            // Act
            var act = () => validator.ValidateContentLength(contentLength);

            // Assert - Should not throw when content length is unknown (streaming validation happens later)
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateSize_ZeroContentLength_Succeeds()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                MaxFileSizeBytes = 1024 * 1024 // 1MB
            };
            var validator = new FileSizeValidator(options);
            long contentLength = 0;

            // Act
            var act = () => validator.ValidateContentLength(contentLength);

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateStreamingBytes_WithinLimit_Succeeds()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                MaxFileSizeBytes = 1024 * 1024 // 1MB
            };
            var validator = new FileSizeValidator(options);
            long bytesRead = 512 * 1024; // 512KB

            // Act
            var act = () => validator.ValidateStreamingBytes(bytesRead);

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateStreamingBytes_ExceedsLimit_ThrowsException()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                MaxFileSizeBytes = 1024 * 1024 // 1MB
            };
            var validator = new FileSizeValidator(options);
            long bytesRead = 2 * 1024 * 1024; // 2MB

            // Act
            var act = () => validator.ValidateStreamingBytes(bytesRead);

            // Assert
            act.Should().Throw<FileSizeLimitExceededException>()
                .WithMessage("*exceeds*limit*");
        }

        [TestMethod]
        public void ValidateSize_LargeFileWithinLongLimit_Succeeds()
        {
            // Arrange - Test with >2GB limit to verify long type handling
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                MaxFileSizeBytes = 5L * 1024 * 1024 * 1024 // 5GB
            };
            var validator = new FileSizeValidator(options);
            long contentLength = 4L * 1024 * 1024 * 1024; // 4GB

            // Act
            var act = () => validator.ValidateContentLength(contentLength);

            // Assert
            act.Should().NotThrow();
        }
    }
}
