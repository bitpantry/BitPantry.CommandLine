using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Execution;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Help
{
    /// <summary>
    /// Tests for help flag interception behavior.
    /// T031: Test help flag detection and validation
    /// FR-018a: Help must be standalone - combining with other args returns error
    /// FR-018b: Error message format specification
    /// </summary>
    [TestClass]
    public class HelpInterceptionTests
    {
        private CommandLineApplication _app;

        [TestInitialize]
        public void Setup()
        {
            _app = new CommandLineApplicationBuilder()
                .RegisterCommand<AddCommand>()
                .Build();
        }

        #region Help Flag Detection Tests

        [TestMethod]
        public async Task HelpFlag_LongForm_Detected()
        {
            // Arrange & Act
            var result = await _app.RunOnce("math add --help");

            // Assert - help should be detected and handled
            result.ResultCode.Should().Be(RunResultCode.Success);
            // Help output would be written to console
        }

        [TestMethod]
        public async Task HelpFlag_ShortForm_Detected()
        {
            // Arrange & Act
            var result = await _app.RunOnce("math add -h");

            // Assert - -h should work same as --help
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public async Task HelpFlag_GroupOnly_ShowsGroupHelp()
        {
            // Arrange & Act
            var result = await _app.RunOnce("math --help");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public async Task HelpFlag_GroupShortForm_ShowsGroupHelp()
        {
            // Arrange & Act
            var result = await _app.RunOnce("math -h");

            // Assert - US2-3: -h shorthand works for groups too
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        #endregion

        #region Help Standalone Validation Tests (FR-018a)

        [TestMethod]
        public async Task HelpWithOtherArgs_ReturnsError()
        {
            // Arrange & Act - FR-018a: help must be standalone
            var result = await _app.RunOnce("math add --num1 5 --help");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.HelpValidationError);
        }

        [TestMethod]
        public async Task HelpBeforeArgs_ReturnsError()
        {
            // Arrange & Act
            var result = await _app.RunOnce("math add --help --num1 5");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.HelpValidationError);
        }

        [TestMethod]
        public async Task ShortHelpWithOtherArgs_ReturnsError()
        {
            // Arrange & Act
            var result = await _app.RunOnce("math add -h --num1 5");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.HelpValidationError);
        }

        #endregion

        #region Error Message Format Tests (FR-018b)

        [TestMethod]
        public async Task HelpWithOtherArgs_ErrorMessageFormat()
        {
            // Arrange & Act
            var result = await _app.RunOnce("math add --num1 5 --help");

            // Assert - FR-018b: specific error message format
            // Note: Actual message validation would be done via console capture
            result.ResultCode.Should().Be(RunResultCode.HelpValidationError);
        }

        #endregion

        #region Pipeline Help Tests

        [TestMethod]
        public async Task PipelineWithHelp_ReturnsHelpDisplayed()
        {
            // Arrange & Act - pipeline with help: --help is stripped globally,
            // pipeline is treated as unrecognized target → root help displayed
            var result = await _app.RunOnce("math add | other-cmd --help");

            // Assert - global parser strips --help, pipeline doesn't match single command,
            // so root help is shown and HelpDisplayed (= Success = 0) is returned
            result.ResultCode.Should().Be(RunResultCode.HelpDisplayed);
        }

        #endregion

        #region Test Helper Classes

        [Group]
        private class MathGroup { }

        [InGroup<MathGroup>]
        [Command(Name = "add")]
        private class AddCommand : CommandBase
        {
            [Argument]
            public int Num1 { get; set; }

            [Argument]
            public int Num2 { get; set; }

            public int Execute(CommandExecutionContext ctx) => Num1 + Num2;
        }

        #endregion
    }
}
