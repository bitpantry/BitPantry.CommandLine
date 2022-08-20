using System;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Tests.CmdAssemblies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class CommandActivatorWithDITests
    {
        static CommandLineApplication _app;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var svcProvider = new ServiceCollection()
                .AddCommands(typeof(Dep1))
                .AddTransient<Dep1>()
            .BuildServiceProvider();

            _app = new CommandLineApplicationBuilder()
                .UsingDependencyContainer(new ServiceProviderContainer(svcProvider))
                .RegisterCommands(typeof(Dep1))
            .Build();
        }

        [TestMethod]
        public void CommandExecute_DoesNotExist()
        {
            var result = _app.Run("doesNotExist").GetAwaiter().GetResult();
            result.ResultCode.Should().Be(1002);
        }

        [TestMethod]
        public void CommandExecute_NotDiClass_DoesNotExist()
        {
            var result = _app.Run("badBaseCommand").GetAwaiter().GetResult();
            result.ResultCode.Should().Be(1002);
        }

        [TestMethod]
        public void CommandExecute_NoDeps_Executes()
        {
            _app.Run("testCommandOneNoDeps").GetAwaiter().GetResult().ResultCode.Should().Be(0);
        }

        [TestMethod]
        public void CommandExecute_Deps_Executes()
        {
            _app.Run("testCommandTwoWithDeps").GetAwaiter().GetResult().ResultCode.Should().Be(0);
        }
    }
}
