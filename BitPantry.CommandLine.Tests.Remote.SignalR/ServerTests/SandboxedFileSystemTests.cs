using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Component;
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

        #region GetCurrentDirectory — Bug Reproduction

        /// <summary>
        /// BUG REPRODUCTION: SandboxedDirectory.GetCurrentDirectory() returns the real process CWD
        /// instead of the sandbox storage root. This causes FilePathAutoCompleteHandler to fail
        /// when invoked with an empty query on the server side, because the real CWD is typically
        /// outside the sandbox root — ValidatePath then throws UnauthorizedAccessException.
        ///
        /// Symptom: Remote ghost text for [FilePathAutoComplete] doesn't appear when the cursor
        /// enters a positional argument position. Typing "\" makes it work because ValidatePath(@"\")
        /// resolves to the storage root.
        /// </summary>
        [TestMethod]
        public void SandboxedFileSystem_GetCurrentDirectory_ReturnsStorageRoot()
        {
            // Arrange
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);

            // Act
            var currentDir = sandboxedFileSystem.Directory.GetCurrentDirectory();

            // Assert — should return the sandbox root, NOT the real process CWD
            currentDir.Should().Be(_storageRoot,
                "in a sandboxed context, GetCurrentDirectory should return the storage root, " +
                "not the real process CWD which is outside the sandbox");
        }

        /// <summary>
        /// BUG REPRODUCTION (end-to-end): FilePathAutoCompleteHandler with SandboxedFileSystem
        /// returns empty options for an empty query, because GetCurrentDirectory() returns the
        /// real process CWD, which is outside the sandbox.
        ///
        /// This test creates files in the sandbox root and verifies that the handler returns
        /// them when called with an empty query — exactly what happens when the cursor enters
        /// a positional argument position.
        /// </summary>
        [TestMethod]
        public async Task FilePathAutoCompleteHandler_WithSandboxedFileSystem_EmptyQuery_ReturnsOptions()
        {
            // Arrange — create sandbox with files
            var innerFileSystem = new FileSystem();
            var pathValidator = new PathValidator(_storageRoot);
            var sandboxedFileSystem = new SandboxedFileSystem(innerFileSystem, pathValidator);

            // Create some files and directories in the sandbox root
            Directory.CreateDirectory(Path.Combine(_storageRoot, "docs"));
            Directory.CreateDirectory(Path.Combine(_storageRoot, "src"));
            File.WriteAllText(Path.Combine(_storageRoot, "readme.txt"), "hello");

            var theme = new Theme();
            var handler = new FilePathAutoCompleteHandler(sandboxedFileSystem, theme);

            var context = new AutoCompleteContext
            {
                QueryString = "", // empty query — what happens when cursor enters positional position
                FullInput = "browse ",
                CursorPosition = 7,
                ArgumentInfo = null!,
                CommandInfo = null!,
                ProvidedValues = new Dictionary<ArgumentInfo, string>()
            };

            // Act
            var options = await handler.GetOptionsAsync(context);

            // Assert — should return the sandbox contents
            options.Should().NotBeEmpty(
                "FilePathAutoCompleteHandler with empty query should list the sandbox root contents, " +
                "not fail because GetCurrentDirectory() returns the real CWD outside the sandbox");
            options.Select(o => o.Value).Should().Contain(v => v.StartsWith("docs"),
                "should include the 'docs' directory from the sandbox root");
        }

        #endregion
    }
}
