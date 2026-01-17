using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class FileTransferOptionsTests
    {
        [TestMethod]
        public void Validate_StorageRootPathNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = null,
                MaxFileSizeBytes = 100 * 1024 * 1024
            };

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*StorageRootPath*");
        }

        [TestMethod]
        public void Validate_StorageRootPathEmpty_ThrowsInvalidOperationException()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "",
                MaxFileSizeBytes = 100 * 1024 * 1024
            };

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*StorageRootPath*");
        }

        [TestMethod]
        public void Validate_MaxFileSizeBytesZero_ThrowsArgumentException()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/valid/path",
                MaxFileSizeBytes = 0
            };

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*MaxFileSizeBytes*");
        }

        [TestMethod]
        public void Validate_MaxFileSizeBytesNegative_ThrowsArgumentException()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/valid/path",
                MaxFileSizeBytes = -1
            };

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*MaxFileSizeBytes*");
        }

        [TestMethod]
        public void Validate_ValidConfiguration_Succeeds()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/valid/path",
                MaxFileSizeBytes = 100 * 1024 * 1024
            };

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Validate_DefaultMaxFileSize_PassesValidation()
        {
            // Arrange - use default MaxFileSizeBytes, just set required StorageRootPath
            var options = new FileTransferOptions
            {
                StorageRootPath = "/valid/path"
                // MaxFileSizeBytes uses default (100MB)
            };

            // Act - validation should pass with default size
            var act = () => options.Validate();

            // Assert - default size is valid (positive)
            act.Should().NotThrow("default MaxFileSizeBytes should be valid");
            
            // Also verify the default is a reasonable size (> 1MB, < 1GB)
            // This tests the behavioral contract: "defaults should be reasonable for typical file transfers"
            options.MaxFileSizeBytes.Should().BeGreaterThan(1024 * 1024, "default should be at least 1MB");
            options.MaxFileSizeBytes.Should().BeLessThanOrEqualTo(1024L * 1024 * 1024, "default should not exceed 1GB");
        }

    }
}
