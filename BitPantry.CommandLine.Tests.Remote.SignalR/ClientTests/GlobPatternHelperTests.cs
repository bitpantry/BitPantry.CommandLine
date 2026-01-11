using FluentAssertions;
using System.IO.Abstractions.TestingHelpers;
using BitPantry.CommandLine.Remote.SignalR.Client;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for GlobPatternHelper utility class.
    /// Tests cover cross-platform path handling and glob pattern operations.
    /// </summary>
    [TestClass]
    public class GlobPatternHelperTests
    {
        private MockFileSystem _fileSystem;

        [TestInitialize]
        public void Setup()
        {
            _fileSystem = new MockFileSystem();
            _fileSystem.Directory.SetCurrentDirectory(@"C:\testdir");
        }

        #region ContainsGlobCharacters Tests

        [TestMethod]
        public void ContainsGlobCharacters_WithAsterisk_ReturnsTrue()
        {
            // Arrange
            var path = "*.txt";

            // Act
            var result = GlobPatternHelper.ContainsGlobCharacters(path);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ContainsGlobCharacters_WithQuestionMark_ReturnsTrue()
        {
            // Arrange
            var path = "file?.txt";

            // Act
            var result = GlobPatternHelper.ContainsGlobCharacters(path);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ContainsGlobCharacters_WithDoubleAsterisk_ReturnsTrue()
        {
            // Arrange
            var path = "**/*.log";

            // Act
            var result = GlobPatternHelper.ContainsGlobCharacters(path);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ContainsGlobCharacters_LiteralPath_ReturnsFalse()
        {
            // Arrange
            var path = "file.txt";

            // Act
            var result = GlobPatternHelper.ContainsGlobCharacters(path);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ContainsGlobCharacters_PathWithDirectory_ReturnsFalse()
        {
            // Arrange
            var path = @"C:\files\data\config.json";

            // Act
            var result = GlobPatternHelper.ContainsGlobCharacters(path);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ParseGlobPattern Tests

        [TestMethod]
        public void ParseGlobPattern_SimplePattern_ReturnsCwdAsBaseDir()
        {
            // Arrange
            var pattern = "*.txt";

            // Act
            var (baseDir, patternPart) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);

            // Assert
            baseDir.Should().Be(@"C:\testdir");
            patternPart.Should().Be("*.txt");
        }

        [TestMethod]
        public void ParseGlobPattern_WithDirectory_SplitsCorrectly()
        {
            // Arrange
            var pattern = @"logs\*.log";

            // Act
            var (baseDir, patternPart) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);

            // Assert
            baseDir.Should().Be("logs");
            patternPart.Should().Be("*.log");
        }

        [TestMethod]
        public void ParseGlobPattern_RecursivePattern_SplitsCorrectly()
        {
            // Arrange
            var pattern = @"data\**\*.csv";

            // Act
            var (baseDir, patternPart) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);

            // Assert
            baseDir.Should().Be("data");
            patternPart.Should().Be("**/*.csv");
        }

        #endregion

        #region GlobPatternToRegex Tests

        [TestMethod]
        public void GlobPatternToRegex_AsteriskPattern_MatchesMultipleFiles()
        {
            // Arrange
            var pattern = "*.txt";
            var regex = GlobPatternHelper.GlobPatternToRegex(pattern);

            // Assert
            regex.IsMatch("file.txt").Should().BeTrue();
            regex.IsMatch("data.txt").Should().BeTrue();
            regex.IsMatch("file.log").Should().BeFalse();
        }

        [TestMethod]
        public void GlobPatternToRegex_QuestionMarkPattern_MatchesSingleChar()
        {
            // Arrange
            var pattern = "file?.txt";
            var regex = GlobPatternHelper.GlobPatternToRegex(pattern);

            // Assert
            regex.IsMatch("file1.txt").Should().BeTrue();
            regex.IsMatch("fileA.txt").Should().BeTrue();
            regex.IsMatch("files.txt").Should().BeTrue(); // 's' is one char, matches ?
            regex.IsMatch("file12.txt").Should().BeFalse(); // '12' is two chars
            regex.IsMatch("file.txt").Should().BeFalse(); // missing one char
        }

        [TestMethod]
        public void GlobPatternToRegex_CaseInsensitive_MatchesDifferentCase()
        {
            // Implements: T157 - Case-insensitive matching for cross-platform safety
            // Arrange
            var pattern = "*.TXT";
            var regex = GlobPatternHelper.GlobPatternToRegex(pattern);

            // Assert
            regex.IsMatch("data.txt").Should().BeTrue();
            regex.IsMatch("README.TXT").Should().BeTrue();
            regex.IsMatch("Config.Txt").Should().BeTrue();
        }

        #endregion

        #region Cross-Platform Path Normalization Tests (T156-T159)

        /// <summary>
        /// Implements: T156
        /// ParseGlobPattern with Windows backslashes normalizes to forward slashes internally.
        /// </summary>
        [TestMethod]
        public void ParseGlobPattern_WindowsBackslashes_NormalizesCorrectly()
        {
            // Arrange
            var pattern = @"data\**\*.txt";

            // Act
            var (baseDir, patternPart) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);

            // Assert
            baseDir.Should().Be("data");
            patternPart.Should().Be("**/*.txt"); // Normalized to forward slashes
        }

        /// <summary>
        /// Implements: T158
        /// ParseGlobPattern with mixed separators handles correctly.
        /// </summary>
        [TestMethod]
        public void ParseGlobPattern_MixedSeparators_HandlesCorrectly()
        {
            // Arrange - mixed backslash and forward slash
            var pattern = @"files/data\**/*.txt";

            // Act
            var (baseDir, patternPart) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);

            // Assert
            baseDir.Should().Be("files" + _fileSystem.Path.DirectorySeparatorChar + "data");
            patternPart.Should().Be("**/*.txt");
        }

        /// <summary>
        /// Implements: T159
        /// ParseGlobPattern with forward slashes on Windows works correctly.
        /// </summary>
        [TestMethod]
        public void ParseGlobPattern_ForwardSlashesOnWindows_ParsesCorrectly()
        {
            // Arrange - forward slashes (works on both Windows and Unix)
            var pattern = "files/data/*.txt";

            // Act
            var (baseDir, patternPart) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);

            // Assert
            baseDir.Should().Be("files" + _fileSystem.Path.DirectorySeparatorChar + "data");
            patternPart.Should().Be("*.txt");
        }

        /// <summary>
        /// Implements: T156 (additional)
        /// ParseGlobPattern with absolute Windows path with backslashes.
        /// </summary>
        [TestMethod]
        public void ParseGlobPattern_AbsoluteWindowsPath_ParsesCorrectly()
        {
            // Arrange
            var pattern = @"C:\files\data\*.txt";

            // Act
            var (baseDir, patternPart) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);

            // Assert
            // MockFileSystem uses Windows separators, so baseDir should have backslashes
            baseDir.Should().Be(@"C:\files\data");
            patternPart.Should().Be("*.txt");
        }

        /// <summary>
        /// Additional test for recursive pattern with backslashes.
        /// </summary>
        [TestMethod]
        public void ParseGlobPattern_RecursiveWithBackslashes_NormalizesPattern()
        {
            // Arrange
            var pattern = @"logs\2024\**\*.log";

            // Act
            var (baseDir, patternPart) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);

            // Assert
            baseDir.Should().Be("logs" + _fileSystem.Path.DirectorySeparatorChar + "2024");
            patternPart.Should().Be("**/*.log");
        }

        #endregion
    }
}
