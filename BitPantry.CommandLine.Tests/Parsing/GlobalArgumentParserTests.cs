using BitPantry.CommandLine.Processing.Parsing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Parsing
{
    /// <summary>
    /// Tests for GlobalArgumentParser which extracts global arguments
    /// (--profile, --help) from raw input before command parsing.
    /// </summary>
    [TestClass]
    public class GlobalArgumentParserTests
    {
        #region --profile / -P extraction

        [TestMethod]
        public void Parse_ProfileLongForm_ExtractsProfileName()
        {
            var result = GlobalArgumentParser.Parse("server connect --profile production", out var cleaned);

            result.ProfileName.Should().Be("production");
            cleaned.Should().Be("server connect");
        }

        [TestMethod]
        public void Parse_ProfileShortAlias_ExtractsProfileName()
        {
            var result = GlobalArgumentParser.Parse("server connect -P production", out var cleaned);

            result.ProfileName.Should().Be("production");
            cleaned.Should().Be("server connect");
        }

        [TestMethod]
        public void Parse_ProfileQuotedValue_ExtractsWithoutQuotes()
        {
            var result = GlobalArgumentParser.Parse("server connect --profile \"my profile\"", out var cleaned);

            result.ProfileName.Should().Be("my profile");
            cleaned.Should().Be("server connect");
        }

        [TestMethod]
        public void Parse_ProfileAtStart_ExtractsAndCleansInput()
        {
            var result = GlobalArgumentParser.Parse("--profile staging server connect", out var cleaned);

            result.ProfileName.Should().Be("staging");
            cleaned.Should().Be("server connect");
        }

        [TestMethod]
        public void Parse_ProfileInMiddle_ExtractsAndCleansInput()
        {
            var result = GlobalArgumentParser.Parse("server --profile staging connect", out var cleaned);

            result.ProfileName.Should().Be("staging");
            cleaned.Should().Be("server connect");
        }

        [TestMethod]
        public void Parse_NoProfile_ProfileNameIsNull()
        {
            var result = GlobalArgumentParser.Parse("server connect", out var cleaned);

            result.ProfileName.Should().BeNull();
            cleaned.Should().Be("server connect");
        }

        #endregion

        #region --help / -h extraction

        [TestMethod]
        public void Parse_HelpLongForm_SetsHelpRequested()
        {
            var result = GlobalArgumentParser.Parse("math add --help", out var cleaned);

            result.HelpRequested.Should().BeTrue();
            cleaned.Should().Be("math add");
        }

        [TestMethod]
        public void Parse_HelpShortForm_SetsHelpRequested()
        {
            var result = GlobalArgumentParser.Parse("math add -h", out var cleaned);

            result.HelpRequested.Should().BeTrue();
            cleaned.Should().Be("math add");
        }

        [TestMethod]
        public void Parse_HelpOnly_SetsHelpRequestedWithEmptyClean()
        {
            var result = GlobalArgumentParser.Parse("--help", out var cleaned);

            result.HelpRequested.Should().BeTrue();
            cleaned.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_ShortHelpOnly_SetsHelpRequestedWithEmptyClean()
        {
            var result = GlobalArgumentParser.Parse("-h", out var cleaned);

            result.HelpRequested.Should().BeTrue();
            cleaned.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_HelpAtStart_ExtractsAndCleansInput()
        {
            var result = GlobalArgumentParser.Parse("--help math add", out var cleaned);

            result.HelpRequested.Should().BeTrue();
            cleaned.Should().Be("math add");
        }

        [TestMethod]
        public void Parse_NoHelp_HelpRequestedIsFalse()
        {
            var result = GlobalArgumentParser.Parse("math add --num1 5", out var cleaned);

            result.HelpRequested.Should().BeFalse();
            cleaned.Should().Be("math add --num1 5");
        }

        #endregion

        #region Combined global arguments

        [TestMethod]
        public void Parse_ProfileAndHelp_BothExtracted()
        {
            var result = GlobalArgumentParser.Parse("server status --profile prod --help", out var cleaned);

            result.ProfileName.Should().Be("prod");
            result.HelpRequested.Should().BeTrue();
            cleaned.Should().Be("server status");
        }

        [TestMethod]
        public void Parse_HelpAndProfile_BothExtracted()
        {
            var result = GlobalArgumentParser.Parse("--help -P staging server status", out var cleaned);

            result.ProfileName.Should().Be("staging");
            result.HelpRequested.Should().BeTrue();
            cleaned.Should().Be("server status");
        }

        #endregion

        #region Edge cases

        [TestMethod]
        public void Parse_NullInput_ReturnsEmptyDefaults()
        {
            var result = GlobalArgumentParser.Parse(null, out var cleaned);

            result.ProfileName.Should().BeNull();
            result.HelpRequested.Should().BeFalse();
            cleaned.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_EmptyInput_ReturnsEmptyDefaults()
        {
            var result = GlobalArgumentParser.Parse("", out var cleaned);

            result.ProfileName.Should().BeNull();
            result.HelpRequested.Should().BeFalse();
            cleaned.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_NoGlobalArgs_InputPassedThrough()
        {
            var result = GlobalArgumentParser.Parse("math add --num1 5 --num2 10", out var cleaned);

            result.ProfileName.Should().BeNull();
            result.HelpRequested.Should().BeFalse();
            cleaned.Should().Be("math add --num1 5 --num2 10");
        }

        [TestMethod]
        public void Parse_MultipleSpaces_NormalizedToSingle()
        {
            var result = GlobalArgumentParser.Parse("math  --profile prod  add", out var cleaned);

            result.ProfileName.Should().Be("prod");
            cleaned.Should().Be("math add");
        }

        #endregion
    }
}
