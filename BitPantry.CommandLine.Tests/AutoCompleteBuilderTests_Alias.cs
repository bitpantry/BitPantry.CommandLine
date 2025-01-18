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
    public class AutoCompleteBuilderTests_Alias
    {
        private static CommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            _registry = new CommandRegistry(services);

            _registry.RegisterCommand<CommandWithArg>(); // CommandWithArg arg1|a
            _registry.RegisterCommand<CommandWithTwoArgs>(); // CommandWithTwoArgs arg1|a xyzq|x

            _serviceProvider = services.BuildServiceProvider();
        }


        [TestMethod]
        public void AutoCompleteOneNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArg -", 17);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("a");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("-a");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("-a");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(18);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(16);

            ac.Options.Should().HaveCount(1);
        }

        [TestMethod]
        public void AutoCompleteOneArgUpperCaseQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArg -A", 17);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("a");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("-a");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("-a");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(18);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(16);

            ac.Options.Should().HaveCount(1);
        }

        [TestMethod]
        public void AutoCompleteOneArgBadQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithArg -c", 17);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("a");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("-a");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("-a");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(18);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(16);

            ac.Options.Should().HaveCount(1);
        }

        [TestMethod]
        public void AutoCompleteFirstAlias_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgs -", 21);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("a");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("-a");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("-a");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(22);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(20);

            ac.Options.Should().HaveCount(2);
        }

        [TestMethod]
        public void AutoCompleteFirstAliasSecondHasValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgs - -x", 21);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("a");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("-a");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("-a -x");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(22);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(20);

            ac.Options.Should().HaveCount(1);
        }

        [TestMethod]
        public void AutoCompleteFirstAliasSecondHasBadValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithTwoArgs - -y", 21);

            ac.CurrentResult.Option.Should().NotBeNull();
            ac.CurrentResult.Option.Value.Should().Be("a");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("-a");
            ac.CurrentResult.AutoCompletedInputString.Should().Be("-a -y");
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(22);
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(20);

            ac.Options.Should().HaveCount(2);
        }


    }
}
