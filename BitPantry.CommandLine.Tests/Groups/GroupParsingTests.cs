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
        #region Consolidated: GroupPath Extraction Tests

        [TestMethod]
        [DataRow("command", "", "command", "", "single token - no group path")]
        [DataRow("math add", "math", "add", "", "two tokens - first is group path")]
        [DataRow("math advanced calculate", "math|advanced", "calculate", "", "three tokens - first two are group path")]
        [DataRow("math add --value 5", "math", "add", "--value|5", "tokens with arguments - group path extracted")]
        [DataRow("", "", null, "", "empty input - empty result")]
        [DataRow("math.add", "", "math.add", "", "dot notation - treated as command name")]
        [DataRow("math   add    --value   5", "math", "add", "--value|5", "multiple spaces - normalized correctly")]
        [DataRow("Math ADD", "Math", "ADD", "", "mixed case - preserved for resolution")]
        public void ExtractGroupPath_VariousInputs_ParsesCorrectly(
            string input, 
            string expectedGroupPathPiped, 
            string expectedCommandName,
            string expectedRemainingArgsPiped,
            string scenario)
        {
            // Arrange
            var expectedGroupPath = string.IsNullOrEmpty(expectedGroupPathPiped) 
                ? Array.Empty<string>() 
                : expectedGroupPathPiped.Split('|');
            var expectedRemainingArgs = string.IsNullOrEmpty(expectedRemainingArgsPiped)
                ? Array.Empty<string>()
                : expectedRemainingArgsPiped.Split('|');

            // Act
            var result = ExtractGroupPath(input);

            // Assert
            result.GroupPath.Should().BeEquivalentTo(expectedGroupPath, because: scenario);
            result.CommandName.Should().Be(expectedCommandName, because: scenario);
            result.RemainingArgs.Should().BeEquivalentTo(expectedRemainingArgs, because: scenario);
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
