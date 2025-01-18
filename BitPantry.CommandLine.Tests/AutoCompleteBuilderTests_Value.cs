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
    public class AutoCompleteBuilderTests_Value
    {
        private static CommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            _registry = new CommandRegistry(services);

            _registry.RegisterCommand<CommandWithArgAc>(); // CommandWithArgAc arg1|a
            _registry.RegisterCommand<CommandWithTwoArgAc>(); // CommandWithTwoArgAc arg1|a arg2|b

            _serviceProvider = services.BuildServiceProvider();
        }

        [TestMethod]
        public void AutoCompleteArg1NoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArgAc --arg1 ", 25);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Opt1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(29);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(25);

            ac.Options.Should().HaveCount(3);
        }

        [TestMethod]
        public void AutoCompleteArg1NoQueryAndSpace_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArgAc --arg1   ", 27);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Opt1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(31);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(27);

            ac.Options.Should().HaveCount(3);
        }

        [TestMethod]
        public void AutoCompleteBadArgName_NoResult()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArgAc --arg ", 24);

            ac.Options.Should().HaveCount(0);
        }

        [TestMethod]
        public void AutoCompleteArg1WithQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArgAc --arg1 b", 26);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Big2");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Big2");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("Big2");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(29);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(25);

            ac.Options.Should().HaveCount(1);
        }

        [TestMethod]
        public void AutoCompleteArg1WithQueryMultipleOptions_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArgAc --arg1 o", 26);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Opt1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(29);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(25);

            ac.Options.Should().HaveCount(2);
        }

        [TestMethod]
        public void AutoCompleteArg1WithQueryMultipleOptionsNavigate_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArgAc --arg1 o", 26);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Opt1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(29);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(25);

            ac.Options.Should().HaveCount(2);

            ac.NextOption().Should().Be(true);

            ac.CurrentResult.Option.Value.Should().Be("obc3");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("obc3");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("obc3");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(29);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(25);

            ac.NextOption().Should().Be(true);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Opt1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(29);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(25);
        }

        [TestMethod]
        public void AutoCompleteAliasNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArgAc -a ", 21);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Opt1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(25);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(21);

            ac.AutoCompleteContext.Should().NotBeNull();
            ac.AutoCompleteContext.Values.Should().HaveCount(0);

            ac.Options.Should().HaveCount(3);
        }

        [TestMethod]
        public void AutoCompleteBadAlias_NoResult()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArgAc -aaa ", 22);

            ac.Options.Should().HaveCount(0);            
        }

        [TestMethod]
        public void AutoCompleteFirstAliasNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgAc -a  -b", 24);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Opt1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("Opt1 -b");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(28);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(24);

            ac.Options.Should().HaveCount(3);
        }

        [TestMethod]
        public void AutoCompleteCtxFirstArgWithSecondValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgAc -a  -b x", 24);

            ac.AutoCompleteContext.Should().NotBeNull();
            ac.AutoCompleteContext.Values.Should().HaveCount(1);

            ac.AutoCompleteContext.Values.Keys.First().Alias.Should().Be('b');
            ac.AutoCompleteContext.Values.Values.First().Should().Be("x");

            ac.Options.Should().HaveCount(3);
        }

        [TestMethod]
        public void AutoCompleteCtxFirstArgWithAllValues_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgAc -a v -b x", 25);

            ac.AutoCompleteContext.Should().NotBeNull();
            ac.AutoCompleteContext.Values.Should().HaveCount(2);

            ac.AutoCompleteContext.Values.Keys.First().Alias.Should().Be('a');
            ac.AutoCompleteContext.Values.Values.First().Should().Be("v");

            ac.Options.Should().HaveCount(0);
        }

        [TestMethod]
        public void AutoCompleteCtxFirstArgWithBadArg_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgAc -a v -b x -c 123", 25);

            ac.AutoCompleteContext.Should().NotBeNull();
            ac.AutoCompleteContext.Values.Should().HaveCount(2);

            ac.AutoCompleteContext.Values.Keys.First().Alias.Should().Be('a');
            ac.AutoCompleteContext.Values.Values.First().Should().Be("v");

            ac.Options.Should().HaveCount(0);
        }

        [DataTestMethod]
        [DataRow(24)]
        [DataRow(25)]
        [DataRow(26)]
        [DataRow(27)]
        public void AutoCompleteNoQuerySplitTrailingWhitespace_AutoCompleted(int cursorPosition)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgAc -a     ", cursorPosition);

            var padding = string.Empty.PadLeft(27 - cursorPosition, ' ');

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Opt1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be($"Opt1");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(cursorPosition + "Opt1".Length);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(cursorPosition);

            ac.Options.Should().HaveCount(3);
        }

        [DataTestMethod]
        [DataRow(24)]
        [DataRow(25)]
        [DataRow(26)]
        [DataRow(27)]
        public void AutoCompleteFirstAliasNoQuerySplitWhitespace_AutoCompleted(int cursorPosition)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgAc -a     -b", cursorPosition);

            var padding = string.Empty.PadLeft(27 - cursorPosition, ' ');

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("Opt1");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("Opt1");
            ac.CurrentResult.AutoCompletedInputString.Should().Be($"Opt1 {padding}-b");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(cursorPosition + "Opt1".Length);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(cursorPosition);

            ac.Options.Should().HaveCount(3);
        }

    }
}
