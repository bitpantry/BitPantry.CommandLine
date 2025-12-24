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
        public void ParseArgumentWithValueAndUnexpected_Parsed()
        {
            var input = new ParsedCommand("cmdName --param1 val unexpected");

            input.Elements.Count.Should().Be(7);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateArgumentNode(input, 2, "param1", 4);
            ValidateEmptyNode(input, 3, " ");
            ValidateArgumentValueNode(input, 4, "val", 2);
            ValidateEmptyNode(input, 5, " ");
            ValidateUnexpectedNode(input, 6, "unexpected");
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
        public void ParseInputWithEmptyParameterName_UnexpectedElement()
        {
            var input = new ParsedCommand("cmdName -- \"val\"");

            input.Elements.Count.Should().Be(5);

            ValidateCommandNode(input, "cmdName");
            ValidateEmptyNode(input, 1, " ");
            ValidateUnexpectedNode(input, 2, "--", "");
            ValidateEmptyNode(input, 3, " ");
            ValidateUnexpectedNode(input, 4, "\"val\"", "val");
        }

        [TestMethod]
        public void ParsedWithGroupCommand_Parsed()
        {
            // With space-separated syntax, "group command" is parsed as two command elements
            var input = new ParsedCommand("myGroup myCommand");

            input.Elements.Count.Should().Be(3);

            ValidateCommandNode(input, "myGroup", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidateCommandNode(input, "myCommand", 2);
        }

        [TestMethod]
        public void ParsedWithGroupCommandAndParameters_Parsed()
        {
            var input = new ParsedCommand("myGroup myCommand -- \"val\"");

            input.Elements.Count.Should().Be(7);

            ValidateCommandNode(input, "myGroup", 0);
            ValidateEmptyNode(input, 1, " ");
            ValidateCommandNode(input, "myCommand", 2);
            ValidateEmptyNode(input, 3, " ");
            ValidateUnexpectedNode(input, 4, "--", "");
            ValidateEmptyNode(input, 5, " ");
            ValidateUnexpectedNode(input, 6, "\"val\"", "val");
        }

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
