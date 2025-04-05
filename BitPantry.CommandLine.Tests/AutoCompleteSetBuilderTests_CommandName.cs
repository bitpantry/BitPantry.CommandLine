using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class AutoCompleteSetBuilderTests_CommandName
    {
        private static CommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            _registry = new CommandRegistry();

            _registry.RegisterCommand<Command>(); // Command
            _registry.RegisterCommand<CommandWithNameAttribute>(); // myCommand
            _registry.RegisterCommand<MultipleArgumentsAndAliases>(); // MultipleArgumentsAndAliases propertyTwo|p prop|X
            _registry.RegisterCommand<CommandWithNamespace>(); // BitPantry.CommandWithNamespace
            _registry.RegisterCommand<DupNameDifferentNamespace>(); // BitPantry.Command

            _registry.ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        [DataTestMethod]
        [DataRow("co", 1)] // start
        [DataRow("co", 2)] // end
        [DataRow("co", 3)] // very end
        [DataRow("Command", 2)] // exact match
        [DataRow("Command", 8)] // exact match with cursor at very end
        [DataRow("COMM", 3)] // all caps
        public async Task AutoCompleteCommandName_AutoCompleted(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.Options[0].Value.Should().Be("Command");
        }

        [DataTestMethod]
        [DataRow("xyz", 1)]
        [DataRow("abc", 2)]
        [DataRow("def", 3)]
        public async Task AutoCompleteNotExists_NoResult(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(0);
        }

        [DataTestMethod]
        [DataRow(5)]
        [DataRow(0)]
        [DataRow(-2)]
        public void AutoCompleteOffPostion_NoResult(int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var elem = new ParsedInput("co").GetElementAtCursorPosition(position);

            elem.Should().BeNull();
        }

        [TestMethod]
        public async Task AutoCompleteValidCommandNameWithoutNamespace_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithNamespace").GetElementAtCursorPosition(1));

            opt.Options.Should().HaveCount(1);
            opt.Options[0].Value.Should().Be("CommandWithNamespace");
            opt.Options[0].Format.Should().Be("BitPantry.{0}");
        }

        [DataTestMethod]
        [DataRow("BitPantry.CommandW", 5)] // partial
        [DataRow("BitPantry.CommandWithNamespace", 5)] // exact
        [DataRow("BITPANTRY.COMMANDW", 5)] // all caps
        [DataRow("BitPantry.CommandWithNamespace", 31)] // exact with cursor at very end
        public async Task AutoCompleteCommandWithNamespace_AutoCompleted(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);

            opt.Options[0].Value.Should().Be("CommandWithNamespace");
            opt.Options[0].GetFormattedValue().Should().Be("BitPantry.CommandWithNamespace");
        }

        [DataTestMethod]
        [DataRow("BitPantry.Comm", 5)] // partial
        [DataRow("BitPantry.Command", 5)] // exact
        [DataRow("BITPANTRY.COMMAND", 5)] // all caps
        public async Task AutoCompleteCommandWithNamespaceWithOptions_AutoCompleted(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(2);

            opt.Options[0].Value.Should().Be("Command");
            opt.Options[0].GetFormattedValue().Should().Be("BitPantry.Command");

            opt.Options[1].Value.Should().Be("CommandWithNamespace");
            opt.Options[1].GetFormattedValue().Should().Be("BitPantry.CommandWithNamespace");
        }

        [TestMethod]
        public async Task AutoCompleteNamespace_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("Bit").GetElementAtCursorPosition(1));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);

            opt.Options[0].Value.Should().Be("BitPantry");
            opt.Options[0].GetFormattedValue().Should().Be("BitPantry.");
        }

        [TestMethod]
        public async Task AutoCompleteFullNamespaceWithoutDot_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("BitPantry").GetElementAtCursorPosition(1));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);

            opt.Options[0].Value.Should().Be("BitPantry");
            opt.Options[0].GetFormattedValue().Should().Be("BitPantry.");
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        public void AutoCompleteEmptyString_NoResult(string emptyString)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var elem = new ParsedInput(emptyString).GetElementAtCursorPosition(1);
            elem.Should().BeNull();
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        public async Task AutoCompleteEmptyStringWithNamespace_NoResult(string emptyString)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput($"BitPantry.{emptyString}").GetElementAtCursorPosition(1));

            opt.Should().BeNull();
        }

        [TestMethod]
        public async Task AutoCompleteCommandWithNamespaceChangeOptions_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("BitPantry.com").GetElementAtCursorPosition(1));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(2);

            opt.CurrentOption.Should().Be(opt.Options[0]);

            opt.Options[0].Value.Should().Be("Command");
            opt.Options[0].GetFormattedValue().Should().Be("BitPantry.Command");

            opt.Options[1].Value.Should().Be("CommandWithNamespace");
            opt.Options[1].GetFormattedValue().Should().Be("BitPantry.CommandWithNamespace");
        }

        [TestMethod]
        public async Task AutoCompletePipedInput_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("command | my").GetElementAtCursorPosition(13));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);

            opt.Options[0].Value.Should().Be("myCommand");
        }

        [TestMethod]
        public async Task AutoCompleteFirstCommandPipedInput_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("com | my").GetElementAtCursorPosition(2));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.Options[0].Value.Should().Be("Command");
        }

    }
}
