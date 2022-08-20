using BitPantry.CommandLine.Tests.Commands.ApplicationCommands;
using BitPantry.CommandLine.Tests.Components;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class CommandLineApplicationTests
    {
        static TestInterface _interface;
        static CommandLineApplication _app;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            _interface = new TestInterface();

            _app = new CommandLineApplicationBuilder()
                .RegisterCommand<TestExecute>()
                .RegisterCommand<TestExecuteCancel>()
                .RegisterCommand<TestExecuteError>()
                .RegisterCommand<TestExecuteWithReturnType>()
                .RegisterCommand<TestExecuteWithReturnTypeAsync>()
                .RegisterCommand<TestExecuteWithReturnTypeAsyncGeneric>()
                .UsingInterface(_interface)
                .Build();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (_app != null)
                _app.Dispose();
        }

        [TestMethod]
        public void TestExecute_Success()
        {
            _app.Run("testExecute").GetAwaiter().GetResult().ResultCode.Should().Be(0);
        }

        [TestMethod]
        public void ExecuteCancel_Success()
        {
            var stopWatch = Stopwatch.StartNew();

            var execution = _app.Run("testExecuteCancel");       
            _interface.CancelExecution();

            execution.GetAwaiter().GetResult().ResultCode.Should().Be(0);

            stopWatch.Stop();

            stopWatch.Elapsed.Milliseconds.Should().BeLessThan(200);
        }

        [TestMethod]
        public void ExecuteError_Error()
        {
            var execution = _app.Run("testExecuteError");

            var result = execution.GetAwaiter().GetResult();

            result.ResultCode.Should().Be(1003);
            result.RunError.Should().NotBeNull();
        }

        [TestMethod]
        public void ExecuteReturnType_Success()
        {
            var result = _app.Run("testExecuteWithReturnType").GetAwaiter().GetResult();

            result.Result.GetType().Should().Be<string>();
            result.Result.Should().Be("hello world!");
        }

        [TestMethod]
        public void ExecuteReturnTypeAsync_Success()
        {
            var result = _app.Run("testExecuteWithReturnTypeAsync").GetAwaiter().GetResult();

            result.Result.Should().BeNull();
        }

        [TestMethod]
        public void ExecuteReturnTypeAsyncGeneric_Success()
        {
            var result = _app.Run("testExecuteWithReturnTypeAsyncGeneric").GetAwaiter().GetResult();

            result.Result.GetType().Should().Be<int>();
            result.Result.Should().Be(42);
        }

    }
}
