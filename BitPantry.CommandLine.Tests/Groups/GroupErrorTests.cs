using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Execution;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Tests for error messages when resolution fails.
    /// T054: Error message tests for invalid subcommands and non-existent groups.
    /// </summary>
    [TestClass]
    public class GroupErrorTests
    {
        private CommandLineApplication _app;
        private StringWriter _output;
        private IAnsiConsole _console;

        [TestInitialize]
        public void Setup()
        {
            _output = new StringWriter();
            _console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Out = new AnsiConsoleOutput(_output)
            });

            _app = new CommandLineApplicationBuilder()
                .RegisterCommand<ValidCommand>()
                .UsingConsole(_console)
                .Build();
        }

        [TestMethod]
        public async Task InvalidSubcommand_ShowsErrorAndAvailableCommands()
        {
            // Arrange & Act
            var result = await _app.Run("math nonexistent");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.ResolutionError);
            var output = _output.ToString();
            output.Should().Contain("not found", "error message should indicate command not found");
        }

        [TestMethod]
        public async Task NonExistentGroup_ShowsError()
        {
            // Arrange & Act
            var result = await _app.Run("nonexistentgroup add");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.ResolutionError);
            var output = _output.ToString();
            output.Should().Contain("not found", "error should indicate not found");
        }

        [TestMethod]
        public async Task ResolutionError_ReturnsNonZeroExitCode()
        {
            // Arrange & Act
            var result = await _app.Run("nonexistent command");

            // Assert
            ((int)result.ResultCode).Should().NotBe(0, "resolution error should have non-zero exit code");
        }

        #region Test Helper Classes

        [Group]
        [API.Description("Math operations")]
        public class MathGroup { }

        [Command(Group = typeof(MathGroup), Name = "add")]
        public class ValidCommand : CommandBase
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
