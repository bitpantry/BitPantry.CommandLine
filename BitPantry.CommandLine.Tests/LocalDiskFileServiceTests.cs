using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;

namespace BitPantry.CommandLine.Tests
{

    [TestClass]
    public class LocalDiskFileServiceTests
    {
        private readonly LocalDiskFileService _fileService;
        private readonly LocalDiskFileService _fileServiceWithRoot;

        public LocalDiskFileServiceTests()
        {
            _fileService = new LocalDiskFileService();
            _fileServiceWithRoot = new LocalDiskFileService("rootDir");
        }

        [TestMethod]
        public void WriteFile_ShouldCreateFileWithContent()
        {
            // Arrange
            var filePath = "testfile.txt";
            var content = "Hello, World!";

            // Act
            _fileService.WriteAllText(filePath, content);

            // Assert
            File.Exists(filePath).Should().BeTrue();
            File.ReadAllText(filePath).Should().Be(content);

            // Cleanup
            File.Delete(filePath);
        }

        [TestMethod]
        public void ReadFile_ShouldReturnFileContent()
        {
            // Arrange
            var filePath = "testfile.txt";
            var content = "Hello, World!";
            File.WriteAllText(filePath, content);

            // Act
            var result = _fileService.ReadAllText(filePath);

            // Assert
            result.Should().Be(content);

            // Cleanup
            File.Delete(filePath);
        }

        [TestMethod]
        public void ReadFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var filePath = "nonexistentfile.txt";

            // Act
            Action act = () => _fileService.ReadAllText(filePath);

            // Assert
            act.Should().Throw<FileNotFoundException>();
        }

        [TestMethod]
        public void DeleteFile_ShouldRemoveFile()
        {
            // Arrange
            var filePath = "testfile.txt";
            File.WriteAllText(filePath, "Hello, World!");

            // Act
            _fileService.Delete(filePath);

            // Assert
            File.Exists(filePath).Should().BeFalse();
        }

        [TestMethod]
        public void WriteFile_WithRootDirectory_ShouldCreateFileWithContent()
        {
            // Arrange
            var filePath = "subdir/testfile.txt";
            var content = "Hello, World!";

            // Act
            _fileServiceWithRoot.WriteAllText(filePath, content);

            // Assert
            File.Exists(Path.Combine("rootDir", filePath)).Should().BeTrue();
            File.ReadAllText(Path.Combine("rootDir", filePath)).Should().Be(content);

            // Cleanup
            File.Delete(Path.Combine("rootDir", filePath));
        }

        [TestMethod]
        public void WriteFile_WithRootDirectory_ShouldThrowInvalidOperationException_WhenPathIsRooted()
        {
            // Arrange
            var filePath = Path.Combine("C:", "testfile.txt");

            // Act
            Action act = () => _fileServiceWithRoot.WriteAllText(filePath, "Hello, World!");

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("path must be relative");
        }

        [TestMethod]
        public void EnsureDirectoryExists_ShouldCreateDirectoryIfNotExists()
        {
            // Arrange
            var filePath = "newdir/testfile.txt";
            var content = "Hello, World!";

            // Act
            _fileServiceWithRoot.WriteAllText(filePath, content);

            // Assert
            Directory.Exists(Path.Combine("rootDir", "newdir")).Should().BeTrue();
            File.Exists(Path.Combine("rootDir", filePath)).Should().BeTrue();
            File.ReadAllText(Path.Combine("rootDir", filePath)).Should().Be(content);

            // Cleanup
            File.Delete(Path.Combine("rootDir", filePath));
            Directory.Delete(Path.Combine("rootDir", "newdir"));
        }
    }
}
