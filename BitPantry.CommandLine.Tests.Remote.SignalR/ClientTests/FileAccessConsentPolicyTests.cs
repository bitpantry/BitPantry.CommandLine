using BitPantry.CommandLine.Remote.SignalR.Client;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for FileAccessConsentPolicy.
    /// Tests cover glob pattern matching for the client-side file access consent engine.
    /// </summary>
    [TestClass]
    public class FileAccessConsentPolicyTests
    {
        private FileAccessConsentPolicy _policy;

        [TestInitialize]
        public void Setup()
        {
            _policy = new FileAccessConsentPolicy();
        }

        #region IsAllowed Tests

        [TestMethod]
        public void IsAllowed_NoPatterns_ReturnsFalse()
        {
            // Arrange - no patterns configured (default state)

            // Act
            var result = _policy.IsAllowed(@"c:\data\file.txt");

            // Assert
            result.Should().BeFalse("with no patterns configured, no path should be allowed");
        }

        [TestMethod]
        public void IsAllowed_ExactMatch_ReturnsTrue()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"c:\data\file.txt" });

            // Act
            var result = _policy.IsAllowed(@"c:\data\file.txt");

            // Assert
            result.Should().BeTrue("exact path match should be allowed");
        }

        [TestMethod]
        public void IsAllowed_StarGlob_MatchesFiles()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"c:\data\*" });

            // Act & Assert
            _policy.IsAllowed(@"c:\data\file.txt").Should().BeTrue("* should match files in the directory");
            _policy.IsAllowed(@"c:\data\report.csv").Should().BeTrue("* should match any file in the directory");
            _policy.IsAllowed(@"c:\data\sub\file.txt").Should().BeFalse("* should NOT match files in subdirectories");
        }

        [TestMethod]
        public void IsAllowed_DoubleStarGlob_MatchesRecursive()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"c:\data\**" });

            // Act & Assert
            _policy.IsAllowed(@"c:\data\file.txt").Should().BeTrue("** should match files directly in the directory");
            _policy.IsAllowed(@"c:\data\sub\deep\file.txt").Should().BeTrue("** should match files in nested subdirectories");
        }

        [TestMethod]
        public void IsAllowed_QuestionMark_MatchesSingleChar()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { "file?.txt" });

            // Act & Assert
            _policy.IsAllowed("file1.txt").Should().BeTrue("? should match a single character");
            _policy.IsAllowed("fileA.txt").Should().BeTrue("? should match any single character");
            _policy.IsAllowed("file12.txt").Should().BeFalse("? should NOT match two characters");
            _policy.IsAllowed("file.txt").Should().BeFalse("? should NOT match zero characters");
        }

        [TestMethod]
        public void IsAllowed_NonMatchingPath_ReturnsFalse()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"c:\data\**" });

            // Act
            var result = _policy.IsAllowed(@"c:\secrets\pw.txt");

            // Assert
            result.Should().BeFalse("path outside allowed pattern should not be allowed");
        }

        [TestMethod]
        public void IsAllowed_CaseInsensitive_MatchesDifferentCase()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"C:\Data\**" });

            // Act
            var result = _policy.IsAllowed(@"c:\data\file.txt");

            // Assert
            result.Should().BeTrue("pattern matching should be case-insensitive");
        }

        [TestMethod]
        public void IsAllowed_MultiplePatterns_AnyMatch_ReturnsTrue()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"c:\docs\*", @"c:\data\**" });

            // Act & Assert
            _policy.IsAllowed(@"c:\data\file.txt").Should().BeTrue("path matches second pattern");
            _policy.IsAllowed(@"c:\docs\readme.md").Should().BeTrue("path matches first pattern");
            _policy.IsAllowed(@"c:\secrets\pw.txt").Should().BeFalse("path matches neither pattern");
        }

        #endregion

        #region RequiresConsent Tests

        [TestMethod]
        public void RequiresConsent_AllowedPath_ReturnsFalse()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"c:\data\**" });

            // Act
            var result = _policy.RequiresConsent(@"c:\data\file.txt");

            // Assert
            result.Should().BeFalse("allowed path should not require consent");
        }

        [TestMethod]
        public void RequiresConsent_UnallowedPath_ReturnsTrue()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"c:\data\**" });

            // Act
            var result = _policy.RequiresConsent(@"c:\secrets\pw.txt");

            // Assert
            result.Should().BeTrue("path not matching any allowed pattern should require consent");
        }

        #endregion

        #region SetAllowedPatterns Tests

        [TestMethod]
        public void SetAllowedPatterns_ReplacesExisting()
        {
            // Arrange - set initial patterns
            _policy.SetAllowedPatterns(new[] { @"c:\data\**" });
            _policy.IsAllowed(@"c:\data\file.txt").Should().BeTrue("initial pattern should allow the path");

            // Act - replace with new patterns
            _policy.SetAllowedPatterns(new[] { @"c:\docs\**" });

            // Assert
            _policy.IsAllowed(@"c:\data\file.txt").Should().BeFalse("old pattern should no longer apply");
            _policy.IsAllowed(@"c:\docs\readme.md").Should().BeTrue("new pattern should apply");
        }

        #endregion

        #region GetPathsRequiringConsent Tests

        [TestMethod]
        public void GetPathsRequiringConsent_MixedPaths_ReturnsOnlyUnapproved()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"c:\data\**" });

            var paths = new[]
            {
                @"c:\data\file.txt",       // allowed
                @"c:\secrets\pw.txt",      // not allowed
                @"c:\data\sub\report.csv", // allowed
                @"c:\other\config.json"    // not allowed
            };

            // Act
            var result = _policy.GetPathsRequiringConsent(paths);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(@"c:\secrets\pw.txt");
            result.Should().Contain(@"c:\other\config.json");
            result.Should().NotContain(@"c:\data\file.txt");
            result.Should().NotContain(@"c:\data\sub\report.csv");
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void IsAllowed_ForwardSlashPaths_MatchesCorrectly()
        {
            // Arrange - Linux-style paths
            _policy.SetAllowedPatterns(new[] { "/home/user/data/**" });

            // Act & Assert
            _policy.IsAllowed("/home/user/data/file.txt").Should().BeTrue();
            _policy.IsAllowed("/home/user/data/sub/file.txt").Should().BeTrue();
            _policy.IsAllowed("/home/other/file.txt").Should().BeFalse();
        }

        [TestMethod]
        public void IsAllowed_MixedSeparators_NormalizesCorrectly()
        {
            // Arrange - pattern with backslashes
            _policy.SetAllowedPatterns(new[] { @"c:\data\**" });

            // Act - path with forward slashes
            var result = _policy.IsAllowed("c:/data/file.txt");

            // Assert
            result.Should().BeTrue("path separator normalization should handle mixed separators");
        }

        [TestMethod]
        public void IsAllowed_DoubleStarInMiddle_MatchesRecursiveSubdirectories()
        {
            // Arrange
            _policy.SetAllowedPatterns(new[] { @"c:\data\**\*.txt" });

            // Act & Assert
            _policy.IsAllowed(@"c:\data\file.txt").Should().BeTrue("** in middle should match zero subdirectories");
            _policy.IsAllowed(@"c:\data\sub\file.txt").Should().BeTrue("** in middle should match one subdirectory");
            _policy.IsAllowed(@"c:\data\sub\deep\file.txt").Should().BeTrue("** in middle should match nested subdirectories");
            _policy.IsAllowed(@"c:\data\file.csv").Should().BeFalse("non-txt file should not match");
        }

        [TestMethod]
        public void SetAllowedPatterns_EmptyCollection_ClearsPatterns()
        {
            // Arrange - set some patterns first
            _policy.SetAllowedPatterns(new[] { @"c:\data\**" });
            _policy.IsAllowed(@"c:\data\file.txt").Should().BeTrue();

            // Act - clear with empty collection
            _policy.SetAllowedPatterns(Array.Empty<string>());

            // Assert
            _policy.IsAllowed(@"c:\data\file.txt").Should().BeFalse("empty patterns should disallow all paths");
        }

        #endregion
    }
}
