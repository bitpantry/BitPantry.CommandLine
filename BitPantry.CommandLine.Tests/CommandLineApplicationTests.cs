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
                .RegisterCommand<TestExecuteAsync>()
                .RegisterCommand<TestExecuteCancel>()
                .RegisterCommand<TestExecuteError>()
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
        public void ExecuteAsync_Success()
        {
            _app.Run("testExecuteAsync").GetAwaiter().GetResult().ResultCode.Should().Be(0);
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
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Message.Should().Contain("unhandled exception");
            result.Errors[0].Exception.Should().NotBeNull();
        }

    }
}
