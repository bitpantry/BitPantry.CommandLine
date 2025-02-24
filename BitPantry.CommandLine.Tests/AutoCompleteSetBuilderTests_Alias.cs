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
    public class AutoCompleteSetBuilderTests_Alias
    {
        private static CommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            _registry = new CommandRegistry();

            _registry.RegisterCommand<CommandWithArg>(); // CommandWithArg arg1|a
            _registry.RegisterCommand<CommandWithTwoArgs>(); // CommandWithTwoArgs arg1|a xyzq|x

            _registry.ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        [TestMethod]
        public async Task AutoCompleteOneNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithArg -").GetElementAtCursorPosition(17));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);
            opt.Options[0].Value.Should().Be("a");
            opt.Options[0].GetFormattedValue().Should().Be("-a");
        }

        [TestMethod]
        public async Task AutoCompleteOneArgUpperCaseQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithArg -A").GetElementAtCursorPosition(17));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);
            opt.Options[0].Value.Should().Be("a");
            opt.Options[0].GetFormattedValue().Should().Be("-a");
        }

        [TestMethod]
        public async Task AutoCompleteOneArgBadQuery_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithArg -c").GetElementAtCursorPosition(17));

            opt.Should().BeNull();
        }

        [TestMethod]
        public async Task AutoCompleteFirstAlias_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithTwoArgs -").GetElementAtCursorPosition(21));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(2);
            opt.Options[0].Value.Should().Be("a");
            opt.Options[0].GetFormattedValue().Should().Be("-a");
        }

        [TestMethod]
        public async Task AutoCompleteFirstAliasSecondHasValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithTwoArgs - -x").GetElementAtCursorPosition(21));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);
            opt.Options[0].Value.Should().Be("a");
            opt.Options[0].GetFormattedValue().Should().Be("-a");
        }

        [TestMethod]
        public async Task AutoCompleteFirstAliasSecondHasBadValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithTwoArgs - -y").GetElementAtCursorPosition(21));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(2);
            opt.Options[0].Value.Should().Be("a");
            opt.Options[0].GetFormattedValue().Should().Be("-a");
        }
    }
}
