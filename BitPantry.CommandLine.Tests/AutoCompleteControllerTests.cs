using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using FluentAssertions;
using BitPantry.VirtualConsole.Testing;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

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
        private static IAutoCompleteHandlerRegistry _handlerRegistry;

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

            var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
            _handlerRegistry = handlerBuilder.Build(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            _input = new ConsoleLineMirror(_console);
            _acCtrl = new AutoCompleteController(new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry));
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
            var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerBuilder.Build(services);
            var sp = services.BuildServiceProvider();
            
            var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            var input = new ConsoleLineMirror(console);
            var acCtrl = new AutoCompleteController(new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

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

        #region Spec 008-autocomplete-extensions

        /// <summary>
        /// Implements: 008:TC-4.1
        /// End-to-end enum autocomplete works with default application.
        /// When user types "err" for an enum argument, autocomplete returns "Error".
        /// </summary>
        [TestMethod]
        public async Task EnumArgument_WithPrefix_AutocompletesMatchingValue()
        {
            // Arrange - set up a fresh registry with enum command
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<CommandWithEnumArg>(); // CommandWithEnumArg --Level
            var registry = builder.Build(services);

            var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerBuilder.Build(services);

            var sp = services.BuildServiceProvider();

            var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            var input = new ConsoleLineMirror(console);
            var acCtrl = new AutoCompleteController(
                new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

            // User types command with partial enum value
            input.Write("CommandWithEnumArg --Level err");

            // Act - trigger autocomplete
            await acCtrl.Begin(input);

            // Assert - enum value "Error" should be autocompleted
            // Buffer: "CommandWithEnumArg --Level Error" = 32 characters
            input.Buffer.Should().Be("CommandWithEnumArg --Level Error");
            input.BufferPosition.Should().Be(32);
        }

        /// <summary>
        /// Implements: 008:TC-4.2
        /// Custom Type Handler overrides built-in when registered after.
        /// The last-registered handler wins for matching types.
        /// </summary>
        [TestMethod]
        public async Task CustomTypeHandler_RegisteredAfterBuiltIn_OverridesBuiltIn()
        {
            // Arrange - set up registry with enum command
            var services = new ServiceCollection();
            var cmdBuilder = new CommandRegistryBuilder();
            cmdBuilder.RegisterCommand<CommandWithEnumArg>(); // CommandWithEnumArg --Level
            var cmdRegistry = cmdBuilder.Build(services);

            // Register custom handler AFTER built-ins (it should override EnumAutoCompleteHandler)
            var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
            handlerBuilder.Register<CustomLogLevelHandler>(); // This returns "CUSTOM_VALUE" instead of enum values
            var handlerRegistry = handlerBuilder.Build(services);

            var sp = services.BuildServiceProvider();

            var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            var input = new ConsoleLineMirror(console);
            var acCtrl = new AutoCompleteController(
                new AutoCompleteOptionSetBuilder(cmdRegistry, new NoopServerProxy(), sp, handlerRegistry));

            // User types command with partial value
            input.Write("CommandWithEnumArg --Level ");

            // Act - trigger autocomplete
            await acCtrl.Begin(input);

            // Assert - custom handler's value should be used, NOT the built-in enum values
            input.Buffer.Should().Be("CommandWithEnumArg --Level CUSTOM_VALUE");
        }

        /// <summary>
        /// Implements: 008:TC-4.3
        /// Attribute Handler used even when matching Type Handler exists.
        /// Attribute takes precedence over type-matched handlers.
        /// </summary>
        [TestMethod]
        public async Task AttributeHandler_TakesPrecedenceOverTypeHandler()
        {
            // Arrange - set up registry with command that has [AutoComplete<...>] attribute
            var services = new ServiceCollection();
            var cmdBuilder = new CommandRegistryBuilder();
            cmdBuilder.RegisterCommand<CommandWithAttributeHandler>(); // Has [AutoComplete<AttributeSpecifiedHandler>]
            var cmdRegistry = cmdBuilder.Build(services);

            // Default handler registry includes EnumAutoCompleteHandler (which would match TestLogLevel)
            var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerBuilder.Build(services);

            // Register the attribute-specified handler with DI
            services.AddTransient<AttributeSpecifiedHandler>();

            var sp = services.BuildServiceProvider();

            var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            var input = new ConsoleLineMirror(console);
            var acCtrl = new AutoCompleteController(
                new AutoCompleteOptionSetBuilder(cmdRegistry, new NoopServerProxy(), sp, handlerRegistry));

            // User types command
            input.Write("CommandWithAttributeHandler --Level ");

            // Act - trigger autocomplete
            await acCtrl.Begin(input);

            // Assert - attribute handler's value should be used, NOT the built-in enum values
            input.Buffer.Should().Be("CommandWithAttributeHandler --Level ATTRIBUTE_VALUE");
        }

        /// <summary>
        /// Implements: 008:TC-4.4
        /// Handler receives ProvidedValues in context with already-entered values.
        /// </summary>
        [TestMethod]
        public async Task Handler_ReceivesProvidedValuesInContext()
        {
            // Arrange - set up registry with multi-argument command
            var services = new ServiceCollection();
            var cmdBuilder = new CommandRegistryBuilder();
            cmdBuilder.RegisterCommand<CommandWithMultipleArgs>(); // Has --First and --Second
            var cmdRegistry = cmdBuilder.Build(services);

            // Register handler that captures context
            var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerBuilder.Build(services);

            // Create and register the capturing handler
            var capturingHandler = new ContextCapturingHandler();
            services.AddSingleton<ContextCapturingHandler>(capturingHandler);

            var sp = services.BuildServiceProvider();

            var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            var input = new ConsoleLineMirror(console);
            var acCtrl = new AutoCompleteController(
                new AutoCompleteOptionSetBuilder(cmdRegistry, new NoopServerProxy(), sp, handlerRegistry));

            // User types command with --First already entered, now completing --Second
            input.Write("CommandWithMultipleArgs --First existingValue --Second ");

            // Act - trigger autocomplete
            await acCtrl.Begin(input);

            // Assert - handler should have received the First argument value in ProvidedValues
            capturingHandler.LastContext.Should().NotBeNull();
            capturingHandler.LastContext!.ProvidedValues.Should().NotBeEmpty();
            
            // Find the First argument and verify its value was captured
            var firstArg = capturingHandler.LastContext.ProvidedValues
                .FirstOrDefault(kv => kv.Key.Name == "First");
            firstArg.Key.Should().NotBeNull("ProvidedValues should contain the 'First' argument");
            firstArg.Value.Should().Be("existingValue");
        }

        /// <summary>
        /// Implements: 008:TC-4.5
        /// Boolean autocomplete works end-to-end.
        /// When user types "f" for a bool argument, autocomplete returns "false".
        /// </summary>
        [TestMethod]
        public async Task BooleanArgument_WithPrefix_AutocompletesMatchingValue()
        {
            // Arrange - set up a fresh registry with bool command
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<CommandWithBoolArg>(); // CommandWithBoolArg --Verbose
            var registry = builder.Build(services);

            var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerBuilder.Build(services);

            var sp = services.BuildServiceProvider();

            var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            var input = new ConsoleLineMirror(console);
            var acCtrl = new AutoCompleteController(
                new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

            // User types command with partial bool value
            input.Write("CommandWithBoolArg --Verbose f");

            // Act - trigger autocomplete
            await acCtrl.Begin(input);

            // Assert - bool value "false" should be autocompleted
            input.Buffer.Should().Be("CommandWithBoolArg --Verbose false");
        }

        /// <summary>
        /// Implements: 008:TC-4.6
        /// Nullable enum autocomplete works end-to-end.
        /// When user types a prefix for a nullable enum argument, autocomplete returns matching enum values.
        /// </summary>
        [TestMethod]
        public async Task NullableEnumArgument_WithPrefix_AutocompletesMatchingValue()
        {
            // Arrange - set up a fresh registry with nullable enum command
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<CommandWithNullableEnumArg>(); // CommandWithNullableEnumArg --Level
            var registry = builder.Build(services);

            var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerBuilder.Build(services);

            var sp = services.BuildServiceProvider();

            var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
            var input = new ConsoleLineMirror(console);
            var acCtrl = new AutoCompleteController(
                new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

            // User types command with partial enum value (nullable TestLogLevel?)
            input.Write("CommandWithNullableEnumArg --Level err");

            // Act - trigger autocomplete
            await acCtrl.Begin(input);

            // Assert - enum value "Error" should be autocompleted (unwrapped from nullable)
            input.Buffer.Should().Be("CommandWithNullableEnumArg --Level Error");
        }

        #endregion

        #region Test Helpers

        /// <summary>
        /// Custom handler for TestLogLevel that returns a distinctive value
        /// to verify it overrides the built-in EnumAutoCompleteHandler.
        /// </summary>
        private class CustomLogLevelHandler : ITypeAutoCompleteHandler
        {
            public bool CanHandle(Type argumentType)
            {
                // Handle TestLogLevel (same type as built-in EnumAutoCompleteHandler would match)
                return argumentType == typeof(TestLogLevel);
            }

            public Task<List<AutoCompleteOption>> GetOptionsAsync(
                AutoCompleteContext context,
                CancellationToken cancellationToken = default)
            {
                // Return a distinctive value to prove this handler was used
                var options = new List<AutoCompleteOption>
                {
                    new AutoCompleteOption("CUSTOM_VALUE")
                };
                return Task.FromResult(options);
            }
        }

        /// <summary>
        /// Handler specified via [AutoComplete<AttributeSpecifiedHandler>] attribute.
        /// Returns a distinctive value to prove attribute precedence.
        /// </summary>
        private class AttributeSpecifiedHandler : IAutoCompleteHandler
        {
            public Task<List<AutoCompleteOption>> GetOptionsAsync(
                AutoCompleteContext context,
                CancellationToken cancellationToken = default)
            {
                var options = new List<AutoCompleteOption>
                {
                    new AutoCompleteOption("ATTRIBUTE_VALUE")
                };
                return Task.FromResult(options);
            }
        }

        /// <summary>
        /// Command with [AutoComplete<>] attribute on enum argument.
        /// Even though TestLogLevel is an enum (matched by EnumAutoCompleteHandler),
        /// the attribute handler should take precedence.
        /// </summary>
        [Command]
        private class CommandWithAttributeHandler : CommandBase
        {
            [Argument]
            [AutoComplete<AttributeSpecifiedHandler>]
            public TestLogLevel Level { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with multiple string arguments for testing ProvidedValues context.
        /// </summary>
        [Command]
        private class CommandWithMultipleArgs : CommandBase
        {
            [Argument]
            public string First { get; set; }

            [Argument]
            [AutoComplete<ContextCapturingHandler>]
            public string Second { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Handler that captures the context for verification.
        /// </summary>
        private class ContextCapturingHandler : IAutoCompleteHandler
        {
            public AutoCompleteContext? LastContext { get; private set; }

            public Task<List<AutoCompleteOption>> GetOptionsAsync(
                AutoCompleteContext context,
                CancellationToken cancellationToken = default)
            {
                LastContext = context;
                var options = new List<AutoCompleteOption>
                {
                    new AutoCompleteOption("CAPTURED")
                };
                return Task.FromResult(options);
            }
        }

        /// <summary>
        /// Command with a boolean argument for testing BooleanAutoCompleteHandler.
        /// </summary>
        [Command]
        private class CommandWithBoolArg : CommandBase
        {
            [Argument]
            public bool Verbose { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with a nullable enum argument for testing nullable enum unwrapping.
        /// </summary>
        [Command]
        private class CommandWithNullableEnumArg : CommandBase
        {
            [Argument]
            public TestLogLevel? Level { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion
    }
}
