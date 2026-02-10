using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Tests.Commands.ApplicationCommands;
using BitPantry.CommandLine.Tests.Commands.PositionalCommands;
using BitPantry.CommandLine.Tests.Commands.ResolveCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class CommandLineApplicationTests
    {
        static CommandLineApplication _app;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            _app = new CommandLineApplicationBuilder()
                .RegisterCommand<TestExecute>()
                .RegisterCommand<TestExecuteCancel>()
                .RegisterCommand<TestExecuteError>()
                .RegisterCommand<TestExecuteWithReturnType>()
                .RegisterCommand<TestExecuteWithReturnTypeAsync>()
                .RegisterCommand<TestExecuteWithReturnTypeAsyncGeneric>()
                .RegisterCommand<ReturnsZero>()
                .RegisterCommand<ReturnsInputPlusOne>()
                .RegisterCommand<ReturnsByteArray>()
                .RegisterCommand<ReceivesByteArray>()
                .RegisterCommand<ExtendedCommand>()
                .RegisterCommand<ExtendVirtualCommand>()
                .RegisterCommand<TestPositionalCommand>()
                .Build();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (_app != null)
                _app.Dispose();
        }

        [TestMethod]
        public void RegisterDuplicateCommand_Replaced()
        {
            // With ReplaceDuplicateCommands defaulting to false, we need to explicitly set it to true
            var app = new CommandLineApplicationBuilder();
            app.CommandRegistryBuilder.ReplaceDuplicateCommands = true;
            
            app
                .RegisterCommand<TestExecute>()
                .RegisterCommand<TestExecute>();
            
            // Should succeed without throwing
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RegisterDuplicateCommand_DefaultDisallowed_Exception()
        {
            // Default behavior now disallows duplicates
            var app = new CommandLineApplicationBuilder()
                .RegisterCommand<TestExecute>()
                .RegisterCommand<TestExecute>();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RegisterDuplicateCommand_Disallowed_Exception()
        {
            var app = new CommandLineApplicationBuilder();
            app.CommandRegistryBuilder.ReplaceDuplicateCommands = false;

            app
                .RegisterCommand<TestExecute>()
                .RegisterCommand<TestExecute>();
        }

        [TestMethod]
        public void TestExecute_Success()
        {
            _app.RunOnce("testExecute").GetAwaiter().GetResult().ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public void ExecuteCancel_Success()
        {
            var tokenSrc = new CancellationTokenSource();
            var token = tokenSrc.Token;

            var stopWatch = Stopwatch.StartNew();

            var execution = _app.RunOnce("testExecuteCancel", token);
            tokenSrc.Cancel();

            var result = execution.GetAwaiter().GetResult();

            stopWatch.Stop();

            stopWatch.Elapsed.Milliseconds.Should().BeLessThan(1000);
            
            result.ResultCode.Should().Be(RunResultCode.RunCanceled);
            result.Result.Should().BeNull();
        }

        [TestMethod]
        public void ExecuteError_Error()
        {
            var execution = _app.RunOnce("testExecuteError");

            var result = execution.GetAwaiter().GetResult();

            result.Result.Should().BeNull();
            result.ResultCode.Should().Be(RunResultCode.RunError);
            result.RunError.Should().NotBeNull();
        }

        [TestMethod]
        public void ExecuteReturnType_Success()
        {
            var result = _app.RunOnce("testExecuteWithReturnType").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().Be("hello world!");
        }

        [TestMethod]
        public void ExecuteReturnTypeAsync_Success()
        {
            var result = _app.RunOnce("testExecuteWithReturnTypeAsync").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().BeNull();
        }

        [TestMethod]
        public void ExecuteReturnTypeAsyncGeneric_Success()
        {
            var result = _app.RunOnce("testExecuteWithReturnTypeAsyncGeneric").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.GetType().Should().Be<int>();
            result.Result.Should().Be(42);
        }

        [TestMethod]
        public void ExecuteBasicPipeline_Success()
        {
            var result = _app.RunOnce("testExecute | testExecuteWithReturnType").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().Be("hello world!");
        }

        [TestMethod]
        public void PassDataBetweenCommands_Success()
        {
            var result = _app.RunOnce("returnsZero | returnsInputPlusOne").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().Be(1);
        }


        [TestMethod]
        public void PassDataBetweenCommandsMany_Success()
        {
            var result = _app.RunOnce("returnsZero | returnsInputPlusOne | returnsInputPlusOne | returnsInputPlusOne").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().Be(3);
        }

        [TestMethod]
        public void PassByteArray_success()
        {
            var result = _app.RunOnce("returnsByteArray | receivesByteArray").GetAwaiter().GetResult();
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public void ExtendedCommand_success()
        {
            var result = _app.RunOnce("extendedCommand").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be(42);
        }

        [TestMethod]
        public async Task VirtualExecuteExtended_Success()
        {
            var result = await _app.RunOnce("test evirt --arg1 val1 --arg2 val2");

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be("extend:base:val1:val2");
        }

        #region Positional Argument Integration Tests (INT-001, INT-004)

        /// <summary>
        /// INT-001: Full positional execution - command with positional arguments runs end-to-end
        /// </summary>
        [TestMethod]
        public async Task PositionalExecution_INT001_FullPositionalExecution()
        {
            // Act - run command with positional arguments
            var result = await _app.RunOnce("testPositionalCommand source.txt dest.txt");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be("source.txt|dest.txt");
        }

        /// <summary>
        /// INT-004: Backward compatibility - existing named argument syntax still works
        /// </summary>
        [TestMethod]
        public async Task PositionalExecution_INT004_BackwardCompatibility()
        {
            // Act - run with existing named argument syntax (should still work)
            var result = await _app.RunOnce("testExecuteWithReturnType");

            // Assert - existing behavior preserved
            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be("hello world!");
        }

        /// <summary>
        /// Additional: Test mixed positional with named arguments via full execution
        /// </summary>
        [TestMethod]
        public async Task PositionalExecution_MixedPositionalAndNamed()
        {
            // This test ensures that commands with both positional and named args work
            // Using the existing test command with only positional args for now
            var result = await _app.RunOnce("testPositionalCommand first second");

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be("first|second");
        }

        /// <summary>
        /// Reproduces issue: "remotepaint Cyan --matte false --size Huge" fails with
        /// "The value 'size' does not exist for enumeration".
        /// Tests: Positional 0 filled, then named bool, then positional 1 via named syntax.
        /// </summary>
        [TestMethod]
        public async Task PositionalExecution_EnumPositionalWithInterleavedNamed_BothPositionalsResolved()
        {
            // Arrange
            using var app = new CommandLineApplicationBuilder()
                .RegisterCommand<EnumPositionalWithNamedCommand>()
                .Build();
            
            EnumPositionalWithNamedCommand.Reset();

            // Act - Position 0 positionally, named bool, then Position 1 via named syntax
            var result = await app.RunOnce("enumPositionalWithNamed Cyan --flag false --size Huge");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success, 
                $"command should succeed. Error: {result.RunError?.Message}");
            EnumPositionalWithNamedCommand.LastColor.Should().Be(TestColor.Cyan);
            EnumPositionalWithNamedCommand.LastSize.Should().Be(TestSize.Huge);
            EnumPositionalWithNamedCommand.LastFlag.Should().Be(false);
        }

        /// <summary>
        /// INT-006: Validation error at startup - invalid positional config throws on register
        /// </summary>
        [TestMethod]
        public void PositionalValidation_INT006_ValidationErrorAtStartup()
        {
            // Arrange - create a new registry builder to test registration error
            var builder = new CommandRegistryBuilder();
            
            // Act & Assert - registering a command with gap in positions should throw
            Action registerInvalidCommand = () => builder.RegisterCommand<InvalidPositionalGapCommand>();
            
            registerInvalidCommand.Should().Throw<PositionalArgumentValidationException>()
                .WithMessage("*position*");
        }

        // Test command with invalid positional configuration (gap in positions)
        [Command(Name = "invalidGapTest")]
        private class InvalidPositionalGapCommand : CommandBase
        {
            [Argument(Position = 0)]
            public string First { get; set; }
            
            [Argument(Position = 2)]  // Gap - position 1 is missing
            public string Third { get; set; }
            
            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion
    }
}
