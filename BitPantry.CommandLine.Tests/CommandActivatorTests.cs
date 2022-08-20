using BitPantry.CommandLine.Processing;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.CommandLine.Tests.Commands.ActivateCommands;
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
    public class CommandActivatorTests
    {
        private static CommandActivator _activator;
        private static CommandResolver _resolver;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var registry = new CommandRegistry();

            registry.RegisterCommand<Command>();
            registry.RegisterCommand<WithArgument>();
            registry.RegisterCommand<WithIntArg>();
            registry.RegisterCommand<MultipleArgs>();
            registry.RegisterCommand<WithAlias>();
            registry.RegisterCommand<WithOption>();

            _resolver = new CommandResolver(registry);
            _activator = new CommandActivator();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_activator != null)
                _activator.Dispose();
        }

        [TestMethod]
        public void ActivateCommand_Activated()
        {
            var input = new ParsedInput("command");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<Command>();
        }

        [TestMethod]
        public void ActivateWithoutArgInput_Activated()
        {
            var input = new ParsedInput("withArgument");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithArgument>();
            ((WithArgument)act.Command).ArgOne.Should().Be(0);
        }

        [TestMethod]
        public void ActivateIntArg_Activated()
        {
            var input = new ParsedInput("withIntArg --intArg 10");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithIntArg>();
            ((WithIntArg)act.Command).IntArg.Should().Be(10);
        }

        [TestMethod]
        public void ActivateMultipleArgs_Activated()
        {
            var input = new ParsedInput("MultipleArgs --argOne 10 --strArg \"hello world\"");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<MultipleArgs>();
            ((MultipleArgs)act.Command).ArgOne.Should().Be(10);
            ((MultipleArgs)act.Command).StrArg.Should().Be("hello world");
        }

        [TestMethod]
        public void ActivateAlias_Activated()
        {
            var input = new ParsedInput("withAlias -a 10");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithAlias>();
            ((WithAlias)act.Command).ArgOne.Should().Be(10);
        }

        [TestMethod]
        public void ActivateOption_Activated()
        {
            var input = new ParsedInput("withOption --optOne");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithOption>();
            ((WithOption)act.Command).OptOne.IsPresent.Should().BeTrue();
        }

        [TestMethod]
        public void ActivateOptionAbsent_Activated()
        {
            var input = new ParsedInput("withOption");
            var res = _resolver.Resolve(input);

            var act = _activator.Activate(res);

            act.Command.Should().NotBeNull();
            act.Command.GetType().Should().Be<WithOption>();
            ((WithOption)act.Command).OptOne.IsPresent.Should().BeFalse();
        }


    }
}
