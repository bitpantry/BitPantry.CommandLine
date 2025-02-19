using BitPantry.CommandLine.Processing.Parsing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class ParsedInputTests
    {
        [TestMethod]
        public void ParseInputOnePipe_Parsed()
        {
            var input = new ParsedInput("cmd1 | cmd2");

            input.ParsedCommands.Should().HaveCount(2);

            input.ParsedCommands[0].ToString().Should().Be("cmd1 ");
            input.ParsedCommands[0].StringLength.Should().Be(5);

            input.ParsedCommands[1].ToString().Should().Be(" cmd2");
            input.ParsedCommands[1].StringLength.Should().Be(5);

            input.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ParseInputTwoPipe_Parsed()
        {
            var input = new ParsedInput(" cmd1 | cmd2 | cmd3 ");

            input.ParsedCommands.Should().HaveCount(3);
            input.ParsedCommands[0].ToString().Should().Be(" cmd1 ");
            input.ParsedCommands[0].StringLength.Should().Be(6);

            input.ParsedCommands[1].ToString().Should().Be(" cmd2 ");
            input.ParsedCommands[1].StringLength.Should().Be(6);

            input.ParsedCommands[2].ToString().Should().Be(" cmd3 ");
            input.ParsedCommands[2].StringLength.Should().Be(6);

            input.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ParseInputQuoteParameter_Parsed()
        {
            var input = new ParsedInput(" cmd1 -p \"val\" | cmd2");

            input.ParsedCommands.Should().HaveCount(2);
            input.ParsedCommands[0].ToString().Should().Be(" cmd1 -p \"val\" ");
            input.ParsedCommands[1].ToString().Should().Be(" cmd2");
            input.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ParseInputQuoteParameterWithPipe_Parsed()
        {
            var input = new ParsedInput(" cmd1 -p \"val|val2\" | cmd2");

            input.ParsedCommands.Should().HaveCount(2);
            input.ParsedCommands[0].ToString().Should().Be(" cmd1 -p \"val|val2\" ");
            input.ParsedCommands[1].ToString().Should().Be(" cmd2");
            input.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ParseInputInvalidCommand_Parsed()
        {
            var input = new ParsedInput(" cmd1 x -p \"val|val2\" | cmd2");

            input.ParsedCommands.Should().HaveCount(2);
            input.ParsedCommands[0].ToString().Should().Be(" cmd1 x -p \"val|val2\" ");
            input.ParsedCommands[1].ToString().Should().Be(" cmd2");
            input.IsValid.Should().BeFalse();
        }

        [TestMethod]
        public void ParseInputUpFrontArgumentDash_Parsed()
        {
            var input = new ParsedInput("cmd1 -d \"-get version\"");

            input.ParsedCommands.Should().HaveCount(1);
            input.ParsedCommands[0].ToString().Should().Be("cmd1 -d \"-get version\"");
            input.IsValid.Should().BeTrue();

            input.ParsedCommands[0].Elements[4].Value.Should().Be("-get version");
        }

        [TestMethod]
        public void ParseInputEnclosedQuotes_Parsed()
        {
            var input = new ParsedInput("cmd1 -d \"he said, \"hello\"!\"");

            input.ParsedCommands.Should().HaveCount(1);
            input.ParsedCommands[0].ToString().Should().Be("cmd1 -d \"he said, \"hello\"!\"");
            input.IsValid.Should().BeTrue();

            input.ParsedCommands[0].Elements[4].Value.Should().Be("he said, \"hello\"!");
        }

        [TestMethod]
        public void ParseCommandWithNamespace()
        {
            var input = new ParsedInput("ns.cmd");

            input.ParsedCommands.Should().HaveCount(1);
            input.ParsedCommands[0].ToString().Should().Be("ns.cmd");
            input.IsValid.Should().BeTrue();
        }

    }
}
