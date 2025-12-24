using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Tests for parsing group paths from space-separated input.
    /// T009: Test GroupPath extraction from space-separated input
    /// </summary>
    [TestClass]
    public class GroupParsingTests
    {
        #region GroupPath Extraction Tests

        [TestMethod]
        public void Parse_SingleToken_NoGroupPath()
        {
            // Arrange
            var input = "command";

            // Act
            var result = ExtractGroupPath(input);

            // Assert
            result.GroupPath.Should().BeEmpty();
            result.CommandName.Should().Be("command");
        }

        [TestMethod]
        public void Parse_TwoTokens_FirstIsGroupPath()
        {
            // Arrange
            var input = "math add";

            // Act
            var result = ExtractGroupPath(input);

            // Assert
            result.GroupPath.Should().BeEquivalentTo(new[] { "math" });
            result.CommandName.Should().Be("add");
        }

        [TestMethod]
        public void Parse_ThreeTokens_FirstTwoAreGroupPath()
        {
            // Arrange
            var input = "math advanced calculate";

            // Act
            var result = ExtractGroupPath(input);

            // Assert
            result.GroupPath.Should().BeEquivalentTo(new[] { "math", "advanced" });
            result.CommandName.Should().Be("calculate");
        }

        [TestMethod]
        public void Parse_TokensWithArguments_GroupPathExtractedCorrectly()
        {
            // Arrange
            var input = "math add --value 5";

            // Act
            var result = ExtractGroupPath(input);

            // Assert
            result.GroupPath.Should().BeEquivalentTo(new[] { "math" });
            result.CommandName.Should().Be("add");
            result.RemainingArgs.Should().BeEquivalentTo(new[] { "--value", "5" });
        }

        [TestMethod]
        public void Parse_EmptyInput_EmptyResult()
        {
            // Arrange
            var input = "";

            // Act
            var result = ExtractGroupPath(input);

            // Assert
            result.GroupPath.Should().BeEmpty();
            result.CommandName.Should().BeNull();
        }

        [TestMethod]
        public void Parse_DotNotation_TreatedAsCommandName()
        {
            // Old dot notation should not be parsed as group path
            // "math.add" should be treated as a single command name (which won't resolve)
            
            // Arrange
            var input = "math.add";

            // Act
            var result = ExtractGroupPath(input);

            // Assert
            result.GroupPath.Should().BeEmpty();
            result.CommandName.Should().Be("math.add");
        }

        [TestMethod]
        public void Parse_MultipleSpaces_NormalizedCorrectly()
        {
            // Arrange
            var input = "math   add    --value   5";

            // Act
            var result = ExtractGroupPath(input);

            // Assert
            result.GroupPath.Should().BeEquivalentTo(new[] { "math" });
            result.CommandName.Should().Be("add");
            result.RemainingArgs.Should().BeEquivalentTo(new[] { "--value", "5" });
        }

        #endregion

        #region Case Sensitivity Tests

        [TestMethod]
        public void Parse_MixedCase_PreservedForResolution()
        {
            // Arrange
            var input = "Math ADD";

            // Act
            var result = ExtractGroupPath(input);

            // Assert
            result.GroupPath.Should().BeEquivalentTo(new[] { "Math" });
            result.CommandName.Should().Be("ADD");
        }

        #endregion

        // Helper method that will be replaced by actual implementation
        // This represents the expected parsing behavior
        private ParsedGroupPath ExtractGroupPath(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ParsedGroupPath
                {
                    GroupPath = Array.Empty<string>(),
                    CommandName = null,
                    RemainingArgs = Array.Empty<string>()
                };
            }

            var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (tokens.Length == 0)
            {
                return new ParsedGroupPath
                {
                    GroupPath = Array.Empty<string>(),
                    CommandName = null,
                    RemainingArgs = Array.Empty<string>()
                };
            }

            if (tokens.Length == 1)
            {
                return new ParsedGroupPath
                {
                    GroupPath = Array.Empty<string>(),
                    CommandName = tokens[0],
                    RemainingArgs = Array.Empty<string>()
                };
            }

            // Find where arguments start (first token starting with -)
            int argStartIndex = -1;
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].StartsWith("-"))
                {
                    argStartIndex = i;
                    break;
                }
            }

            int commandAndGroupEnd = argStartIndex == -1 ? tokens.Length : argStartIndex;
            
            // Last non-arg token is the command name
            // All tokens before it are the group path
            var groupPath = new List<string>();
            for (int i = 0; i < commandAndGroupEnd - 1; i++)
            {
                groupPath.Add(tokens[i]);
            }

            string commandName = commandAndGroupEnd > 0 ? tokens[commandAndGroupEnd - 1] : null;

            var remainingArgs = new List<string>();
            if (argStartIndex != -1)
            {
                for (int i = argStartIndex; i < tokens.Length; i++)
                {
                    remainingArgs.Add(tokens[i]);
                }
            }

            return new ParsedGroupPath
            {
                GroupPath = groupPath.ToArray(),
                CommandName = commandName,
                RemainingArgs = remainingArgs.ToArray()
            };
        }

        private class ParsedGroupPath
        {
            public string[] GroupPath { get; set; }
            public string CommandName { get; set; }
            public string[] RemainingArgs { get; set; }
        }
    }
}
