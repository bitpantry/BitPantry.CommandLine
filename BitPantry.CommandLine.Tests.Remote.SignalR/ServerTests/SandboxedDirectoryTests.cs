using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    /// <summary>
    /// Unit tests for SandboxedDirectory implementation.
    /// Validates path validation and delegation to inner file system.
    /// </summary>
    [TestClass]
    public class SandboxedDirectoryTests
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
        public void Exists_DirectoryExists_ReturnsTrue()
        {
            // Arrange
            var dirPath = Path.Combine(StorageRoot, "subdir");
            _mockFileSystem.Directory.CreateDirectory(dirPath);

            // Act
            var result = _sandboxedFileSystem.Directory.Exists("subdir");

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void Exists_DirectoryNotExists_ReturnsFalse()
        {
            // Act
            var result = _sandboxedFileSystem.Directory.Exists("nonexistent");

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void Exists_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Arrange
            _mockFileSystem.Directory.CreateDirectory(@"C:\secret");

            // Act & Assert
            Action act = () => _sandboxedFileSystem.Directory.Exists("../secret");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region CreateDirectory Tests

        [TestMethod]
        public void CreateDirectory_ValidPath_CreatesDirectory()
        {
            // Act
            _sandboxedFileSystem.Directory.CreateDirectory("newdir");

            // Assert
            var expectedPath = Path.Combine(StorageRoot, "newdir");
            _mockFileSystem.Directory.Exists(expectedPath).Should().BeTrue();
        }

        [TestMethod]
        public void CreateDirectory_AlreadyExists_Succeeds()
        {
            // Arrange
            var dirPath = Path.Combine(StorageRoot, "existing");
            _mockFileSystem.Directory.CreateDirectory(dirPath);

            // Act - should not throw
            var result = _sandboxedFileSystem.Directory.CreateDirectory("existing");

            // Assert
            result.Should().NotBeNull();
            _mockFileSystem.Directory.Exists(dirPath).Should().BeTrue();
        }

        [TestMethod]
        public void CreateDirectory_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.Directory.CreateDirectory("../outside");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        [TestMethod]
        public void CreateDirectory_Nested_CreatesAllLevels()
        {
            // Act
            _sandboxedFileSystem.Directory.CreateDirectory("level1/level2/level3");

            // Assert
            var expectedPath = Path.Combine(StorageRoot, "level1", "level2", "level3");
            _mockFileSystem.Directory.Exists(expectedPath).Should().BeTrue();
        }

        #endregion

        #region EnumerateFiles Tests

        [TestMethod]
        public void EnumerateFiles_ReturnsFiles()
        {
            // Arrange
            var dirPath = Path.Combine(StorageRoot, "withfiles");
            _mockFileSystem.Directory.CreateDirectory(dirPath);
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "file1.txt"), "content1");
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "file2.txt"), "content2");

            // Act
            var files = _sandboxedFileSystem.Directory.EnumerateFiles("withfiles").ToList();

            // Assert
            files.Should().HaveCount(2);
            files.Should().Contain(f => f.EndsWith("file1.txt"));
            files.Should().Contain(f => f.EndsWith("file2.txt"));
        }

        [TestMethod]
        public void EnumerateFiles_WithPattern_ReturnsMatchingFiles()
        {
            // Arrange
            var dirPath = Path.Combine(StorageRoot, "mixed");
            _mockFileSystem.Directory.CreateDirectory(dirPath);
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "doc.txt"), "text");
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "image.png"), "binary");
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "notes.txt"), "more text");

            // Act
            var txtFiles = _sandboxedFileSystem.Directory.EnumerateFiles("mixed", "*.txt").ToList();

            // Assert
            txtFiles.Should().HaveCount(2);
            txtFiles.Should().AllSatisfy(f => f.Should().EndWith(".txt"));
        }

        [TestMethod]
        public void EnumerateFiles_Recursive_ReturnsAllFiles()
        {
            // Arrange
            var dirPath = Path.Combine(StorageRoot, "recursive");
            _mockFileSystem.Directory.CreateDirectory(dirPath);
            _mockFileSystem.Directory.CreateDirectory(Path.Combine(dirPath, "sub"));
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "root.txt"), "root");
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "sub", "nested.txt"), "nested");

            // Act
            var allFiles = _sandboxedFileSystem.Directory.EnumerateFiles("recursive", "*", SearchOption.AllDirectories).ToList();

            // Assert
            allFiles.Should().HaveCount(2);
        }

        [TestMethod]
        public void EnumerateFiles_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.Directory.EnumerateFiles("../secret").ToList();
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region EnumerateDirectories Tests

        [TestMethod]
        public void EnumerateDirectories_ReturnsSubdirectories()
        {
            // Arrange
            var dirPath = Path.Combine(StorageRoot, "parent");
            _mockFileSystem.Directory.CreateDirectory(dirPath);
            _mockFileSystem.Directory.CreateDirectory(Path.Combine(dirPath, "child1"));
            _mockFileSystem.Directory.CreateDirectory(Path.Combine(dirPath, "child2"));

            // Act
            var dirs = _sandboxedFileSystem.Directory.EnumerateDirectories("parent").ToList();

            // Assert
            dirs.Should().HaveCount(2);
        }

        [TestMethod]
        public void EnumerateDirectories_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.Directory.EnumerateDirectories("../secret").ToList();
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region Delete Tests

        [TestMethod]
        public void Delete_EmptyDirectory_Succeeds()
        {
            // Arrange
            var dirPath = Path.Combine(StorageRoot, "todelete");
            _mockFileSystem.Directory.CreateDirectory(dirPath);

            // Act
            _sandboxedFileSystem.Directory.Delete("todelete");

            // Assert
            _mockFileSystem.Directory.Exists(dirPath).Should().BeFalse();
        }

        [TestMethod]
        public void Delete_NonEmptyNonRecursive_ThrowsIOException()
        {
            // Arrange
            var dirPath = Path.Combine(StorageRoot, "nonempty");
            _mockFileSystem.Directory.CreateDirectory(dirPath);
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "file.txt"), "content");

            // Act & Assert
            Action act = () => _sandboxedFileSystem.Directory.Delete("nonempty", false);
            act.Should().Throw<IOException>();
        }

        [TestMethod]
        public void Delete_NonEmptyRecursive_DeletesAll()
        {
            // Arrange
            var dirPath = Path.Combine(StorageRoot, "recursive");
            _mockFileSystem.Directory.CreateDirectory(dirPath);
            _mockFileSystem.Directory.CreateDirectory(Path.Combine(dirPath, "sub"));
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "file.txt"), "content");
            _mockFileSystem.File.WriteAllText(Path.Combine(dirPath, "sub", "nested.txt"), "nested");

            // Act
            _sandboxedFileSystem.Directory.Delete("recursive", true);

            // Assert
            _mockFileSystem.Directory.Exists(dirPath).Should().BeFalse();
        }

        [TestMethod]
        public void Delete_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.Directory.Delete("../secret");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region Move Tests

        [TestMethod]
        public void Move_ValidPaths_MovesDirectory()
        {
            // Arrange
            var sourcePath = Path.Combine(StorageRoot, "source");
            _mockFileSystem.Directory.CreateDirectory(sourcePath);
            _mockFileSystem.File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "content");

            // Act
            _sandboxedFileSystem.Directory.Move("source", "destination");

            // Assert
            _mockFileSystem.Directory.Exists(sourcePath).Should().BeFalse();
            var destPath = Path.Combine(StorageRoot, "destination");
            _mockFileSystem.Directory.Exists(destPath).Should().BeTrue();
            _mockFileSystem.File.Exists(Path.Combine(destPath, "file.txt")).Should().BeTrue();
        }

        [TestMethod]
        public void Move_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Arrange
            var sourcePath = Path.Combine(StorageRoot, "source");
            _mockFileSystem.Directory.CreateDirectory(sourcePath);

            // Act & Assert
            Action act = () => _sandboxedFileSystem.Directory.Move("source", "../outside");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        #endregion

        #region GetCurrentDirectory / SetCurrentDirectory Tests

        [TestMethod]
        public void SetCurrentDirectory_ThrowsNotSupported()
        {
            // Act & Assert
            Action act = () => _sandboxedFileSystem.Directory.SetCurrentDirectory("subdir");
            act.Should().Throw<NotSupportedException>();
        }

        #endregion
    }
}
