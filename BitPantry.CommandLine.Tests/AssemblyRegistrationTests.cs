using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Tests.Commands.ResolveCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class AssemblyRegistrationTests
    {
        private static CommandLineApplication _app;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            _app = new CommandLineApplicationBuilder()
                .RegisterCommand<ExtendedCommand>()
                .Build();
        }

        [TestMethod]
        public void ExtendedCommand_success()
        {
            var result = _app.RunOnce("extendedCommand").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be(42);
        }
    }
}
