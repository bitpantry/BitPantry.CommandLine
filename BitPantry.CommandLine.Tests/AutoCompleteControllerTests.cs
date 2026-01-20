using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using FluentAssertions;
using BitPantry.VirtualConsole.Testing;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Client;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class AutoCompleteControllerTests
    {
        //private readonly ConsoleKeyInfo TAB = new(' ', ConsoleKey.Tab, false, false, false);
        //private readonly ConsoleKeyInfo SHIFT_TAB = new(' ', ConsoleKey.Tab, true, false, false);
        //private readonly ConsoleKeyInfo ENTER = new(' ', ConsoleKey.Enter, false, false, false);
        //private readonly ConsoleKeyInfo ESC = new(' ', ConsoleKey.Escape, false, false, false);

        private static ICommandRegistry _registry;
        private static ServiceProvider _serviceProvider;

        private static VirtualConsoleAnsiAdapter _console;
        private static ConsoleLineMirror _input;
        private static AutoCompleteController _acCtrl;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var services = new ServiceCollection();

            var builder = new CommandRegistryBuilder();

            builder.RegisterCommand<Command>(); // Command
            builder.RegisterCommand<CommandWithNameAttribute>(); // myCommand
            builder.RegisterCommand<CommandWithGroup>(); // bitpantry.CommandWithGroup
            builder.RegisterCommand<DupNameDifferentGroup>(); // bitpantry.Command
            builder.RegisterCommand<CommandWithTwoArgs>(); // CommandWithTwoArgs --Arg1|a --XyzQp|x

            _registry = builder.Build(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            _input = new ConsoleLineMirror(_console);
            _acCtrl = new AutoCompleteController(new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider));
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
            // Register additional root-level commands for navigation testing
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<Command>(); // Command
            builder.RegisterCommand<CommandWithTwoArgs>(); // CommandWithTwoArgs
            var registry = builder.Build(services);
            var sp = services.BuildServiceProvider();
            
            var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            var input = new ConsoleLineMirror(console);
            var acCtrl = new AutoCompleteController(new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp));

            input.Write("com");

            await acCtrl.Begin(input);

            input.Buffer.Should().Be("Command");
            input.BufferPosition.Should().Be(7);

            acCtrl.NextOption(input);

            input.Buffer.Should().Be("CommandWithTwoArgs");
            input.BufferPosition.Should().Be(18);

            acCtrl.NextOption(input);

            // Should cycle back to first option
            input.Buffer.Should().Be("Command");
            input.BufferPosition.Should().Be(7);

            acCtrl.PreviousOption(input);

            // Should go to last option
            input.Buffer.Should().Be("CommandWithTwoArgs");
            input.BufferPosition.Should().Be(18);
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
            // When the cursor is on bare "--", it's treated as incomplete argument input for autocomplete.
            // After "--", values like "--arg1" are positional (not argument names), so Arg1 is still available.
            _input.Write("commandWithTwoArgs -- --arg1");
            _input.MoveToPosition(21);

            await _acCtrl.Begin(_input);

            // Arg1 comes before XyzQp alphabetically
            _input.Buffer.Should().Be("commandWithTwoArgs --Arg1 --arg1");
            _input.BufferPosition.Should().Be(25);
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
            // When the cursor is on bare "--", it's treated as incomplete argument input for autocomplete.
            // After "--", "-a" is a positional value, so both Arg1 and XyzQp are available for suggestion.
            _input.Write("commandWithTwoArgs -- -a");
            _input.MoveToPosition(21);

            await _acCtrl.Begin(_input);

            // Arg1 comes before XyzQp alphabetically
            _input.Buffer.Should().Be("commandWithTwoArgs --Arg1 -a");
            _input.BufferPosition.Should().Be(25);
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
