using BitPantry.CommandLine.Processing.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests
{


    [TestClass]
    public class StringParsingTests
    {
        [TestMethod]
        public void SplitCommandString_SplitsBySpace()
        {
            // Arrange
            var commandString = "command --arg1 --arg2";

            // Act
            var result = StringParsing.SplitCommandString(commandString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { "command", " ", "--arg1", " ", "--arg2" });
        }

        [TestMethod]
        public void SplitInputString_SplitsByPipe()
        {
            // Arrange
            var inputString = "command1 | command2 | command3";

            // Act
            var result = StringParsing.SplitInputString(inputString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { "command1 ", " command2 ", " command3" });
        }

        [TestMethod]
        public void SplitCommandString_PreservesQuotedStrings()
        {
            // Arrange
            var commandString = "command --arg1 \"with spaces\" --arg2";

            // Act
            var result = StringParsing.SplitCommandString(commandString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { "command", " ", "--arg1", " ", "\"with spaces\"", " ", "--arg2" });
        }

        [TestMethod]
        public void SplitInputString_PreservesQuotedStrings()
        {
            // Arrange
            var inputString = "command1 | command2 --with \"spaces\" | command3";

            // Act
            var result = StringParsing.SplitInputString(inputString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { "command1 ", " command2 --with \"spaces\" ", " command3" });
        }

        [TestMethod]
        public void SplitCommandString_HandleMultiCharacterEmptySpaces()
        {
            // Arrange
            var commandString = "command  --arg1  --arg2";

            // Act
            var result = StringParsing.SplitCommandString(commandString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { "command", "  ", "--arg1", "  ", "--arg2" });
        }

        [TestMethod]
        public void SplitInputString_IgnoresEmptyStrings()
        {
            // Arrange
            var inputString = "command1 |  | command3";

            // Act
            var result = StringParsing.SplitInputString(inputString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { "command1 ", "  ", " command3" });
        }
        [TestMethod]
        public void SplitCommandString_HandlesEmptyString()
        {
            // Arrange
            var commandString = "";

            // Act
            var result = StringParsing.SplitCommandString(commandString);

            // Assert
            result.Should().BeEquivalentTo(new List<string>());
        }

        [TestMethod]
        public void SplitInputString_HandlesEmptyString()
        {
            // Arrange
            var inputString = "";

            // Act
            var result = StringParsing.SplitInputString(inputString);

            // Assert
            result.Should().BeEquivalentTo(new List<string>());
        }

        [TestMethod]
        public void SplitCommandString_HandlesOnlySpaces()
        {
            // Arrange
            var commandString = "     ";

            // Act
            var result = StringParsing.SplitCommandString(commandString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { "     " });
        }

        [TestMethod]
        public void SplitInputString_HandlesOnlyPipes()
        {
            // Arrange
            var inputString = "|||";

            // Act
            var result = StringParsing.SplitInputString(inputString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { });
        }

        [TestMethod]
        public void SplitCommandString_HandlesSpecialCharacters()
        {
            // Arrange
            var commandString = "command --arg1 !@#$%^&*() --arg2";

            // Act
            var result = StringParsing.SplitCommandString(commandString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { "command", " ", "--arg1", " ", "!@#$%^&*()", " ", "--arg2" });
        }

        [TestMethod]
        public void SplitInputString_HandlesSpecialCharacters()
        {
            // Arrange
            var inputString = "command1 | command2 !@#$%^&*() | command3";

            // Act
            var result = StringParsing.SplitInputString(inputString);

            // Assert
            result.Should().BeEquivalentTo(new List<string> { "command1 ", " command2 !@#$%^&*() ", " command3" });
        }

    }
}
