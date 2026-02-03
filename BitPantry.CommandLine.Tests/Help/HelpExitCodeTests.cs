using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Execution;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Help
{
    /// <summary>
    /// Tests for help exit codes - FR-010.
    /// T032: Verify group help and command help exit with code 0
    /// </summary>
    [TestClass]
    public class HelpExitCodeTests
    {
        private CommandLineApplication _app;

        [TestInitialize]
        public void Setup()
        {
            _app = new CommandLineApplicationBuilder()
                .RegisterCommand<AddCommand>()
                .Build();
        }

        [TestMethod]
        public async Task GroupHelp_ExitsWithCode0()
        {
            // Arrange & Act - FR-010: help should exit successfully
            var result = await _app.Run("math --help");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public async Task CommandHelp_ExitsWithCode0()
        {
            // Arrange & Act
            var result = await _app.Run("math add --help");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public async Task RootHelp_ExitsWithCode0()
        {
            // Arrange & Act
            var result = await _app.Run("--help");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public async Task GroupHelpShortForm_ExitsWithCode0()
        {
            // Arrange & Act - T033: -h shorthand
            var result = await _app.Run("math -h");

            // Assert - US2-3: -h should work same as --help
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public async Task CommandHelpShortForm_ExitsWithCode0()
        {
            // Arrange & Act
            var result = await _app.Run("math add -h");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public async Task RootHelpShortForm_ExitsWithCode0()
        {
            // Arrange & Act
            var result = await _app.Run("-h");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

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
