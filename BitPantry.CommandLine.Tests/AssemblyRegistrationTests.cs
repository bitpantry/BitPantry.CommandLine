using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.CommandLine.Tests.CmdAssemblies;
using BitPantry.CommandLine.Tests.Commands.ActivateCommands;
using BitPantry.CommandLine.Tests.Commands.DescribeCommands;
using BitPantry.CommandLine.Tests.Commands.ResolveCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                .RegisterCommands(
                    new Type[] { typeof(AssemblyRegistrationTests) }, 
                    new Type[] 
                        { 
                            typeof(BadCommandName),
                            typeof(BadNamespace_EmptySegment),
                            typeof(BadNamespace_InvalidChars),
                            typeof(BadNamespace_Spaces),
                            typeof(BadBaseCommand),
                            typeof(InvalidExecuteParametersAsync),
                            typeof(InvalidExecuteReturn),
                            typeof(InvalidExecuteReturnAsync),
                            typeof(NoExecute),
                            typeof(Commands.ResolveCommands.Command),
                            typeof(Commands.ActivateCommands.Command)
                        })
                .Build();
        }

        [TestMethod]
        public void ExtendedCommand_success()
        {
            var result = _app.Run("extendedCommand").GetAwaiter().GetResult();

            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be(42);
        }
    }
}
