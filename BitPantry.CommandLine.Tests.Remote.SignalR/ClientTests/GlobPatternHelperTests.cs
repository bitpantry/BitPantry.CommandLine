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

        #region ValidatePattern Tests

        [TestMethod]
        public void ValidatePattern_EmptyString_ReturnsError()
        {
            // Act
            var result = GlobPatternHelper.ValidatePattern("");

            // Assert
            result.IsValid.Should().BeFalse("empty pattern should be invalid");
            result.ErrorMessage.Should().NotBeNullOrEmpty("should provide error message");
            result.SuggestedFormat.Should().NotBeNullOrEmpty("should suggest valid format");
        }

        [TestMethod]
        public void ValidatePattern_WhitespaceOnly_ReturnsError()
        {
            // Act
            var result = GlobPatternHelper.ValidatePattern("   ");

            // Assert
            result.IsValid.Should().BeFalse("whitespace-only pattern should be invalid");
            result.ErrorMessage.Should().Contain("empty", "should indicate pattern is empty/whitespace");
        }

        [TestMethod]
        public void ValidatePattern_ValidPatterns_ReturnsSuccess()
        {
            // Arrange
            var validPatterns = new[]
            {
                "file.txt",
                "*.txt",
                "folder/*.log",
                "**/*.json",
                "data?.csv",
                "logs/2024/*.log"
            };

            foreach (var pattern in validPatterns)
            {
                // Act
                var result = GlobPatternHelper.ValidatePattern(pattern);

                // Assert
                result.IsValid.Should().BeTrue($"pattern '{pattern}' should be valid");
            }
        }

        #endregion

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

        /// <summary>
        /// Implements: T157, IT-CP-002
        /// T157: Case-insensitive matching for cross-platform safety
        /// IT-CP-002: Glob pattern matching is case-insensitive in both commands
        /// </summary>
        [TestMethod]
        public void GlobPatternToRegex_CaseInsensitive_MatchesDifferentCase()
        {
            // Implements: T157, IT-CP-002 - Case-insensitive matching for cross-platform safety
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

        #region ResolveDestinationPath Tests

        [TestMethod]
        public void ResolveDestinationPath_DestinationEndsWithSlash_AppendsFilename()
        {
            // Arrange
            var destination = "C:/downloads/";
            var fileName = "myfile.txt";

            // Act
            var result = GlobPatternHelper.ResolveDestinationPath(destination, fileName);

            // Assert
            result.Should().Be("C:/downloads/myfile.txt");
        }

        [TestMethod]
        public void ResolveDestinationPath_DestinationEndsWithBackslash_AppendsFilename()
        {
            // Arrange
            var destination = @"C:\downloads\";
            var fileName = "data.json";

            // Act
            var result = GlobPatternHelper.ResolveDestinationPath(destination, fileName);

            // Assert
            result.Should().Be(@"C:\downloads/data.json");
        }

        [TestMethod]
        public void ResolveDestinationPath_DestinationIsFilename_ReturnsAsIs()
        {
            // Arrange
            var destination = @"C:\downloads\renamed.txt";
            var fileName = "original.txt";

            // Act
            var result = GlobPatternHelper.ResolveDestinationPath(destination, fileName);

            // Assert
            result.Should().Be(@"C:\downloads\renamed.txt");
        }

        [TestMethod]
        public void ResolveDestinationPath_RelativeDirectory_AppendsFilename()
        {
            // Arrange
            var destination = "./output/";
            var fileName = "report.csv";

            // Act
            var result = GlobPatternHelper.ResolveDestinationPath(destination, fileName);

            // Assert
            result.Should().Be("./output/report.csv");
        }

        #endregion

        #region ReconstructFullPath Tests

        [TestMethod]
        public void ReconstructFullPath_WithBaseDir_PrependsDirectory()
        {
            // Arrange
            var baseDir = "logs";
            var relativePath = "app.log";

            // Act
            var result = GlobPatternHelper.ReconstructFullPath(baseDir, relativePath);

            // Assert
            result.Should().Be("logs/app.log");
        }

        [TestMethod]
        public void ReconstructFullPath_EmptyBaseDir_ReturnsRelativePath()
        {
            // Arrange
            var baseDir = "";
            var relativePath = "file.txt";

            // Act
            var result = GlobPatternHelper.ReconstructFullPath(baseDir, relativePath);

            // Assert
            result.Should().Be("file.txt");
        }

        [TestMethod]
        public void ReconstructFullPath_DotBaseDir_ReturnsRelativePath()
        {
            // Arrange
            var baseDir = ".";
            var relativePath = "config.json";

            // Act
            var result = GlobPatternHelper.ReconstructFullPath(baseDir, relativePath);

            // Assert
            result.Should().Be("config.json");
        }

        [TestMethod]
        public void ReconstructFullPath_NormalizesBackslashes()
        {
            // Arrange
            var baseDir = @"data\files";
            var relativePath = "output.csv";

            // Act
            var result = GlobPatternHelper.ReconstructFullPath(baseDir, relativePath);

            // Assert
            result.Should().Be("data/files/output.csv");
        }

        [TestMethod]
        public void ReconstructFullPath_TrimsTrailingSlash()
        {
            // Arrange
            var baseDir = "logs/";
            var relativePath = "error.log";

            // Act
            var result = GlobPatternHelper.ReconstructFullPath(baseDir, relativePath);

            // Assert
            result.Should().Be("logs/error.log");
        }

        [TestMethod]
        public void ReconstructFullPath_TrimsLeadingSlashFromRelative()
        {
            // Arrange
            var baseDir = "data";
            var relativePath = "/file.txt";

            // Act
            var result = GlobPatternHelper.ReconstructFullPath(baseDir, relativePath);

            // Assert
            result.Should().Be("data/file.txt");
        }

        #endregion

        #region ApplyQuestionMarkFilter Tests

        [TestMethod]
        public void ApplyQuestionMarkFilter_NoQuestionMark_ReturnsAllFiles()
        {
            // Arrange
            var files = new[] { "file1.txt", "file2.txt", "data.log" };
            var pattern = "*.txt";

            // Act
            var result = GlobPatternHelper.ApplyQuestionMarkFilter(files, pattern, f => f).ToList();

            // Assert
            result.Should().HaveCount(3, "no ? in pattern, all files should pass through");
        }

        [TestMethod]
        public void ApplyQuestionMarkFilter_WithQuestionMark_FiltersSingleChar()
        {
            // Arrange
            var files = new[] { "file1.txt", "file2.txt", "file12.txt", "file.txt" };
            var pattern = "file?.txt";

            // Act
            var result = GlobPatternHelper.ApplyQuestionMarkFilter(files, pattern, f => f).ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain("file1.txt");
            result.Should().Contain("file2.txt");
            result.Should().NotContain("file12.txt", "12 is two chars, not one");
            result.Should().NotContain("file.txt", "missing the required single char");
        }

        [TestMethod]
        public void ApplyQuestionMarkFilter_MultipleQuestionMarks_FiltersCorrectly()
        {
            // Arrange
            var files = new[] { "log01.txt", "log12.txt", "log1.txt", "log123.txt" };
            var pattern = "log??.txt";

            // Act
            var result = GlobPatternHelper.ApplyQuestionMarkFilter(files, pattern, f => f).ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain("log01.txt");
            result.Should().Contain("log12.txt");
        }

        [TestMethod]
        public void ApplyQuestionMarkFilter_CaseInsensitive()
        {
            // Arrange
            var files = new[] { "FILE1.TXT", "file2.txt", "File3.Txt" };
            var pattern = "file?.txt";

            // Act
            var result = GlobPatternHelper.ApplyQuestionMarkFilter(files, pattern, f => f).ToList();

            // Assert
            result.Should().HaveCount(3, "matching should be case-insensitive");
        }

        [TestMethod]
        public void ApplyQuestionMarkFilter_WithCustomGetFileName()
        {
            // Arrange - simulating FileInfoEntry-like objects
            var files = new[]
            {
                new { Path = "/logs/file1.log", Size = 100 },
                new { Path = "/logs/file12.log", Size = 200 },
                new { Path = "/logs/file2.log", Size = 300 }
            };
            var pattern = "file?.log";

            // Act
            var result = GlobPatternHelper.ApplyQuestionMarkFilter(
                files, 
                pattern, 
                f => System.IO.Path.GetFileName(f.Path)).ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Select(f => f.Path).Should().Contain("/logs/file1.log");
            result.Select(f => f.Path).Should().Contain("/logs/file2.log");
        }

        #endregion
    }
}
