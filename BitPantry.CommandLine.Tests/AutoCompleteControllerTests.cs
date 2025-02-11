using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using BitPantry.CommandLine.Tests.Service;
using BitPantry.CommandLine.Tests.VirtualConsole;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Remote;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class AutoCompleteControllerTests
    {
        //private readonly ConsoleKeyInfo TAB = new(' ', ConsoleKey.Tab, false, false, false);
        //private readonly ConsoleKeyInfo SHIFT_TAB = new(' ', ConsoleKey.Tab, true, false, false);
        //private readonly ConsoleKeyInfo ENTER = new(' ', ConsoleKey.Enter, false, false, false);
        //private readonly ConsoleKeyInfo ESC = new(' ', ConsoleKey.Escape, false, false, false);

        private static CommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        private static VirtualAnsiConsole _console;
        private static ConsoleLineMirror _input;
        private static AutoCompleteController _acCtrl;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            _registry = new CommandRegistry();

            _registry.RegisterCommand<Command>(); // Command
            _registry.RegisterCommand<CommandWithNameAttribute>(); // myCommand
            _registry.RegisterCommand<CommandWithNamespace>(); // BitPantry.CommandWithNamespace
            _registry.RegisterCommand<DupNameDifferentNamespace>(); // BitPantry.Command
            _registry.RegisterCommand<CommandWithTwoArgs>(); // CommandWithTwoArgs --Arg1|a --XyzQp|x

            _registry.ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _console = new VirtualAnsiConsole();
            _input = new ConsoleLineMirror(_console);
            _acCtrl = new AutoCompleteController(new AutoCompleteOptionSetBuilder(_registry, _serviceProvider));
        }

        [TestMethod]
        public async Task CommandName_Success()
        {
            _input.Write("com");

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("Command");
            _input.BufferPosition.Should().Be(7);
        }

        [TestMethod]
        public async Task CommandNameNavigateOptions_Success()
        {
            _input.Write("com");

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("Command");
            _input.BufferPosition.Should().Be(7);

            _acCtrl.NextOption(_input);

            _input.Buffer.Should().Be("CommandWithTwoArgs");
            _input.BufferPosition.Should().Be(18);

            _acCtrl.NextOption(_input);

            _input.Buffer.Should().Be("Command");
            _input.BufferPosition.Should().Be(7);

            _acCtrl.PreviousOption(_input);

            _input.Buffer.Should().Be("CommandWithTwoArgs");
            _input.BufferPosition.Should().Be(18);
        }

        [TestMethod]
        public async Task ArgNameNoQuery_Success()
        {
            _input.Write("commandWithTwoArgs --");

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
            _input.BufferPosition.Should().Be(25);
        }

        [TestMethod]
        public async Task ArgNameNoQueryNavigation_Success()
        {
            _input.Write("commandWithTwoArgs --");

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
            _input.BufferPosition.Should().Be(25);

            _acCtrl.NextOption(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --XyzQp");
            _input.BufferPosition.Should().Be(26);

            _acCtrl.PreviousOption(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
            _input.BufferPosition.Should().Be(25);
        }

        [TestMethod]
        public async Task ArgNameWithQuery_Success()
        {
            _input.Write("commandWithTwoArgs --a");

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
            _input.BufferPosition.Should().Be(25);
        }

        [TestMethod]
        public async Task FirstArgName_Success()
        {
            _input.Write("commandWithTwoArgs -- --arg1");
            _input.MoveToPosition(21);

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --XyzQp --arg1");
            _input.BufferPosition.Should().Be(26);
        }

        [TestMethod]
        public async Task FirstArgNameSecondIsBad_Success()
        {
            _input.Write("commandWithTwoArgs -- --george");
            _input.MoveToPosition(21);

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --Arg1 --george");
            _input.BufferPosition.Should().Be(25);

            _acCtrl.NextOption(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --XyzQp --george");
            _input.BufferPosition.Should().Be(26);

            _acCtrl.NextOption(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --Arg1 --george");
            _input.BufferPosition.Should().Be(25);
        }

        [TestMethod]
        public async Task FirstArgNameWithAlias_Success()
        {
            _input.Write("commandWithTwoArgs -- -a");
            _input.MoveToPosition(21);

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --XyzQp -a");
            _input.BufferPosition.Should().Be(26);
        }

        [TestMethod]
        public async Task FirstAliasNameWithArgumentName_Success()
        {
            _input.Write("commandWithTwoArgs - --arg1");
            _input.MoveToPosition(20);

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs -x --arg1");
            _input.BufferPosition.Should().Be(21);
        }

        [TestMethod]
        public async Task EndAutoComplete_Success()
        {
            _input.Write("commandWithTwoArgs - --arg1");
            _input.MoveToPosition(20);

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs -x --arg1");
            _input.BufferPosition.Should().Be(21);

            _acCtrl.NextOption(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs -x --arg1");
            _input.BufferPosition.Should().Be(21);
        }

        [TestMethod]
        public async Task CancelAutoComplete_Success()
        {
            _input.Write("commandWithTwoArgs --a");

            await _acCtrl.Begin(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
            _input.BufferPosition.Should().Be(25);

            _acCtrl.Cancel(_input);

            _input.Buffer.Should().Be("commandWithTwoArgs --a");
            _input.BufferPosition.Should().Be(22);
        }
    }
}
