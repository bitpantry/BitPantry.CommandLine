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
    public class AutoCompleteBuilderTests_CommandName
    {
        private static CommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            _registry = new CommandRegistry(services);

            _registry.RegisterCommand<Command>(); // Command
            _registry.RegisterCommand<CommandWithNameAttribute>(); // myCommand
            _registry.RegisterCommand<MultipleArgumentsAndAliases>(); // MultipleArgumentsAndAliases propertyTwo|p prop|X
            _registry.RegisterCommand<CommandWithNamespace>(); // BitPantry.CommandWithNamespace
            _registry.RegisterCommand<DupNameDifferentNamespace>(); // BitPantry.Command

            _serviceProvider = services.BuildServiceProvider();
        }

        [DataTestMethod]
        [DataRow("co", 1)] // start
        [DataRow("co", 2)] // end
        [DataRow("co", 3)] // very end
        [DataRow("Command", 2)] // exact match
        [DataRow("Command", 8)] // exact match with cursor at very end
        [DataRow("COMM", 3)] // all caps
        public void AutoCompleteCommandName_AutoCompleted(string query, int position)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, query, position);
           
            ac.CurrentResult.AutoCompletedInputString.Should().Be("Command");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(8);
            ac.Options.Should().HaveCount(1);
        }

        [DataTestMethod]
        [DataRow("xyz", 1)] 
        [DataRow("abc", 2)]
        [DataRow("def", 3)]
        public void AutoCompleteNotExists_NoResult(string query, int position)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, query, position);

            ac.CurrentResult.Option.Should().BeNull();
            ac.Options.Should().HaveCount(0);
        }

        [DataTestMethod]
        [DataRow(5)]
        [DataRow(0)]
        [DataRow(-2)]
        public void AutoCompleteOffPostion_NoResult(int position)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "co", position);

            ac.CurrentResult.Option.Should().BeNull();
            ac.Options.Should().HaveCount(0);
        }

        [TestMethod]
        public void AutoCompleteValidCommandNameWithoutNamespace_NoResult()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "CommandWithNamespace", 1);

            ac.CurrentResult.Option.Should().BeNull();
            ac.Options.Should().HaveCount(0);
        }

        [DataTestMethod]
        [DataRow("BitPantry.CommandW", 5)] // partial
        [DataRow("BitPantry.CommandWithNamespace", 5)] // exact
        [DataRow("BITPANTRY.COMMANDW", 5)] // all caps
        [DataRow("BitPantry.CommandWithNamespace", 31)] // exact with cursor at very end
        public void AutoCompleteCommandWithNamespace_AutoCompleted(string query, int position)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, query, position);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("BitPantry.CommandWithNamespace");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(31);
            ac.Options.Should().HaveCount(1);

            ac.CurrentResult.Option.Value.Should().Be("CommandWithNamespace");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("BitPantry.CommandWithNamespace");
        }

        [DataTestMethod]
        [DataRow("BitPantry.Comm", 5)] // partial
        [DataRow("BitPantry.Command", 5)] // exact
        [DataRow("BITPANTRY.COMMAND", 5)] // all caps
        public void AutoCompleteCommandWithNamespaceWithOptions_AutoCompleted(string query, int position)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, query, position);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("BitPantry.Command");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(18);
            ac.Options.Should().HaveCount(2);

            ac.CurrentResult.Option.Value.Should().Be("Command");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("BitPantry.Command");

            ac.Options[1].Value.Should().Be("CommandWithNamespace");
            ac.Options[1].GetFormattedValue().Should().Be("BitPantry.CommandWithNamespace");
        }

        [TestMethod]
        public void AutoCompleteNamespace_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "Bit", 1);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("BitPantry.");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(11);
            ac.Options.Should().HaveCount(1);
        }

        [TestMethod]
        public void AutoCompleteFullNamespaceWithoutDot_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "BitPantry", 1);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("BitPantry.");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(11);
            ac.Options.Should().HaveCount(1);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        public void AutoCompleteEmptyString_NoResult(string emptyString)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, emptyString, 1);

            ac.CurrentResult.Option.Should().BeNull();
            ac.Options.Should().HaveCount(0);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        public void AutoCompleteEmptyStringWithNamespace_NoResult(string emptyString)
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, $"BitPantry.{emptyString}", 1);

            ac.CurrentResult.Option.Should().BeNull();
            ac.Options.Should().HaveCount(0);
        }

        [TestMethod]
        public void AutoCompleteCommandWithNamespaceChangeOptions_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "bitpantry.com", 1);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("BitPantry.Command");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(18);
            ac.Options.Should().HaveCount(2);

            ac.CurrentResult.Option.Value.Should().Be("Command");
            ac.CurrentResult.Option.GetFormattedValue().Should().Be("BitPantry.Command");

            ac.Options[1].Value.Should().Be("CommandWithNamespace");
            ac.Options[1].GetFormattedValue().Should().Be("BitPantry.CommandWithNamespace");

            ac.NextOption().Should().Be(true);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("BitPantry.CommandWithNamespace");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(31);

            ac.NextOption().Should().Be(true);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("BitPantry.Command");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(18);

            ac.PreviousOption().Should().Be(true);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("BitPantry.CommandWithNamespace");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(31);
        }

        [TestMethod]
        public void AutoCompletePipedInput_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "command | my", 13);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("myCommand");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(11);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(20);
            ac.Options.Should().HaveCount(1);
        }

        [TestMethod]
        public void AutoCompleteFirstCommandPipedInput_AutoCompleted()
        {
            var ac = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, "com | my", 2);

            ac.CurrentResult.AutoCompletedInputString.Should().Be("Command | my");
            ac.CurrentResult.AutoCompleteStartPosition.Should().Be(1);
            ac.CurrentResult.AutoCompletedCursorPosition.Should().Be(8);
            ac.Options.Should().HaveCount(1);
        }

    }
}
