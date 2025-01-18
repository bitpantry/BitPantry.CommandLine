using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class AutoCompleteBuilderTests_ArgName
    {
        private static CommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            _registry = new CommandRegistry(services);

            _registry.RegisterCommand<Command>(); // Command
            _registry.RegisterCommand<MultipleArgumentsAndAliases>(); // MultipleArgumentsAndAliases propertyTwo|p prop|X
            _registry.RegisterCommand<CommandWithArgAc>(); // CommandWithArgAc arg1
            _registry.RegisterCommand<CommandWithArg>(); // CommandWithArgNoAc arg1
            _registry.RegisterCommand<CommandWithTwoArgs>(); // CommandWithTwoArgs arg1 xyzq

            _serviceProvider = services.BuildServiceProvider();
        }

        [TestMethod]
        public void AutoCompleteNoArgs_NoResult()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "Command --", 11);
           
            ac.Options.Should().HaveCount(0);
        }

        [TestMethod]
        public void AutoCompleteOneArgNonMatchingQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArg --m", 19);  
            
            ac.Options.Should().HaveCount(0);
        }

        [TestMethod]
        public void AutoCompleteOneArgNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArg --", 18);  

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Arg1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(22);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(16);

            ac.Options.Should().HaveCount(1);
        }

        [DataTestMethod]
        [DataRow(16)]
        [DataRow(17)]
        [DataRow(18)]
        public void AutoCompleteOneArgNoQueryDifferentPos_AutoCompleted(int position)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArg --", position);  

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Arg1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(22);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(16);

            ac.Options.Should().HaveCount(1);
        }

        [TestMethod]
        public void AutoCompleteCmdNameOneArgNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWith --", 2);  

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("CommandWithArg");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("CommandWithArg");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("CommandWithArg --");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(15);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);

            ac.Options.Should().HaveCount(3);
        }

        [TestMethod]
        public void AutoCompleteArgNameNoCommand_NoResult()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, " --", 2);

            ac.Options.Should().HaveCount(0);
        }

        [TestMethod]
        public void AutoCompleteFirstArgument_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgs --", 22);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Arg1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(26);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(20);

            ac.Options.Should().HaveCount(2);
        }

        [TestMethod]
        public void AutoCompleteFirstArgumentNavigateOptions_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgs --", 22);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Arg1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(26);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(20);

            ac.Options.Should().HaveCount(2);

            ac.NextOption().Should().BeTrue();

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("XyzQp");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("--XyzQp");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("--XyzQp");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(27);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(20);

            ac.NextOption().Should().BeTrue();

            ac.CurrentResult.Option.Value.Should().Be("Arg1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(26);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(20);
        }

        [TestMethod]
        public void AutoCompleteSecondArgumentFirstHasValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgs --arg1 --", 29);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("XyzQp");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("--XyzQp");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("--XyzQp");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(34);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(27);

            ac.Options.Should().HaveCount(1);
        }

        [TestMethod]
        public void AutoCompleteSecondArgumentFirstHasBadValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgs --arg9 --", 29);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Arg1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(33);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(27);

            ac.Options.Should().HaveCount(2);
        }

        [TestMethod]
        public void AutoCompleteFirstArgumentFirstHasNoValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgs --arg --", 25);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Arg1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("--Arg1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("--Arg1 --");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(26);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(20);

            ac.Options.Should().HaveCount(1);
        }

    }
}
