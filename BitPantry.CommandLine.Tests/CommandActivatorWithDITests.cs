using System;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Execution;
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
            var appBuilder = new CommandLineApplicationBuilder();

            appBuilder.RegisterCommands(typeof(Dep1));
            appBuilder.Services.AddTransient<Dep1>();

            _app = appBuilder.Build();

        }

        [TestMethod]
        public void CommandExecute_DoesNotExist()
        {
            var result = _app.Run("doesNotExist").GetAwaiter().GetResult();
            result.ResultCode.Should().Be(RunResultCode.ResolutionError);
        }

        [TestMethod]
        public void CommandExecute_NotDiClass_DoesNotExist()
        {
            var result = _app.Run("badBaseCommand").GetAwaiter().GetResult();
            result.ResultCode.Should().Be(RunResultCode.ResolutionError);
        }

        [TestMethod]
        public void CommandExecute_NoDeps_Executes()
        {
            _app.Run("testCommandOneNoDeps").GetAwaiter().GetResult().ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public void CommandExecute_Deps_Executes()
        {
            _app.Run("testCommandTwoWithDeps").GetAwaiter().GetResult().ResultCode.Should().Be(RunResultCode.Success);
        }
    }
}
