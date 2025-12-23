using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class ExtensionValidationTests
    {
        [TestMethod]
        public void ValidateExtension_AllowedExtension_Succeeds()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                AllowedExtensions = new[] { ".txt", ".pdf", ".doc" }
            };
            var validator = new ExtensionValidator(options);

            // Act
            var act = () => validator.ValidateExtension("document.txt");

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateExtension_DisallowedExtension_ThrowsException()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                AllowedExtensions = new[] { ".txt", ".pdf", ".doc" }
            };
            var validator = new ExtensionValidator(options);

            // Act
            var act = () => validator.ValidateExtension("script.exe");

            // Assert
            act.Should().Throw<FileExtensionNotAllowedException>()
                .WithMessage("*.exe*not allowed*");
        }

        [TestMethod]
        public void ValidateExtension_AllowedExtensionsNull_AllowsAll()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                AllowedExtensions = null
            };
            var validator = new ExtensionValidator(options);

            // Act - Any extension should be allowed
            var act1 = () => validator.ValidateExtension("script.exe");
            var act2 = () => validator.ValidateExtension("document.txt");
            var act3 = () => validator.ValidateExtension("archive.zip");

            // Assert
            act1.Should().NotThrow();
            act2.Should().NotThrow();
            act3.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateExtension_CaseInsensitiveMatch_Succeeds()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                AllowedExtensions = new[] { ".txt", ".PDF" }
            };
            var validator = new ExtensionValidator(options);

            // Act - Case insensitive matching
            var act1 = () => validator.ValidateExtension("DOCUMENT.TXT");
            var act2 = () => validator.ValidateExtension("report.pdf");
            var act3 = () => validator.ValidateExtension("notes.Txt");

            // Assert
            act1.Should().NotThrow();
            act2.Should().NotThrow();
            act3.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateExtension_NoExtension_ThrowsIfNotInAllowList()
        {
            // Arrange - Files without extensions are NOT allowed when allow list is set
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                AllowedExtensions = new[] { ".txt", ".pdf" }
            };
            var validator = new ExtensionValidator(options);

            // Act
            var act = () => validator.ValidateExtension("README");

            // Assert
            act.Should().Throw<FileExtensionNotAllowedException>()
                .WithMessage("*not allowed*");
        }

        [TestMethod]
        public void ValidateExtension_NoExtension_AllowedWhenNullAllowList()
        {
            // Arrange - Files without extensions ARE allowed when allow list is null
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                AllowedExtensions = null
            };
            var validator = new ExtensionValidator(options);

            // Act
            var act = () => validator.ValidateExtension("README");

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateExtension_EmptyAllowList_ThrowsForAllExtensions()
        {
            // Arrange - Empty array means no extensions are allowed
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                AllowedExtensions = Array.Empty<string>()
            };
            var validator = new ExtensionValidator(options);

            // Act
            var act = () => validator.ValidateExtension("document.txt");

            // Assert
            act.Should().Throw<FileExtensionNotAllowedException>();
        }

        [TestMethod]
        public void ValidateExtension_MultipleDotsInFilename_ChecksLastExtension()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                AllowedExtensions = new[] { ".txt", ".gz" }
            };
            var validator = new ExtensionValidator(options);

            // Act - Should check the last extension
            var act1 = () => validator.ValidateExtension("archive.tar.gz");
            var act2 = () => validator.ValidateExtension("file.backup.txt");

            // Assert
            act1.Should().NotThrow();
            act2.Should().NotThrow();
        }

        [TestMethod]
        public void ValidateExtension_PathWithDirectories_ExtractsFilename()
        {
            // Arrange
            var options = new FileTransferOptions
            {
                StorageRootPath = "/test",
                AllowedExtensions = new[] { ".txt" }
            };
            var validator = new ExtensionValidator(options);

            // Act
            var act = () => validator.ValidateExtension("subfolder/nested/document.txt");

            // Assert
            act.Should().NotThrow();
        }
    }
}
