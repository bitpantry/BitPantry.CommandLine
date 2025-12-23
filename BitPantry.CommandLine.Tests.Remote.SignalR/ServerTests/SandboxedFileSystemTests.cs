using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    /// <summary>
    /// Tests for server-side SandboxedFileSystem which wraps local file system
    /// operations with path validation to confine access to StorageRootPath.
    /// </summary>
    [TestClass]
    public class SandboxedFileSystemTests
    {
        private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), $"sandboxed-fs-tests-{Guid.NewGuid()}");

        [TestInitialize]
        public void TestInitialize()
        {
            // Create a fresh storage root for each test
            Directory.CreateDirectory(_storageRoot);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up the storage root after each test
            if (Directory.Exists(_storageRoot))
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
        }

        [TestMethod]
        public void SandboxedFileSystem_File_Exists_ValidPath_DelegatesToLocalFileSystem()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);
            
            // Create a test file in the storage root
            var testFilePath = Path.Combine(_storageRoot, "test-file.txt");
            File.WriteAllText(testFilePath, "test content");

            // Act - Use relative path from the sandboxed perspective
            var exists = sandboxedFileSystem.File.Exists("test-file.txt");

            // Assert
            exists.Should().BeTrue(
                "file exists in the storage root and should be accessible via sandboxed file system");
        }

        [TestMethod]
        public void SandboxedFileSystem_File_Exists_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);

            // Act - Attempt path traversal
            Action act = () => sandboxedFileSystem.File.Exists("../../../etc/passwd");

            // Assert
            act.Should().Throw<UnauthorizedAccessException>(
                "path traversal attempts should be rejected by the sandboxed file system");
        }

        [TestMethod]
        public void SandboxedFileSystem_Directory_Exists_ValidPath_DelegatesToLocalFileSystem()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);
            
            // Create a test directory in the storage root
            var testDirPath = Path.Combine(_storageRoot, "test-dir");
            Directory.CreateDirectory(testDirPath);

            // Act
            var exists = sandboxedFileSystem.Directory.Exists("test-dir");

            // Assert
            exists.Should().BeTrue(
                "directory exists in the storage root and should be accessible via sandboxed file system");
        }

        [TestMethod]
        public void SandboxedFileSystem_Directory_CreateDirectory_ValidPath_CreatesLocally()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);

            // Act
            sandboxedFileSystem.Directory.CreateDirectory("new-directory");

            // Assert
            var expectedPath = Path.Combine(_storageRoot, "new-directory");
            Directory.Exists(expectedPath).Should().BeTrue(
                "directory should be created in the storage root");
        }

        [TestMethod]
        public void SandboxedFileSystem_File_WriteAllText_ValidPath_WritesToStorageRoot()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);
            var testContent = "This is test content from the sandboxed file system";

            // Act
            sandboxedFileSystem.File.WriteAllText("output.txt", testContent);

            // Assert
            var expectedPath = Path.Combine(_storageRoot, "output.txt");
            File.Exists(expectedPath).Should().BeTrue(
                "file should be created in the storage root");
            File.ReadAllText(expectedPath).Should().Be(testContent,
                "file content should match what was written");
        }

        [TestMethod]
        public void SandboxedFileSystem_File_ReadAllText_ValidPath_ReadsFromStorageRoot()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);
            
            // Create a test file in the storage root
            var testFilePath = Path.Combine(_storageRoot, "read-test.txt");
            var expectedContent = "Content to be read by sandboxed file system";
            File.WriteAllText(testFilePath, expectedContent);

            // Act
            var content = sandboxedFileSystem.File.ReadAllText("read-test.txt");

            // Assert
            content.Should().Be(expectedContent,
                "sandboxed file system should read content from files in storage root");
        }

        [TestMethod]
        public void SandboxedFileSystem_Directory_CreateDirectory_NestedPath_CreatesAllLevels()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);

            // Act
            sandboxedFileSystem.Directory.CreateDirectory("level1/level2/level3");

            // Assert
            var expectedPath = Path.Combine(_storageRoot, "level1", "level2", "level3");
            Directory.Exists(expectedPath).Should().BeTrue(
                "nested directories should be created under the storage root");
        }

        [TestMethod]
        public void SandboxedFileSystem_File_WriteAllText_PathTraversal_ThrowsUnauthorizedAccess()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);

            // Act - Attempt to write outside storage root via path traversal
            Action act = () => sandboxedFileSystem.File.WriteAllText("../malicious.txt", "malicious content");

            // Assert
            act.Should().Throw<UnauthorizedAccessException>(
                "attempting to write outside storage root should throw UnauthorizedAccessException");
        }

        [TestMethod]
        public void SandboxedFileSystem_Directory_Delete_ValidPath_DeletesFromStorageRoot()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);
            
            // Create a test directory with content
            var testDirPath = Path.Combine(_storageRoot, "to-delete");
            Directory.CreateDirectory(testDirPath);
            File.WriteAllText(Path.Combine(testDirPath, "file.txt"), "content");

            // Act
            sandboxedFileSystem.Directory.Delete("to-delete", recursive: true);

            // Assert
            Directory.Exists(testDirPath).Should().BeFalse(
                "directory should be deleted from storage root");
        }

        [TestMethod]
        public void SandboxedFileSystem_File_Delete_ValidPath_DeletesFromStorageRoot()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);
            
            // Create a test file
            var testFilePath = Path.Combine(_storageRoot, "to-delete.txt");
            File.WriteAllText(testFilePath, "content to delete");

            // Act
            sandboxedFileSystem.File.Delete("to-delete.txt");

            // Assert
            File.Exists(testFilePath).Should().BeFalse(
                "file should be deleted from storage root");
        }
    }
}
