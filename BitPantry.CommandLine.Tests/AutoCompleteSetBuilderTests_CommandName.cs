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
    /// <summary>
    /// Tests for root-level command name autocomplete functionality.
    /// Group-based autocomplete tests are in AutoCompleteSetBuilderTests_Groups.cs
    /// </summary>
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

            _registry.RegisterCommand<Command>(); // Command (root level)
            _registry.RegisterCommand<CommandWithNameAttribute>(); // myCommand (root level)
            _registry.RegisterCommand<MultipleArgumentsAndAliases>(); // MultipleArgumentsAndAliases (root level)
            _registry.RegisterCommand<CommandWithGroup>(); // bitpantry CommandWithGroup
            _registry.RegisterCommand<DupNameDifferentGroup>(); // bitpantry Command

            _registry.ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        #region Root Level Command Autocomplete

        [DataTestMethod]
        [DataRow("co", 1)] // start
        [DataRow("co", 2)] // end
        [DataRow("co", 3)] // very end
        [DataRow("Command", 2)] // exact match
        [DataRow("Command", 8)] // exact match with cursor at very end
        [DataRow("COMM", 3)] // all caps (case insensitive)
        public async Task AutoCompleteCommandName_AutoCompleted(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            // With group-based autocomplete, only root-level commands are shown at root level
            // CommandWithGroup and DupNameDifferentGroup are in the bitpantry group, so they're not shown here
            opt.Options.Should().HaveCount(1);
            opt.Options[0].Value.Should().Be("Command");
        }

        [DataTestMethod]
        [DataRow("my", 1)]
        [DataRow("myC", 2)]
        [DataRow("myCommand", 5)]
        public async Task AutoCompleteCommandWithCustomName_AutoCompleted(string query, int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput(query).GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().Contain(o => o.Value == "myCommand");
        }

        #endregion

        #region Invalid Position Cases

        [DataTestMethod]
        [DataRow(5)]
        [DataRow(0)]
        [DataRow(-2)]
        public void AutoCompleteOffPosition_NoResult(int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var elem = new ParsedInput("co").GetElementAtCursorPosition(position);

            elem.Should().BeNull();
        }

        #endregion

        #region Empty Input Cases

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        public void AutoCompleteEmptyString_NoResult(string emptyString)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var elem = new ParsedInput(emptyString).GetElementAtCursorPosition(1);
            elem.Should().BeNull();
        }

        #endregion

        #region Pipeline Autocomplete

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
            // With group-based autocomplete, only root-level commands are shown at root level
            opt.Options.Should().HaveCount(1);
            opt.Options[0].Value.Should().Be("Command");
        }

        #endregion
    }
}
