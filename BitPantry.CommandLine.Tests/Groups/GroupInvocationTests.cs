using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Execution;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// End-to-end tests for command execution via group path.
    /// T023: End-to-end test of command execution via group path
    /// T024: Root-level command test (FR-006)
    /// T025: Argument parsing test (FR-014)
    /// </summary>
    [TestClass]
    public class GroupInvocationTests
    {
        private CommandLineApplication _app;

        [TestInitialize]
        public void TestInitialize()
        {
            _app = new CommandLineApplicationBuilder()
                .RegisterCommand<AddCommand>()
                .RegisterCommand<SubtractCommand>()
                .RegisterCommand<VersionCommand>()
                .Build();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _app?.Dispose();
        }

        [TestMethod]
        public async Task InvokeGroupedCommand_Success()
        {
            // Arrange & Act
            var result = await _app.Run("math add --num1 5 --num2 3");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be(8);
        }

        [TestMethod]
        public async Task InvokeGroupedCommand_WithArguments_ArgumentsParsed()
        {
            // Arrange & Act (FR-014 - tokens after command resolution are parsed as arguments)
            var result = await _app.Run("math subtract --value 10");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be(-10);
        }

        [TestMethod]
        public async Task InvokeRootLevelCommand_Success()
        {
            // Arrange & Act (FR-006 - command with no Group property)
            var result = await _app.Run("version");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be("1.0.0");
        }

        [TestMethod]
        public async Task InvokeGroupedCommand_CaseInsensitive()
        {
            // Arrange & Act
            var result = await _app.Run("MATH ADD --num1 2 --num2 2");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be(4);
        }

        [TestMethod]
        public async Task InvokeDotNotation_NotRecognized()
        {
            // Arrange & Act - old dot notation should fail
            var result = await _app.Run("math.add --num1 1 --num2 1");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.ResolutionError);
        }

        [TestMethod]
        public async Task InvokeNonExistentCommand_Error()
        {
            // Arrange & Act
            var result = await _app.Run("math nonexistent");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.ResolutionError);
        }

        // Test helper classes
        [Group]
        public class MathGroup { }

        [Command(Group = typeof(MathGroup), Name = "add")]
        public class AddCommand : CommandBase
        {
            [Argument]
            public int Num1 { get; set; }

            [Argument]
            public int Num2 { get; set; }

            public int Execute(CommandExecutionContext ctx)
            {
                return Num1 + Num2;
            }
        }

        [Command(Group = typeof(MathGroup), Name = "subtract")]
        public class SubtractCommand : CommandBase
        {
            [Argument]
            public int Value { get; set; }

            public int Execute(CommandExecutionContext ctx)
            {
                return -Value;
            }
        }

        [Command(Name = "version")]
        public class VersionCommand : CommandBase
        {
            public string Execute(CommandExecutionContext ctx)
            {
                return "1.0.0";
            }
        }
    }
}
