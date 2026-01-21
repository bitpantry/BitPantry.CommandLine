using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
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
    public class AutoCompleteSetBuilderTests_ArgName
    {
        private static ICommandRegistry _registry;
        private static ServiceProvider _serviceProvider;
        private static IAutoCompleteHandlerRegistry _handlerRegistry;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            var builder = new CommandRegistryBuilder();

            builder.RegisterCommand<Command>(); // Command
            builder.RegisterCommand<MultipleArgumentsAndAliases>(); // MultipleArgumentsAndAliases propertyTwo|p prop|X
            builder.RegisterCommand<CommandWithArg>(); // CommandWithArgNoAc arg1
            builder.RegisterCommand<CommandWithTwoArgs>(); // CommandWithTwoArgs arg1 xyzq

            _registry = builder.Build(services);

            var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
            _handlerRegistry = handlerBuilder.Build(services);

            _serviceProvider = services.BuildServiceProvider();
        }



        [TestMethod]
        public async Task AutoCompleteNoArgs_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput("Command --").GetElementAtCursorPosition(11));

            opt.Should().BeNull();
        }

        [TestMethod]
        public async Task AutoCompleteOneArgNonMatchingQuery_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithArg --m").GetElementAtCursorPosition(19));

            opt.Should().BeNull();
        }

        [TestMethod]
        public async Task AutoCompleteOneArgNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithArg --").GetElementAtCursorPosition(18));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);

            opt.Options[0].Value.Should().Be("Arg1");
            opt.Options[0].GetFormattedValue().Should().Be("--Arg1");
        }

        [DataTestMethod]
        [DataRow(16)]
        [DataRow(17)]
        [DataRow(18)]
        public async Task AutoCompleteOneArgNoQueryDifferentPos_AutoCompleted(int position)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithArg --").GetElementAtCursorPosition(position));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);

            opt.Options[0].Value.Should().Be("Arg1");
            opt.Options[0].GetFormattedValue().Should().Be("--Arg1");
        }

        [TestMethod]
        public async Task AutoCompleteCmdNameOneArgNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWith --").GetElementAtCursorPosition(2));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(2);

            opt.Options[0].Value.Should().Be("CommandWithArg");
            opt.Options[0].GetFormattedValue().Should().Be("CommandWithArg");

            opt.Options.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task AutoCompleteArgNameNoCommand_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput(" --").GetElementAtCursorPosition(2));

            opt.Should().BeNull();
        }

        [TestMethod]
        public async Task AutoCompleteFirstArgument_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithTwoArgs --").GetElementAtCursorPosition(22));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(2);

            opt.Options[0].Value.Should().Be("Arg1");
            opt.Options[0].GetFormattedValue().Should().Be("--Arg1");
        }

        [TestMethod]
        public async Task AutoCompleteSecondArgumentFirstHasValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithTwoArgs --arg1 --").GetElementAtCursorPosition(29));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(1);

            opt.Options[0].Value.Should().Be("XyzQp");
            opt.Options[0].GetFormattedValue().Should().Be("--XyzQp");
        }

        [TestMethod]
        public async Task AutoCompleteSecondArgumentFirstHasBadValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithTwoArgs --arg9 --").GetElementAtCursorPosition(29));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(2);

            opt.Options[0].Value.Should().Be("Arg1");
            opt.Options[0].GetFormattedValue().Should().Be("--Arg1");
        }

        [TestMethod]
        public async Task AutoCompleteFirstArgumentFirstHasNoValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithTwoArgs --arg --").GetElementAtCursorPosition(25));


            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(2);

            opt.Options[0].Value.Should().Be("Arg1");
            opt.Options[0].GetFormattedValue().Should().Be("--Arg1");
        }

    }
}
