using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using System;
using System.IO;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class FileTransferOptionsTests
    {
        #region Default Values Tests

        [TestMethod]
        public void Constructor_DefaultStorageRootPath_IsSet()
        {
            // Arrange & Act
            var options = new FileTransferOptions();

            // Assert
            options.StorageRootPath.Should().NotBeNullOrWhiteSpace("default StorageRootPath should be set");
            options.StorageRootPath.Should().EndWith(FileTransferOptions.DefaultStorageDirectoryName);
            options.StorageRootPath.Should().StartWith(AppContext.BaseDirectory);
        }

        [TestMethod]
        public void Constructor_DefaultIsEnabled_IsTrue()
        {
            // Arrange & Act
            var options = new FileTransferOptions();

            // Assert
            options.IsEnabled.Should().BeTrue("file transfer should be enabled by default");
        }

        [TestMethod]
        public void Validate_DefaultOptions_Succeeds()
        {
            // Arrange - use all defaults without any configuration
            var options = new FileTransferOptions();

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().NotThrow("default options should be valid");
        }

        #endregion

        #region Disable Functionality Tests

        [TestMethod]
        public void Disable_SetsIsEnabledToFalse()
        {
            // Arrange
            var options = new FileTransferOptions();

            // Act
            options.Disable();

            // Assert
            options.IsEnabled.Should().BeFalse("Disable() should set IsEnabled to false");
        }

        [TestMethod]
        public void Disable_ReturnsThisForMethodChaining()
        {
            // Arrange
            var options = new FileTransferOptions();

            // Act
            var result = options.Disable();

            // Assert
            result.Should().BeSameAs(options, "Disable() should return the same instance for method chaining");
        }

        [TestMethod]
        public void Validate_WhenDisabled_SkipsValidation()
        {
            // Arrange - disable file transfer, set invalid values
            var options = new FileTransferOptions
            {
                StorageRootPath = null,  // Would normally throw
                MaxFileSizeBytes = -1     // Would normally throw
            };
            options.Disable();

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().NotThrow("validation should be skipped when disabled");
        }

        [TestMethod]
        public void Validate_WhenDisabled_StorageRootPathNullDoesNotThrow()
        {
            // Arrange
            var options = new FileTransferOptions { StorageRootPath = null };
            options.Disable();

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().NotThrow("StorageRootPath validation should be skipped when disabled");
        }

        [TestMethod]
        public void Validate_WhenDisabled_MaxFileSizeZeroDoesNotThrow()
        {
            // Arrange
            var options = new FileTransferOptions { MaxFileSizeBytes = 0 };
            options.Disable();

            // Act
            var act = () => options.Validate();

            // Assert
            act.Should().NotThrow("MaxFileSizeBytes validation should be skipped when disabled");
        }

        #endregion

        #region Validation Tests (When Enabled)

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

        #endregion
    }
}
