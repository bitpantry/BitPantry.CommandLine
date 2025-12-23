using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    /// <summary>
    /// Unit tests for SandboxedFile implementation.
    /// Validates path validation and delegation to inner file system.
    /// </summary>
    [TestClass]
    public class SandboxedFileTests
    {
        private MockFileSystem _mockFileSystem;
        private PathValidator _pathValidator;
        private SandboxedFileSystem _sandboxedFileSystem;
        private const string StorageRoot = @"C:\storage";

        [TestInitialize]
        public void Setup()
        {
            _mockFileSystem = new MockFileSystem();
            _mockFileSystem.Directory.CreateDirectory(StorageRoot);
            _pathValidator = new PathValidator(StorageRoot);
            _sandboxedFileSystem = new SandboxedFileSystem(_mockFileSystem, _pathValidator);
        }

        #region Exists Tests

        [TestMethod]
        public void Exists_FileExists_ReturnsTrue()
        {
            // Arrange
            var filePath = Path.Combine(StorageRoot, "test.txt");
            _mockFileSystem.File.WriteAllText(filePath, "content");

            // Act
            var result = _sandboxedFileSystem.File.Exists("test.txt");

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void Exists_FileNotExists_ReturnsFalse()
        {
            // Act
            var result = _sandboxedFileSystem.File.Exists("nonexistent.txt");

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void Exists_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Arrange
            _mockFileSystem.Directory.CreateDirectory(@"C:\secret");
            _mockFileSystem.File.WriteAllText(@"C:\secret\password.txt", "secret");

            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.Exists("../secret/password.txt");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region ReadAllText Tests

        [TestMethod]
        public void ReadAllText_FileExists_ReturnsContent()
        {
            // Arrange
            var content = "Hello, World!";
            var filePath = Path.Combine(StorageRoot, "readme.txt");
            _mockFileSystem.File.WriteAllText(filePath, content);

            // Act
            var result = _sandboxedFileSystem.File.ReadAllText("readme.txt");

            // Assert
            result.Should().Be(content);
        }

        [TestMethod]
        public void ReadAllText_FileNotExists_ThrowsFileNotFoundException()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.ReadAllText("nonexistent.txt");
            act.Should().Throw<FileNotFoundException>();
        }

        [TestMethod]
        public void ReadAllText_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Arrange
            _mockFileSystem.Directory.CreateDirectory(@"C:\secret");
            _mockFileSystem.File.WriteAllText(@"C:\secret\data.txt", "sensitive");

            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.ReadAllText("../secret/data.txt");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region WriteAllBytes Tests

        [TestMethod]
        public void WriteAllBytes_ValidPath_WritesFile()
        {
            // Arrange
            var content = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            _sandboxedFileSystem.File.WriteAllBytes("output.bin", content);

            // Assert
            var actualPath = Path.Combine(StorageRoot, "output.bin");
            _mockFileSystem.File.Exists(actualPath).Should().BeTrue();
            _mockFileSystem.File.ReadAllBytes(actualPath).Should().BeEquivalentTo(content);
        }

        [TestMethod]
        public void WriteAllBytes_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Arrange
            var content = new byte[] { 0x01, 0x02, 0x03 };

            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.WriteAllBytes("../outside.bin", content);
            act.Should().Throw<UnauthorizedAccessException>();
        }

        [TestMethod]
        public void WriteAllBytes_NestedPath_CreatesDirectoriesAndWritesFile()
        {
            // Arrange
            var content = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            _sandboxedFileSystem.File.WriteAllBytes("subdir/nested/output.bin", content);

            // Assert
            var actualPath = Path.Combine(StorageRoot, "subdir", "nested", "output.bin");
            _mockFileSystem.File.Exists(actualPath).Should().BeTrue();
            _mockFileSystem.File.ReadAllBytes(actualPath).Should().BeEquivalentTo(content);
        }

        #endregion

        #region Delete Tests

        [TestMethod]
        public void Delete_FileExists_DeletesFile()
        {
            // Arrange
            var filePath = Path.Combine(StorageRoot, "todelete.txt");
            _mockFileSystem.File.WriteAllText(filePath, "content");

            // Act
            _sandboxedFileSystem.File.Delete("todelete.txt");

            // Assert
            _mockFileSystem.File.Exists(filePath).Should().BeFalse();
        }

        [TestMethod]
        public void Delete_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.Delete("../secret/file.txt");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region GetAttributes Tests

        [TestMethod]
        public void GetAttributes_ValidPath_ReturnsAttributes()
        {
            // Arrange
            var filePath = Path.Combine(StorageRoot, "attrtest.txt");
            _mockFileSystem.File.WriteAllText(filePath, "content");

            // Act
            var attrs = _sandboxedFileSystem.File.GetAttributes("attrtest.txt");

            // Assert
            attrs.Should().NotBe((FileAttributes)0);
        }

        [TestMethod]
        public void GetAttributes_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.GetAttributes("../secret/file.txt");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region Copy Tests

        [TestMethod]
        public void Copy_ValidPaths_CopiesFile()
        {
            // Arrange
            var sourcePath = Path.Combine(StorageRoot, "source.txt");
            _mockFileSystem.File.WriteAllText(sourcePath, "source content");

            // Act
            _sandboxedFileSystem.File.Copy("source.txt", "dest.txt");

            // Assert
            var destPath = Path.Combine(StorageRoot, "dest.txt");
            _mockFileSystem.File.Exists(destPath).Should().BeTrue();
            _mockFileSystem.File.ReadAllText(destPath).Should().Be("source content");
        }

        [TestMethod]
        public void Copy_SourcePathTraversal_ThrowsUnauthorizedAccess()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.Copy("../outside/source.txt", "dest.txt");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        [TestMethod]
        public void Copy_DestPathTraversal_ThrowsUnauthorizedAccess()
        {
            // Arrange
            var sourcePath = Path.Combine(StorageRoot, "source.txt");
            _mockFileSystem.File.WriteAllText(sourcePath, "content");

            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.Copy("source.txt", "../outside/dest.txt");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region Move Tests

        [TestMethod]
        public void Move_ValidPaths_MovesFile()
        {
            // Arrange
            var sourcePath = Path.Combine(StorageRoot, "tomove.txt");
            _mockFileSystem.File.WriteAllText(sourcePath, "move content");

            // Act
            _sandboxedFileSystem.File.Move("tomove.txt", "moved.txt");

            // Assert
            _mockFileSystem.File.Exists(sourcePath).Should().BeFalse();
            var destPath = Path.Combine(StorageRoot, "moved.txt");
            _mockFileSystem.File.Exists(destPath).Should().BeTrue();
            _mockFileSystem.File.ReadAllText(destPath).Should().Be("move content");
        }

        [TestMethod]
        public void Move_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Arrange
            var sourcePath = Path.Combine(StorageRoot, "source.txt");
            _mockFileSystem.File.WriteAllText(sourcePath, "content");

            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.Move("source.txt", "../outside/moved.txt");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region WriteAllText Tests

        [TestMethod]
        public void WriteAllText_ValidPath_WritesContent()
        {
            // Arrange
            var content = "Hello, sandboxed world!";

            // Act
            _sandboxedFileSystem.File.WriteAllText("greeting.txt", content);

            // Assert
            var actualPath = Path.Combine(StorageRoot, "greeting.txt");
            _mockFileSystem.File.Exists(actualPath).Should().BeTrue();
            _mockFileSystem.File.ReadAllText(actualPath).Should().Be(content);
        }

        [TestMethod]
        public void WriteAllText_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.File.WriteAllText("../outside.txt", "malicious");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion
    }
}
