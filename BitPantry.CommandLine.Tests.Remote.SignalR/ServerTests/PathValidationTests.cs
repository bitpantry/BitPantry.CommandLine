using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Abstractions.TestingHelpers;

using BitPantry.CommandLine.Tests.Infrastructure;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class PathValidationTests
    {
        private static string StorageRoot => TestPaths.ServerStorageRoot;

        [TestMethod]
        public void ValidatePath_RelativePathWithinRoot_ReturnsFullPath()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            var relativePath = "subfolder/file.txt";

            // Act
            var result = validator.ValidatePath(relativePath);

            // Assert
            result.Should().Be(Path.Combine(StorageRoot, "subfolder", "file.txt"));
        }

        [TestMethod]
        public void ValidatePath_PathTraversalWithDotDot_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            var traversalPath = "../file.txt";

            // Act
            var act = () => validator.ValidatePath(traversalPath);

            // Assert
            act.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*outside*");
        }

        [TestMethod]
        public void ValidatePath_PathTraversalWithMultipleDotDot_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            var traversalPath = "subfolder/../../file.txt";

            // Act
            var act = () => validator.ValidatePath(traversalPath);

            // Assert
            act.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*outside*");
        }

        [TestMethod]
        public void ValidatePath_AbsolutePathOutsideRoot_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            // On Windows, an absolute path like C:\Windows\... is detected as rooted and rejected.
            // On Linux, leading / is stripped (treated as sandbox-relative), so use a traversal
            // path that actually escapes the sandbox root.
            var maliciousPath = OperatingSystem.IsWindows()
                ? TestPaths.OutsidePath          // C:\Windows\System32\file.txt
                : "../../etc/passwd";            // resolves outside the sandbox root

            // Act
            var act = () => validator.ValidatePath(maliciousPath);

            // Assert
            act.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*outside*");
        }

        [TestMethod]
        public void ValidatePath_EncodedDotDotSlash_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            // URL-encoded ../ is %2e%2e%2f
            var encodedPath = "%2e%2e%2ffile.txt";

            // Act
            var act = () => validator.ValidatePath(encodedPath);

            // Assert - After URL decoding, should still detect traversal
            act.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*outside*");
        }

        [TestMethod]
        public void ValidatePath_PathWithSpaces_ReturnsValidPath()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            var pathWithSpaces = "sub folder/my file.txt";

            // Act
            var result = validator.ValidatePath(pathWithSpaces);

            // Assert
            result.Should().Be(Path.Combine(StorageRoot, "sub folder", "my file.txt"));
        }

        [TestMethod]
        public void ValidatePath_PathWithUnicode_ReturnsValidPath()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            var unicodePath = "文件夹/文件.txt";

            // Act
            var result = validator.ValidatePath(unicodePath);

            // Assert
            result.Should().Be(Path.Combine(StorageRoot, "文件夹", "文件.txt"));
        }

        [TestMethod]
        public void ValidatePath_NullPath_ThrowsArgumentNullException()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);

            // Act
            var act = () => validator.ValidatePath(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void ValidatePath_EmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);

            // Act
            var act = () => validator.ValidatePath(string.Empty);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void ValidatePath_PathAtRootBoundary_ReturnsValidPath()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            // Path that navigates down then up but stays within root
            var boundaryPath = "subfolder/../file.txt";

            // Act
            var result = validator.ValidatePath(boundaryPath);

            // Assert - Should normalize to just file.txt at root
            result.Should().Be(Path.Combine(StorageRoot, "file.txt"));
        }

        [TestMethod]
        public void ValidatePath_WindowsStyleBackslashTraversal_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            var backslashTraversal = "../file.txt";

            // Act
            var act = () => validator.ValidatePath(backslashTraversal);

            // Assert
            act.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*outside*");
        }

        [TestMethod]
        public void ValidatePath_MixedSlashTraversal_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            var mixedPath = "subfolder/../../../file.txt";

            // Act
            var act = () => validator.ValidatePath(mixedPath);

            // Assert
            act.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*outside*");
        }

        [TestMethod]
        public void ValidatePath_ForwardSlashRoot_ReturnsSandboxRoot()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            
            // Act - "/" should be interpreted as the sandbox root, not filesystem root
            var result = validator.ValidatePath("/");

            // Assert - Should return the storage root itself
            result.Should().Be(StorageRoot);
        }

        [TestMethod]
        public void ValidatePath_BackslashRoot_ReturnsSandboxRoot()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            
            // Act - "\" should be interpreted as the sandbox root, not filesystem root
            var result = validator.ValidatePath(@"\");

            // Assert - Should return the storage root itself
            result.Should().Be(StorageRoot);
        }

        [TestMethod]
        public void ValidatePath_ForwardSlashWithFilename_ReturnsFileAtSandboxRoot()
        {
            // Arrange
            var validator = new PathValidator(StorageRoot);
            
            // Act - "/file.txt" should be interpreted as file at sandbox root
            var result = validator.ValidatePath("/file.txt");

            // Assert
            result.Should().Be(Path.Combine(StorageRoot, "file.txt"));
        }
    }
}
