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
        public void Validate_DefaultMaxFileSize_Is100MB()
        {
            // Arrange
            var options = new FileTransferOptions();

            // Assert
            options.MaxFileSizeBytes.Should().Be(100 * 1024 * 1024);
        }

        [TestMethod]
        public void Validate_DefaultAllowedExtensions_IsNull()
        {
            // Arrange
            var options = new FileTransferOptions();

            // Assert
            options.AllowedExtensions.Should().BeNull();
        }
    }
}
