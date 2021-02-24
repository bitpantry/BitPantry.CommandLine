using BitPantry.CommandLine.Tests.Commands.ApplicationCommands;
using BitPantry.CommandLine.Tests.Components;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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
            _app.Run("testExecute").GetAwaiter().GetResult().Should().Be(0);
        }

        [TestMethod]
        public void ExecuteAsync_Success()
        {
            _app.Run("testExecuteAsync").GetAwaiter().GetResult().Should().Be(10);
        }

        [TestMethod]
        public void ExecuteCancel_Success()
        {
            var execution = _app.Run("testExecuteCancel");

            _interface.CancelExecution();

            execution.GetAwaiter().GetResult().Should().BeLessThan(200);
        }

    }
}
