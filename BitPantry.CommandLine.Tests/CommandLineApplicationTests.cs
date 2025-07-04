﻿using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Tests.Commands.ApplicationCommands;
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
            var app = new CommandLineApplicationBuilder()
                .RegisterCommand<TestExecute>()
                .RegisterCommand<TestExecute>();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RegisterDuplicateCommand_Disallowed_Exception()
        {
            var app = new CommandLineApplicationBuilder();
            app.CommandRegistry.ReplaceDuplicateCommands = false;

            app
                .RegisterCommand<TestExecute>()
                .RegisterCommand<TestExecute>();
        }

        [TestMethod]
        public void TestExecute_Success()
        {
            _app.Run("testExecute").GetAwaiter().GetResult().ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public void ExecuteCancel_Success()
        {
            var tokenSrc = new CancellationTokenSource();
            var token = tokenSrc.Token;

            var stopWatch = Stopwatch.StartNew();

            var execution = _app.Run("testExecuteCancel", token);
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
            var execution = _app.Run("testExecuteError");

            var result = execution.GetAwaiter().GetResult();

            result.Result.Should().BeNull();
            result.ResultCode.Should().Be(RunResultCode.RunError);
            result.RunError.Should().NotBeNull();
        }

        [TestMethod]
        public void ExecuteReturnType_Success()
        {
            var result = _app.Run("testExecuteWithReturnType").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().Be("hello world!");
        }

        [TestMethod]
        public void ExecuteReturnTypeAsync_Success()
        {
            var result = _app.Run("testExecuteWithReturnTypeAsync").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().BeNull();
        }

        [TestMethod]
        public void ExecuteReturnTypeAsyncGeneric_Success()
        {
            var result = _app.Run("testExecuteWithReturnTypeAsyncGeneric").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.GetType().Should().Be<int>();
            result.Result.Should().Be(42);
        }

        [TestMethod]
        public void ExecuteBasicPipeline_Success()
        {
            var result = _app.Run("testExecute | testExecuteWithReturnType").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().Be("hello world!");
        }

        [TestMethod]
        public void PassDataBetweenCommands_Success()
        {
            var result = _app.Run("returnsZero | returnsInputPlusOne").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().Be(1);
        }


        [TestMethod]
        public void PassDataBetweenCommandsMany_Success()
        {
            var result = _app.Run("returnsZero | returnsInputPlusOne | returnsInputPlusOne | returnsInputPlusOne").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.RunError.Should().BeNull();
            result.Result.Should().Be(3);
        }

        [TestMethod]
        public void PassByteArray_success()
        {
            var result = _app.Run("returnsByteArray | receivesByteArray").GetAwaiter().GetResult();
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public void ExtendedCommand_success()
        {
            var result = _app.Run("extendedCommand").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be(42);
        }

        [TestMethod]
        public async Task VirtualExecuteExtended_Success()
        {
            var result = await _app.Run("test.evirt --arg1 val1 --arg2 val2");

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be("extend:base:val1:val2");
        }
    }
}
