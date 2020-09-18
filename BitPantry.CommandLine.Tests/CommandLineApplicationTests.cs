using BitPantry.CommandLine.Tests.Commands.ApplicationCommands;
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
        static CommandLineApplication _app;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            _app = new CommandLineApplicationBuilder()
                .RegisterCommand<TestExecute>()
                .RegisterCommand<TestExecuteAsync>()
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

    }
}
