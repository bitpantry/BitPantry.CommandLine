using BitPantry.CommandLine.Processing.Parsing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class ParsedCommandTests
    {
        [TestMethod]
        public void ParseCommandName_Parsed()
        {
            var input = new ParsedCommand("cmdName");

            input.Elements.Count.Should().Be(1);
            ValidateCommandNode(input, "cmdName");
        }

        [TestMethod]
        public void ParseCommandNameWithSpace_Command()
        {
            var input = new ParsedCommand("   cmdName");

            input.Elements.Count.Should().Be(1);

            ValidateCommandNode(input, "cmdName");
        }

        [TestMethod]
        public void ParseInputWithParameterNoValue_Parsed()
        {
            var input = new ParsedCommand("cmdName --param1");

            input.Elements.Count.Should().Be(3);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "param1");
        }

        [TestMethod]
        public void ParseInputWithArgumentNoValue_Parsed()
        {
            var input = new ParsedCommand("cmdName --param1");

            input.Elements.Count.Should().Be(3);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "param1");
        }

        [TestMethod]
        public void ParseInputWithArgumentValue_Parsed()
        {
            var input = new ParsedCommand("cmdName --param1 val");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "param1", 4);
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentValueNode(input, 4, "val", 2);
        }

        [TestMethod]
        public void ParseInputWithArgumentValueWithSpaces_Parsed()
        {
            var input = new ParsedCommand("cmdName --param1    val");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "param1", 4);
            ValidateEmptyNode(input, 3, "    ");
            ValidateArgumentValueNode(input, 4, "val", 2);
        }

        [TestMethod]
        public void ParseInputWithMultipleArguments_Parsed()
        {
            var input = new ParsedCommand("cmdName --param1 val1 --param2 val2");

            input.Elements.Count.Should().Be(9);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "param1", 4);
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentValueNode(input, 4, "val1", 2);
            ValidateEmptyNode(input, 5, " ");
            ValidateArgumentNode(input, 6, "param2", 8);
            ValidateEmptyNode(input, 7, " ");
            ValidateArgumentValueNode(input, 8, "val2", 6);

        }

        [TestMethod]
        public void ParseArgumentWithValueAndPositionalLike_Parsed()
        {
            // After an argument value, subsequent bare values are now classified as positional values
            // (the resolver will determine if they're valid positional arguments)
            var input = new ParsedCommand("cmdName --param1 val positionalLike");

            input.Elements.Count.Should().Be(7);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "param1", 4);
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentValueNode(input, 4, "val", 2);
            ValidateEmptyNode(input, 5, " ");
            ValidatePositionalValueNode(input, 6, "positionalLike");
        }

        [TestMethod]
        public void ParseInputWithQuotedArgumentValue_Parsed()
        {
            var input = new ParsedCommand("cmdName --param1 \"val\"");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "param1", 4);
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentValueNode(input, 4, "\"val\"", 2);
        }

        [TestMethod]
        public void ParseInputWithComplexQuotedArgumentValue_Parsed()
        {
            var input = new ParsedCommand("cmdName --param1 \"this is my quoted value, with commas\"");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "param1", 4);
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentValueNode(input, 4, "\"this is my quoted value, with commas\"", 2);
        }

        [TestMethod]
        public void ParseInputWithAlias_Parsed()
        {
            var input = new ParsedCommand("cmdName -p");

            input.Elements.Count.Should().Be(3);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentAliasNode(input, 2, "p");
        }

        [TestMethod]
        public void ParseInputWithAliasAndValue_Parsed()
        {
            var input = new ParsedCommand("cmdName -p \"val\"");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentAliasNode(input, 2, "p", 4);
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentValueNode(input, 4, "\"val\"", 2);
        }

        [TestMethod]
        public void ParseInputWithMoreThanOneCharacterAlias_ParsedWithValidationErrors()
        {
            var input = new ParsedCommand("cmdName -param \"val\"");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentAliasNode(input, 2, "param", 4);
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentValueNode(input, 4, "\"val\"", 2);

            input.Errors.Count.Should().Be(1);
            input.Errors.First().Element.Should().Be(input.Elements[2]);
            input.Errors.First().Type.Should().Be(ParsedCommandValidationErrorType.InvalidAlias);
            input.Errors.First().Message.Should().NotBeNull();
        }

        [TestMethod]
        public void ParseInputWithEndOfOptionsMarker_ParsedCorrectly()
        {
            // Bare "--" is now recognized as EndOfOptions, and values after it are PositionalValue
            var input = new ParsedCommand("cmdName -- \"val\"");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateEndOfOptionsNode(input, 2);
            ValidateEmptyNode(input, 3, " ");
            ValidatePositionalValueNode(input, 4, "\"val\"");
        }

        [TestMethod]
        public void ParsedWithGroupCommand_Parsed()
        {
            // With space-separated syntax, subsequent bare values after the first command are PositionalValue.
            // The resolver will interpret these as part of the command path during command lookup.
            var input = new ParsedCommand("myGroup myCommand");

            input.Elements.Count.Should().Be(3);

            ValidateCommandNode(input, "myGroup", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidatePositionalValueNode(input, 2, "myCommand");
        }

        [TestMethod]
        public void ParsedWithGroupCommandAndParameters_Parsed()
        {
            // Subsequent bare values are PositionalValue; "--" is EndOfOptions; values after are also PositionalValue
            var input = new ParsedCommand("myGroup myCommand -- \"val\"");

            input.Elements.Count.Should().Be(7);

            ValidateCommandNode(input, "myGroup", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidatePositionalValueNode(input, 2, "myCommand");
            ValidateEmptyNode(input, 3, " ");
            ValidateEndOfOptionsNode(input, 4);
            ValidateEmptyNode(input, 5, " ");
            ValidatePositionalValueNode(input, 6, "\"val\"");
        }

        #region Positional Value Parsing Tests (PARSE-001 through PARSE-011)

        /// <summary>
        /// PARSE-001: Single positional value after command
        /// </summary>
        [TestMethod]
        public void ParseSinglePositionalValue_ParsedAsPositional()
        {
            var input = new ParsedCommand("copy source.txt");

            input.Elements.Count.Should().Be(3);

            ValidateCommandNode(input, "copy", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidatePositionalValueNode(input, 2, "source.txt");
        }

        /// <summary>
        /// PARSE-002: Multiple positional values after command
        /// </summary>
        [TestMethod]
        public void ParseMultiplePositionalValues_ParsedAsPositional()
        {
            var input = new ParsedCommand("copy source.txt dest.txt");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "copy", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidatePositionalValueNode(input, 2, "source.txt");
            ValidateEmptyNode(input, 3, " ");
            ValidatePositionalValueNode(input, 4, "dest.txt");
        }

        /// <summary>
        /// PARSE-003: Positional value followed by named argument
        /// </summary>
        [TestMethod]
        public void ParsePositionalThenNamed_BothParsedCorrectly()
        {
            var input = new ParsedCommand("copy source.txt --force");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "copy", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidatePositionalValueNode(input, 2, "source.txt");
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentNode(input, 4, "force");
        }

        /// <summary>
        /// PARSE-004: Named argument with value followed by positional-like value (should be unexpected without resolution context)
        /// Note: After named arg value, subsequent values are positional if there are positional args defined
        /// </summary>
        [TestMethod]
        public void ParseNamedThenPositionalLike_ParsedAsPositional()
        {
            var input = new ParsedCommand("cmd --opt value pos1");

            input.Elements.Count.Should().Be(7);

            ValidateCommandNode(input, "cmd", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "opt", 4);
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentValueNode(input, 4, "value", 2);
            ValidateEmptyNode(input, 5, " ");
            ValidatePositionalValueNode(input, 6, "pos1");
        }

        /// <summary>
        /// PARSE-005: End-of-options separator (--) recognized
        /// </summary>
        [TestMethod]
        public void ParseEndOfOptionsSeparator_RecognizedAsEndOfOptions()
        {
            var input = new ParsedCommand("cmd source.txt -- -literal");

            input.Elements.Count.Should().Be(7);

            ValidateCommandNode(input, "cmd", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidatePositionalValueNode(input, 2, "source.txt");
            ValidateEmptyNode(input, 3, " ");
            ValidateEndOfOptionsNode(input, 4);
            ValidateEmptyNode(input, 5, " ");
            ValidatePositionalValueNode(input, 6, "-literal");  // After --, even -prefixed values are positional
        }

        /// <summary>
        /// PARSE-006: Multiple values after -- are all positional
        /// </summary>
        [TestMethod]
        public void ParseMultipleAfterEndOfOptions_AllPositional()
        {
            var input = new ParsedCommand("cmd -- val1 val2 --looks-like-opt");

            input.Elements.Count.Should().Be(9);

            ValidateCommandNode(input, "cmd", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidateEndOfOptionsNode(input, 2);
            ValidateEmptyNode(input, 3, " ");
            ValidatePositionalValueNode(input, 4, "val1");
            ValidateEmptyNode(input, 5, " ");
            ValidatePositionalValueNode(input, 6, "val2");
            ValidateEmptyNode(input, 7, " ");
            ValidatePositionalValueNode(input, 8, "--looks-like-opt");  // After --, even --prefixed values are positional
        }

        /// <summary>
        /// PARSE-007: Empty positional region (just command with options, no positional)
        /// </summary>
        [TestMethod]
        public void ParseNoPositionalValues_NoPositionalElements()
        {
            var input = new ParsedCommand("cmd --opt value");

            // No positional values - should just have command, option, value
            var positionalElements = input.Elements.Where(e => e.ElementType == CommandElementType.PositionalValue);
            positionalElements.Should().BeEmpty();
        }

        /// <summary>
        /// PARSE-008: Quoted positional value preserved
        /// </summary>
        [TestMethod]
        public void ParseQuotedPositionalValue_QuotesPreserved()
        {
            var input = new ParsedCommand("cmd \"source file.txt\"");

            input.Elements.Count.Should().Be(3);

            ValidateCommandNode(input, "cmd", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidatePositionalValueNode(input, 2, "\"source file.txt\"");
        }

        /// <summary>
        /// PARSE-009: Mixed quotes and bare positional values
        /// </summary>
        [TestMethod]
        public void ParseMixedQuotesAndBare_AllParsedCorrectly()
        {
            var input = new ParsedCommand("cmd source.txt \"dest file.txt\"");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "cmd", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidatePositionalValueNode(input, 2, "source.txt");
            ValidateEmptyNode(input, 3, " ");
            ValidatePositionalValueNode(input, 4, "\"dest file.txt\"");
        }

        /// <summary>
        /// PARSE-010: Bare -- with no following values
        /// </summary>
        [TestMethod]
        public void ParseBareEndOfOptions_ParsedAsEndOfOptions()
        {
            var input = new ParsedCommand("cmd --");

            input.Elements.Count.Should().Be(3);

            ValidateCommandNode(input, "cmd", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidateEndOfOptionsNode(input, 2);
        }

        /// <summary>
        /// PARSE-011: Mid-positional -- separator
        /// </summary>
        [TestMethod]
        public void ParseMidPositionalEndOfOptions_SeparatesCorrectly()
        {
            var input = new ParsedCommand("cmd pos1 -- pos2");

            input.Elements.Count.Should().Be(7);

            ValidateCommandNode(input, "cmd", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidatePositionalValueNode(input, 2, "pos1");
            ValidateEmptyNode(input, 3, " ");
            ValidateEndOfOptionsNode(input, 4);
            ValidateEmptyNode(input, 5, " ");
            ValidatePositionalValueNode(input, 6, "pos2");
        }

        #endregion

        private void ValidateUnexpectedNode(
            ParsedCommand input, 
            int index, 
            string element,
            string value = null)
        {
            var node = input.Elements[index];

            ValidateInputNode(
                node,
                element,
                CommandElementType.Unexpected,
                index,
                null,
                value ?? element.Trim());
        }

        private void ValidateArgumentValueNode(
            ParsedCommand input, 
            int index, 
            string element, 
            int pairedWithIndex = -1)
        {
            var node = input.Elements[index];
            var pairedWith = pairedWithIndex > -1 ? input.Elements[pairedWithIndex] : null;


            ValidateInputNode(
                node,
                element,
                CommandElementType.ArgumentValue,
                index,
                pairedWith,
                element.Trim(new char[] { ' ', '"' }));
        }

        private void ValidatePositionalValueNode(
            ParsedCommand input,
            int index,
            string element)
        {
            var node = input.Elements[index];

            ValidateInputNode(
                node,
                element,
                CommandElementType.PositionalValue,
                index,
                null,
                element.Trim(new char[] { ' ', '"' }));
        }

        private void ValidateEndOfOptionsNode(
            ParsedCommand input,
            int index)
        {
            var node = input.Elements[index];

            ValidateInputNode(
                node,
                "--",
                CommandElementType.EndOfOptions,
                index,
                null,
                "");
        }

        private void ValidateEmptyNode(
            ParsedCommand input, 
            int index, 
            string element)
        {
            var node = input.Elements[index];

            ValidateInputNode(
                node,
                element,
                CommandElementType.Empty,
                index,
                null,
                element.Trim());
        }

        private void ValidateArgumentNode(
            ParsedCommand input, 
            int index, 
            string paramName, 
            int pairedWithIndex = -1)
        {
            var node = input.Elements[index];
            var pairedWith = pairedWithIndex > -1 ? input.Elements[pairedWithIndex] : null;

            ValidateInputNode(
                node,
                $"--{paramName}",
                CommandElementType.ArgumentName,
                index,
                pairedWith,
                paramName);
        }

        private void ValidateArgumentAliasNode(
            ParsedCommand input,
            int index,
            string paramName,
            int pairedWithIndex = -1)
        {
            var node = input.Elements[index];
            var pairedWith = pairedWithIndex > -1 ? input.Elements[pairedWithIndex] : null;

            ValidateInputNode(
                node,
                $"-{paramName}",
                CommandElementType.ArgumentAlias,
                index,
                pairedWith,
                paramName);
        }

        private void ValidateCommandNode(ParsedCommand input, string cmdName, int index = 0)
        {
            ValidateInputNode(
                input.Elements[index],
                cmdName,
                CommandElementType.Command,
                index,
                null,
                cmdName);
        }

        private void ValidateInputNode(
            ParsedCommandElement node,
            string element,
            CommandElementType type,
            int index,
            ParsedCommandElement pairedWith,
            string value,
            int startPos = -1,
            int endPos = -1)
        {
            node.Raw.Should().Be(element);
            node.ElementType.Should().Be(type);
            node.Index.Should().Be(index);
            node.IsPairedWith.Should().Be(pairedWith);
            node.Value.Should().Be(value);

            if(startPos > -1)
                node.StartPosition.Should().Be(startPos);

            if(endPos > -1)
                node.EndPosition.Should().Be(endPos);
        }
    }
}
