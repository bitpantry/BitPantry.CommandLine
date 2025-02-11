using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Processing.Parsing;
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
    public class AutoCompleteSetBuilderTests_Value
    {
        private static CommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            _registry = new CommandRegistry();

            _registry.RegisterCommand<CommandWithArgAc>(); // CommandWithArgAc arg1|a
            _registry.RegisterCommand<CommandWithTwoArgAc>(); // CommandWithTwoArgAc arg1|a arg2|b

            _registry.ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        

        [TestMethod]
        public async Task AutoCompleteArg1NoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = await ac.BuildOptions(new ParsedInput("CommandWithArgAc --arg1 ").GetElementAtCursorPosition(25));

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.Options[0].Value.Should().Be("Opt1");
            opt.Options[0].GetFormattedValue().Should().Be("Opt1");
        }

        [TestMethod]
        public void AutoCompleteArg1NoQueryAndSpace_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithArgAc --arg1   ").GetElementAtCursorPosition(27)).Result;

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.Options[0].Value.Should().Be("Opt1");
            opt.Options[0].GetFormattedValue().Should().Be("Opt1");
        }

        [TestMethod]
        public void AutoCompleteBadArgName_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithArgAc --arg ").GetElementAtCursorPosition(24)).Result;

            opt.Should().BeNull();
        }

        [TestMethod]
        public void AutoCompleteArg1WithQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithArgAc --arg1 b").GetElementAtCursorPosition(26)).Result;

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.CurrentOption.Value.Should().Be("Big2");
            opt.CurrentOption.GetFormattedValue().Should().Be("Big2");
        }

        [TestMethod]
        public void AutoCompleteArg1WithQueryMultipleOptions_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithArgAc --arg1 o").GetElementAtCursorPosition(26)).Result;

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.CurrentOption.Value.Should().Be("Opt1");
            opt.CurrentOption.GetFormattedValue().Should().Be("Opt1");
        }

        [TestMethod]
        public void AutoCompleteAliasNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithArgAc -a ").GetElementAtCursorPosition(21)).Result;

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.Options[0].Value.Should().Be("Opt1");
            opt.Options[0].GetFormattedValue().Should().Be("Opt1");
        }

        [TestMethod]
        public void AutoCompleteBadAlias_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithArgAc -aaa ").GetElementAtCursorPosition(22)).Result;

            opt.Should().BeNull();
        }

        [TestMethod]
        public void AutoCompleteFirstAliasNoQuery_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithTwoArgAc -a  -b").GetElementAtCursorPosition(24)).Result;

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.Options[0].Value.Should().Be("Opt1");
            opt.Options[0].GetFormattedValue().Should().Be("Opt1");
        }

        [TestMethod]
        public void AutoCompleteCtxFirstArgWithSecondValue_AutoCompleted()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithTwoArgAc -a  -b x").GetElementAtCursorPosition(24)).Result;

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.Options[0].Value.Should().Be("Opt1");
            opt.Options[0].GetFormattedValue().Should().Be("Opt1");

            opt.Options[1].Value.Should().Be("Big2");
            opt.Options[1].GetFormattedValue().Should().Be("Big2");

            opt.Options[2].Value.Should().Be("obc3");
            opt.Options[2].GetFormattedValue().Should().Be("obc3");
        }

        [TestMethod]
        public void AutoCompleteCtxFirstArgWithAllValues_NoResult()
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithTwoArgAc -a v -b x").GetElementAtCursorPosition(25)).Result;

            opt.Should().BeNull();
        }


        [DataTestMethod]
        [DataRow(24)]
        [DataRow(25)]
        [DataRow(26)]
        [DataRow(27)]
        public void AutoCompleteNoQuerySplitTrailingWhitespace_AutoCompleted(int cursorPosition)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithTwoArgAc -a     ").GetElementAtCursorPosition(cursorPosition)).Result;

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.Options[0].Value.Should().Be("Opt1");
            opt.Options[0].GetFormattedValue().Should().Be("Opt1");
        }

        [DataTestMethod]
        [DataRow(24)]
        [DataRow(25)]
        [DataRow(26)]
        [DataRow(27)]
        public void AutoCompleteFirstAliasNoQuerySplitWhitespace_AutoCompleted(int cursorPosition)
        {
            var ac = new AutoCompleteOptionSetBuilder(_registry, _serviceProvider);
            var opt = ac.BuildOptions(new ParsedInput("CommandWithTwoArgAc -a     -b").GetElementAtCursorPosition(cursorPosition)).Result;

            opt.Should().NotBeNull();
            opt.Options.Should().HaveCount(3);

            opt.Options[0].Value.Should().Be("Opt1");
            opt.Options[0].GetFormattedValue().Should().Be("Opt1");
        }

    }
}
