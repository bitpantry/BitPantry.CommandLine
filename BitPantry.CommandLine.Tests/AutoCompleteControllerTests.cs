// TODO - Delete this old stuff

// using BitPantry.CommandLine.AutoComplete;
// using BitPantry.CommandLine.AutoComplete.Handlers;
// using BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using System.Threading.Tasks;
// using FluentAssertions;
// using BitPantry.VirtualConsole.Testing;
// using BitPantry.CommandLine.Input;
// using BitPantry.CommandLine.Client;
// using BitPantry.CommandLine.API;
// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Linq;

// namespace BitPantry.CommandLine.Tests
// {
//     [TestClass]
//     public class AutoCompleteControllerTests
//     {
//         //private readonly ConsoleKeyInfo TAB = new(' ', ConsoleKey.Tab, false, false, false);
//         //private readonly ConsoleKeyInfo SHIFT_TAB = new(' ', ConsoleKey.Tab, true, false, false);
//         //private readonly ConsoleKeyInfo ENTER = new(' ', ConsoleKey.Enter, false, false, false);
//         //private readonly ConsoleKeyInfo ESC = new(' ', ConsoleKey.Escape, false, false, false);

//         private static ICommandRegistry _registry;
//         private static ServiceProvider _serviceProvider;
//         private static IAutoCompleteHandlerRegistry _handlerRegistry;

//         private static VirtualConsoleAnsiAdapter _console;
//         private static ConsoleLineMirror _input;
//         private static AutoCompleteController _acCtrl;

//         [ClassInitialize]
//         public static void Initialize(TestContext ctx)
//         {
//             var services = new ServiceCollection();

//             var builder = new CommandRegistryBuilder();

//             builder.RegisterCommand<Command>(); // Command
//             builder.RegisterCommand<CommandWithNameAttribute>(); // myCommand
//             builder.RegisterCommand<CommandWithGroup>(); // bitpantry.CommandWithGroup
//             builder.RegisterCommand<DupNameDifferentGroup>(); // bitpantry.Command
//             builder.RegisterCommand<CommandWithTwoArgs>(); // CommandWithTwoArgs --Arg1|a --XyzQp|x

//             _registry = builder.Build(services);

//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             _handlerRegistry = handlerBuilder.Build(services);

//             _serviceProvider = services.BuildServiceProvider();
//         }

//         [TestInitialize]
//         public void TestInitialize()
//         {
//             _console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             _input = new ConsoleLineMirror(_console);
//             _acCtrl = new AutoCompleteController(new AutoCompleteOptionSetBuilder(_registry, new NoopServerProxy(), _serviceProvider, _handlerRegistry));
//         }

//         [TestMethod]
//         public async Task CommandName_Success()
//         {
//             _input.Write("com");

//             await _acCtrl.Begin(_input);

//             _input.Buffer.Should().Be("Command");
//             _input.BufferPosition.Should().Be(7);
//         }

//         [TestMethod]
//         public async Task CommandNameNavigateOptions_Success()
//         {
//             // Register additional root-level commands for navigation testing
//             var services = new ServiceCollection();
//             var builder = new CommandRegistryBuilder();
//             builder.RegisterCommand<Command>(); // Command
//             builder.RegisterCommand<CommandWithTwoArgs>(); // CommandWithTwoArgs
//             var registry = builder.Build(services);
//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             var handlerRegistry = handlerBuilder.Build(services);
//             var sp = services.BuildServiceProvider();
            
//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

//             input.Write("com");

//             await acCtrl.Begin(input);

//             input.Buffer.Should().Be("Command");
//             input.BufferPosition.Should().Be(7);

//             acCtrl.NextOption(input);

//             input.Buffer.Should().Be("CommandWithTwoArgs");
//             input.BufferPosition.Should().Be(18);

//             acCtrl.NextOption(input);

//             // Should cycle back to first option
//             input.Buffer.Should().Be("Command");
//             input.BufferPosition.Should().Be(7);

//             acCtrl.PreviousOption(input);

//             // Should go to last option
//             input.Buffer.Should().Be("CommandWithTwoArgs");
//             input.BufferPosition.Should().Be(18);
//         }

//         [TestMethod]
//         public async Task ArgNameNoQuery_Success()
//         {
//             _input.Write("commandWithTwoArgs --");

//             await _acCtrl.Begin(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
//             _input.BufferPosition.Should().Be(25);
//         }

//         [TestMethod]
//         public async Task ArgNameNoQueryNavigation_Success()
//         {
//             _input.Write("commandWithTwoArgs --");

//             await _acCtrl.Begin(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
//             _input.BufferPosition.Should().Be(25);

//             _acCtrl.NextOption(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --XyzQp");
//             _input.BufferPosition.Should().Be(26);

//             _acCtrl.PreviousOption(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
//             _input.BufferPosition.Should().Be(25);
//         }

//         [TestMethod]
//         public async Task ArgNameWithQuery_Success()
//         {
//             _input.Write("commandWithTwoArgs --a");

//             await _acCtrl.Begin(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
//             _input.BufferPosition.Should().Be(25);
//         }

//         [TestMethod]
//         public async Task FirstArgName_Success()
//         {
//             // When the cursor is on bare "--", it's treated as incomplete argument input for autocomplete.
//             // After "--", values like "--arg1" are positional (not argument names), so Arg1 is still available.
//             _input.Write("commandWithTwoArgs -- --arg1");
//             _input.MoveToPosition(21);

//             await _acCtrl.Begin(_input);

//             // Arg1 comes before XyzQp alphabetically
//             _input.Buffer.Should().Be("commandWithTwoArgs --Arg1 --arg1");
//             _input.BufferPosition.Should().Be(25);
//         }

//         [TestMethod]
//         public async Task FirstArgNameSecondIsBad_Success()
//         {
//             _input.Write("commandWithTwoArgs -- --george");
//             _input.MoveToPosition(21);

//             await _acCtrl.Begin(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --Arg1 --george");
//             _input.BufferPosition.Should().Be(25);

//             _acCtrl.NextOption(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --XyzQp --george");
//             _input.BufferPosition.Should().Be(26);

//             _acCtrl.NextOption(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --Arg1 --george");
//             _input.BufferPosition.Should().Be(25);
//         }

//         [TestMethod]
//         public async Task FirstArgNameWithAlias_Success()
//         {
//             // When the cursor is on bare "--", it's treated as incomplete argument input for autocomplete.
//             // After "--", "-a" is a positional value, so both Arg1 and XyzQp are available for suggestion.
//             _input.Write("commandWithTwoArgs -- -a");
//             _input.MoveToPosition(21);

//             await _acCtrl.Begin(_input);

//             // Arg1 comes before XyzQp alphabetically
//             _input.Buffer.Should().Be("commandWithTwoArgs --Arg1 -a");
//             _input.BufferPosition.Should().Be(25);
//         }

//         [TestMethod]
//         public async Task FirstAliasNameWithArgumentName_Success()
//         {
//             _input.Write("commandWithTwoArgs - --arg1");
//             _input.MoveToPosition(20);

//             await _acCtrl.Begin(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs -x --arg1");
//             _input.BufferPosition.Should().Be(21);
//         }

//         [TestMethod]
//         public async Task EndAutoComplete_Success()
//         {
//             _input.Write("commandWithTwoArgs - --arg1");
//             _input.MoveToPosition(20);

//             await _acCtrl.Begin(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs -x --arg1");
//             _input.BufferPosition.Should().Be(21);

//             _acCtrl.NextOption(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs -x --arg1");
//             _input.BufferPosition.Should().Be(21);
//         }

//         [TestMethod]
//         public async Task CancelAutoComplete_Success()
//         {
//             _input.Write("commandWithTwoArgs --a");

//             await _acCtrl.Begin(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --Arg1");
//             _input.BufferPosition.Should().Be(25);

//             _acCtrl.Cancel(_input);

//             _input.Buffer.Should().Be("commandWithTwoArgs --a");
//             _input.BufferPosition.Should().Be(22);
//         }

//         #region Spec 008-autocomplete-extensions

//         /// <summary>
//         /// Implements: 008:TC-4.1
//         /// End-to-end enum autocomplete works with default application.
//         /// When user types "err" for an enum argument, autocomplete returns "Error".
//         /// </summary>
//         [TestMethod]
//         public async Task EnumArgument_WithPrefix_AutocompletesMatchingValue()
//         {
//             // Arrange - set up a fresh registry with enum command
//             var services = new ServiceCollection();
//             var builder = new CommandRegistryBuilder();
//             builder.RegisterCommand<CommandWithEnumArg>(); // CommandWithEnumArg --Level
//             var registry = builder.Build(services);

//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             var handlerRegistry = handlerBuilder.Build(services);

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types command with partial enum value
//             input.Write("CommandWithEnumArg --Level err");

//             // Act - trigger autocomplete
//             await acCtrl.Begin(input);

//             // Assert - enum value "Error" should be autocompleted
//             // Buffer: "CommandWithEnumArg --Level Error" = 32 characters
//             input.Buffer.Should().Be("CommandWithEnumArg --Level Error");
//             input.BufferPosition.Should().Be(32);
//         }

//         /// <summary>
//         /// Implements: 008:TC-4.2
//         /// Custom Type Handler overrides built-in when registered after.
//         /// The last-registered handler wins for matching types.
//         /// </summary>
//         [TestMethod]
//         public async Task CustomTypeHandler_RegisteredAfterBuiltIn_OverridesBuiltIn()
//         {
//             // Arrange - set up registry with enum command
//             var services = new ServiceCollection();
//             var cmdBuilder = new CommandRegistryBuilder();
//             cmdBuilder.RegisterCommand<CommandWithEnumArg>(); // CommandWithEnumArg --Level
//             var cmdRegistry = cmdBuilder.Build(services);

//             // Register custom handler AFTER built-ins (it should override EnumAutoCompleteHandler)
//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             handlerBuilder.Register<CustomLogLevelHandler>(); // This returns "CUSTOM_VALUE" instead of enum values
//             var handlerRegistry = handlerBuilder.Build(services);

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(cmdRegistry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types command with partial value
//             input.Write("CommandWithEnumArg --Level ");

//             // Act - trigger autocomplete
//             await acCtrl.Begin(input);

//             // Assert - custom handler's value should be used, NOT the built-in enum values
//             input.Buffer.Should().Be("CommandWithEnumArg --Level CUSTOM_VALUE");
//         }

//         /// <summary>
//         /// Implements: 008:TC-4.3
//         /// Attribute Handler used even when matching Type Handler exists.
//         /// Attribute takes precedence over type-matched handlers.
//         /// </summary>
//         [TestMethod]
//         public async Task AttributeHandler_TakesPrecedenceOverTypeHandler()
//         {
//             // Arrange - set up registry with command that has [AutoComplete<...>] attribute
//             var services = new ServiceCollection();
//             var cmdBuilder = new CommandRegistryBuilder();
//             cmdBuilder.RegisterCommand<CommandWithAttributeHandler>(); // Has [AutoComplete<AttributeSpecifiedHandler>]
//             var cmdRegistry = cmdBuilder.Build(services);

//             // Default handler registry includes EnumAutoCompleteHandler (which would match TestLogLevel)
//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             var handlerRegistry = handlerBuilder.Build(services);

//             // Register the attribute-specified handler with DI
//             services.AddTransient<AttributeSpecifiedHandler>();

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(cmdRegistry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types command
//             input.Write("CommandWithAttributeHandler --Level ");

//             // Act - trigger autocomplete
//             await acCtrl.Begin(input);

//             // Assert - attribute handler's value should be used, NOT the built-in enum values
//             input.Buffer.Should().Be("CommandWithAttributeHandler --Level ATTRIBUTE_VALUE");
//         }

//         /// <summary>
//         /// Implements: 008:TC-4.4
//         /// Handler receives ProvidedValues in context with already-entered values.
//         /// </summary>
//         [TestMethod]
//         public async Task Handler_ReceivesProvidedValuesInContext()
//         {
//             // Arrange - set up registry with multi-argument command
//             var services = new ServiceCollection();
//             var cmdBuilder = new CommandRegistryBuilder();
//             cmdBuilder.RegisterCommand<CommandWithMultipleArgs>(); // Has --First and --Second
//             var cmdRegistry = cmdBuilder.Build(services);

//             // Register handler that captures context
//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             var handlerRegistry = handlerBuilder.Build(services);

//             // Create and register the capturing handler
//             var capturingHandler = new ContextCapturingHandler();
//             services.AddSingleton<ContextCapturingHandler>(capturingHandler);

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(cmdRegistry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types command with --First already entered, now completing --Second
//             input.Write("CommandWithMultipleArgs --First existingValue --Second ");

//             // Act - trigger autocomplete
//             await acCtrl.Begin(input);

//             // Assert - handler should have received the First argument value in ProvidedValues
//             capturingHandler.LastContext.Should().NotBeNull();
//             capturingHandler.LastContext!.ProvidedValues.Should().NotBeEmpty();
            
//             // Find the First argument and verify its value was captured
//             var firstArg = capturingHandler.LastContext.ProvidedValues
//                 .FirstOrDefault(kv => kv.Key.Name == "First");
//             firstArg.Key.Should().NotBeNull("ProvidedValues should contain the 'First' argument");
//             firstArg.Value.Should().Be("existingValue");
//         }

//         /// <summary>
//         /// Implements: 008:TC-4.5
//         /// Boolean autocomplete works end-to-end.
//         /// When user types "f" for a bool argument, autocomplete returns "false".
//         /// </summary>
//         [TestMethod]
//         public async Task BooleanArgument_WithPrefix_AutocompletesMatchingValue()
//         {
//             // Arrange - set up a fresh registry with bool command
//             var services = new ServiceCollection();
//             var builder = new CommandRegistryBuilder();
//             builder.RegisterCommand<CommandWithBoolArg>(); // CommandWithBoolArg --Verbose
//             var registry = builder.Build(services);

//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             var handlerRegistry = handlerBuilder.Build(services);

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types command with partial bool value
//             input.Write("CommandWithBoolArg --Verbose f");

//             // Act - trigger autocomplete
//             await acCtrl.Begin(input);

//             // Assert - bool value "false" should be autocompleted
//             input.Buffer.Should().Be("CommandWithBoolArg --Verbose false");
//         }

//         /// <summary>
//         /// Implements: 008:TC-4.6
//         /// Nullable enum autocomplete works end-to-end.
//         /// When user types a prefix for a nullable enum argument, autocomplete returns matching enum values.
//         /// </summary>
//         [TestMethod]
//         public async Task NullableEnumArgument_WithPrefix_AutocompletesMatchingValue()
//         {
//             // Arrange - set up a fresh registry with nullable enum command
//             var services = new ServiceCollection();
//             var builder = new CommandRegistryBuilder();
//             builder.RegisterCommand<CommandWithNullableEnumArg>(); // CommandWithNullableEnumArg --Level
//             var registry = builder.Build(services);

//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             var handlerRegistry = handlerBuilder.Build(services);

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types command with partial enum value (nullable TestLogLevel?)
//             input.Write("CommandWithNullableEnumArg --Level err");

//             // Act - trigger autocomplete
//             await acCtrl.Begin(input);

//             // Assert - enum value "Error" should be autocompleted (unwrapped from nullable)
//             input.Buffer.Should().Be("CommandWithNullableEnumArg --Level Error");
//         }

//         /// <summary>
//         /// Implements: 008:TC-4.7
//         /// Handler exception gracefully degrades with logging.
//         /// When handler throws an exception, autocomplete returns no suggestions (graceful degradation).
//         /// </summary>
//         [TestMethod]
//         public async Task HandlerException_GracefullyDegrades_ReturnsNoSuggestions()
//         {
//             // Arrange - set up registry with command that has throwing handler
//             var services = new ServiceCollection();
//             services.AddTransient<ThrowingHandler>(); // Register the throwing handler with DI
//             var builder = new CommandRegistryBuilder();
//             builder.RegisterCommand<CommandWithThrowingHandler>(); // CommandWithThrowingHandler --Value
//             var registry = builder.Build(services);

//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             var handlerRegistry = handlerBuilder.Build(services);

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types command and triggers autocomplete on the argument with throwing handler
//             input.Write("CommandWithThrowingHandler --Value ");

//             // Act - trigger autocomplete (handler will throw)
//             // This should NOT throw - exception should be caught and handled gracefully
//             await acCtrl.Begin(input);

//             // Assert - buffer should remain unchanged (no autocomplete suggestions applied)
//             // The handler threw an exception, so no options should be available
//             input.Buffer.Should().Be("CommandWithThrowingHandler --Value ");
//         }

//         /// <summary>
//         /// Implements: 008:TC-4.8
//         /// Handler returning empty is valid result (no fallback).
//         /// When handler returns empty list, system does NOT continue to next handler.
//         /// Empty is a valid result meaning "I handled this, there are just no suggestions".
//         /// </summary>
//         [TestMethod]
//         public async Task HandlerReturningEmpty_IsValidResult_NoFallbackToTypeHandler()
//         {
//             // Reset static trackers
//             EmptyReturningHandler.Reset();
//             FallbackStringHandler.Reset();

//             // Arrange - set up registry with command that has empty-returning handler
//             // Also register a fallback type handler for string
//             var services = new ServiceCollection();
//             services.AddTransient<EmptyReturningHandler>();
//             services.AddTransient<FallbackStringHandler>();
//             var builder = new CommandRegistryBuilder();
//             builder.RegisterCommand<CommandWithEmptyHandler>();
//             var registry = builder.Build(services);

//             // Register the fallback string handler - this should NOT be called
//             // because the attribute handler (empty returning) takes precedence
//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             handlerBuilder.Register<FallbackStringHandler>();
//             var handlerRegistry = handlerBuilder.Build(services);

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types command and triggers autocomplete on the argument
//             input.Write("CommandWithEmptyHandler --Value ");

//             // Act - trigger autocomplete
//             await acCtrl.Begin(input);

//             // Assert
//             // 1. EmptyReturningHandler WAS called (the attribute handler)
//             EmptyReturningHandler.WasCalled.Should().BeTrue("attribute handler should be invoked");

//             // 2. FallbackStringHandler was NOT called (empty is valid, no fallback)
//             FallbackStringHandler.WasCalled.Should().BeFalse(
//                 "fallback handler should NOT be called when first handler returns empty - empty is a valid result");

//             // 3. Buffer should remain unchanged (no autocomplete suggestions applied)
//             input.Buffer.Should().Be("CommandWithEmptyHandler --Value ");
//         }

//         /// <summary>
//         /// Implements: 008:TC-4.9
//         /// New input cancels pending autocomplete request.
//         /// When user types while a slow handler is still processing, the first request
//         /// should be cancelled via CancellationToken.
//         /// </summary>
//         [TestMethod]
//         public async Task NewInput_CancelsPendingRequest_UsesSecondRequestResult()
//         {
//             // Reset static trackers
//             SlowHandler.Reset();

//             // Arrange - set up registry with command that has slow handler
//             var services = new ServiceCollection();
//             services.AddTransient<SlowHandler>();
//             var builder = new CommandRegistryBuilder();
//             builder.RegisterCommand<CommandWithSlowHandler>();
//             var registry = builder.Build(services);

//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             var handlerRegistry = handlerBuilder.Build(services);

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types command and triggers autocomplete on the argument
//             input.Write("CommandWithSlowHandler --Value ");

//             // Act - trigger first autocomplete (will take 500ms)
//             var firstTask = acCtrl.Begin(input);

//             // Simulate user typing again quickly (before first completes)
//             // This should trigger cancellation of the first request
//             await Task.Delay(50); // Small delay to let first request start
//             input.Write("a"); // User types another character
//             var secondTask = acCtrl.Begin(input);

//             // Wait for both to complete
//             await Task.WhenAll(firstTask, secondTask);

//             // Assert
//             // 1. SlowHandler should have been called twice
//             SlowHandler.CallCount.Should().Be(2, "handler should be invoked twice");

//             // 2. First invocation should have been cancelled
//             SlowHandler.CancelledCount.Should().BeGreaterOrEqualTo(1, 
//                 "first request should be cancelled when second request starts");

//             // 3. Only one invocation should complete successfully (the second one)
//             SlowHandler.CompletedCount.Should().Be(1, 
//                 "only the second request should complete successfully");
//         }

//         /// <summary>
//         /// Implements: 008:UX-001
//         /// Ghost text auto-appears when cursor enters an autocomplete-applicable position.
//         /// No additional keypress is needed - just calling Begin triggers ghost text display.
//         /// Ghost text shows the first alphabetical match.
//         /// </summary>
//         [TestMethod]
//         public async Task GhostText_AutoAppears_AtApplicablePosition()
//         {
//             // Arrange - set up registry with multiple commands that match prefix
//             var services = new ServiceCollection();
//             var builder = new CommandRegistryBuilder();
//             builder.RegisterCommand<ZebraCommand>(); // Alphabetically last
//             builder.RegisterCommand<AlphaCommand>(); // Alphabetically first
//             builder.RegisterCommand<BetaCommand>();  // Alphabetically second
//             var registry = builder.Build(services);

//             var handlerBuilder = new AutoCompleteHandlerRegistryBuilder();
//             var handlerRegistry = handlerBuilder.Build(services);

//             var sp = services.BuildServiceProvider();

//             var console = new VirtualConsoleAnsiAdapter(new BitPantry.VirtualConsole.VirtualConsole(80, 24));
//             var input = new ConsoleLineMirror(console);
//             var acCtrl = new AutoCompleteController(
//                 new AutoCompleteOptionSetBuilder(registry, new NoopServerProxy(), sp, handlerRegistry));

//             // User types partial text at command position
//             input.Write("a");

//             // Act - trigger autocomplete at applicable position
//             // No additional keypress needed - just calling Begin shows ghost text
//             await acCtrl.Begin(input);

//             // Assert
//             // 1. Buffer should now contain ghost text with first alphabetical match
//             input.Buffer.Should().Be("AlphaCommand", 
//                 "ghost text should auto-appear with first alphabetical match");

//             // 2. Buffer position should be at end of inserted text
//             input.BufferPosition.Should().Be("AlphaCommand".Length);
//         }

//         #endregion

//         #region Test Helpers

//         /// <summary>
//         /// Custom handler for TestLogLevel that returns a distinctive value
//         /// to verify it overrides the built-in EnumAutoCompleteHandler.
//         /// </summary>
//         private class CustomLogLevelHandler : ITypeAutoCompleteHandler
//         {
//             public bool CanHandle(Type argumentType)
//             {
//                 // Handle TestLogLevel (same type as built-in EnumAutoCompleteHandler would match)
//                 return argumentType == typeof(TestLogLevel);
//             }

//             public Task<List<AutoCompleteOption>> GetOptionsAsync(
//                 AutoCompleteContext context,
//                 CancellationToken cancellationToken = default)
//             {
//                 // Return a distinctive value to prove this handler was used
//                 var options = new List<AutoCompleteOption>
//                 {
//                     new AutoCompleteOption("CUSTOM_VALUE")
//                 };
//                 return Task.FromResult(options);
//             }
//         }

//         /// <summary>
//         /// Handler specified via [AutoComplete<AttributeSpecifiedHandler>] attribute.
//         /// Returns a distinctive value to prove attribute precedence.
//         /// </summary>
//         private class AttributeSpecifiedHandler : IAutoCompleteHandler
//         {
//             public Task<List<AutoCompleteOption>> GetOptionsAsync(
//                 AutoCompleteContext context,
//                 CancellationToken cancellationToken = default)
//             {
//                 var options = new List<AutoCompleteOption>
//                 {
//                     new AutoCompleteOption("ATTRIBUTE_VALUE")
//                 };
//                 return Task.FromResult(options);
//             }
//         }

//         /// <summary>
//         /// Command with [AutoComplete<>] attribute on enum argument.
//         /// Even though TestLogLevel is an enum (matched by EnumAutoCompleteHandler),
//         /// the attribute handler should take precedence.
//         /// </summary>
//         [Command]
//         private class CommandWithAttributeHandler : CommandBase
//         {
//             [Argument]
//             [AutoComplete<AttributeSpecifiedHandler>]
//             public TestLogLevel Level { get; set; }

//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         /// <summary>
//         /// Command with multiple string arguments for testing ProvidedValues context.
//         /// </summary>
//         [Command]
//         private class CommandWithMultipleArgs : CommandBase
//         {
//             [Argument]
//             public string First { get; set; }

//             [Argument]
//             [AutoComplete<ContextCapturingHandler>]
//             public string Second { get; set; }

//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         /// <summary>
//         /// Handler that captures the context for verification.
//         /// </summary>
//         private class ContextCapturingHandler : IAutoCompleteHandler
//         {
//             public AutoCompleteContext? LastContext { get; private set; }

//             public Task<List<AutoCompleteOption>> GetOptionsAsync(
//                 AutoCompleteContext context,
//                 CancellationToken cancellationToken = default)
//             {
//                 LastContext = context;
//                 var options = new List<AutoCompleteOption>
//                 {
//                     new AutoCompleteOption("CAPTURED")
//                 };
//                 return Task.FromResult(options);
//             }
//         }

//         /// <summary>
//         /// Command with a boolean argument for testing BooleanAutoCompleteHandler.
//         /// </summary>
//         [Command]
//         private class CommandWithBoolArg : CommandBase
//         {
//             [Argument]
//             public bool Verbose { get; set; }

//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         /// <summary>
//         /// Command with a nullable enum argument for testing nullable enum unwrapping.
//         /// </summary>
//         [Command]
//         private class CommandWithNullableEnumArg : CommandBase
//         {
//             [Argument]
//             public TestLogLevel? Level { get; set; }

//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         /// <summary>
//         /// Handler that throws an exception for testing graceful degradation.
//         /// </summary>
//         private class ThrowingHandler : IAutoCompleteHandler
//         {
//             public Task<List<AutoCompleteOption>> GetOptionsAsync(
//                 AutoCompleteContext context,
//                 CancellationToken cancellationToken = default)
//             {
//                 throw new InvalidOperationException("Test exception from ThrowingHandler");
//             }
//         }

//         /// <summary>
//         /// Command with a throwing handler for testing exception handling.
//         /// </summary>
//         [Command]
//         private class CommandWithThrowingHandler : CommandBase
//         {
//             [Argument]
//             [AutoComplete<ThrowingHandler>]
//             public string Value { get; set; }

//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         /// <summary>
//         /// Handler that returns an empty list (valid result - no suggestions).
//         /// </summary>
//         private class EmptyReturningHandler : IAutoCompleteHandler
//         {
//             public static bool WasCalled { get; private set; }

//             public static void Reset() => WasCalled = false;

//             public Task<List<AutoCompleteOption>> GetOptionsAsync(
//                 AutoCompleteContext context,
//                 CancellationToken cancellationToken = default)
//             {
//                 WasCalled = true;
//                 return Task.FromResult(new List<AutoCompleteOption>()); // Empty list is a valid result
//             }
//         }

//         /// <summary>
//         /// Fallback type handler for string - should NOT be called when EmptyReturningHandler returns empty.
//         /// </summary>
//         private class FallbackStringHandler : ITypeAutoCompleteHandler
//         {
//             public static bool WasCalled { get; private set; }

//             public static void Reset() => WasCalled = false;

//             public bool CanHandle(Type argumentType) => argumentType == typeof(string);

//             public Task<List<AutoCompleteOption>> GetOptionsAsync(
//                 AutoCompleteContext context,
//                 CancellationToken cancellationToken = default)
//             {
//                 WasCalled = true;
//                 return Task.FromResult(new List<AutoCompleteOption>
//                 {
//                     new AutoCompleteOption("FALLBACK_VALUE")
//                 });
//             }
//         }

//         /// <summary>
//         /// Command with empty-returning handler for testing empty is valid result.
//         /// </summary>
//         [Command]
//         private class CommandWithEmptyHandler : CommandBase
//         {
//             [Argument]
//             [AutoComplete<EmptyReturningHandler>]
//             public string Value { get; set; }

//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         /// <summary>
//         /// Handler that takes 500ms to respond, used for testing request cancellation.
//         /// Tracks whether CancellationToken was honored.
//         /// </summary>
//         private class SlowHandler : IAutoCompleteHandler
//         {
//             private static int _callCount;
//             private static int _cancelledCount;
//             private static int _completedCount;

//             public static int CallCount => _callCount;
//             public static int CancelledCount => _cancelledCount;
//             public static int CompletedCount => _completedCount;

//             public static void Reset()
//             {
//                 _callCount = 0;
//                 _cancelledCount = 0;
//                 _completedCount = 0;
//             }

//             public async Task<List<AutoCompleteOption>> GetOptionsAsync(
//                 AutoCompleteContext context,
//                 CancellationToken cancellationToken = default)
//             {
//                 Interlocked.Increment(ref _callCount);

//                 try
//                 {
//                     // Simulate slow operation (500ms)
//                     await Task.Delay(500, cancellationToken);

//                     Interlocked.Increment(ref _completedCount);
//                     return new List<AutoCompleteOption>
//                     {
//                         new AutoCompleteOption("SLOW_RESULT")
//                     };
//                 }
//                 catch (OperationCanceledException)
//                 {
//                     Interlocked.Increment(ref _cancelledCount);
//                     throw;
//                 }
//             }
//         }

//         /// <summary>
//         /// Command with slow handler for testing request cancellation.
//         /// </summary>
//         [Command]
//         private class CommandWithSlowHandler : CommandBase
//         {
//             [Argument]
//             [AutoComplete<SlowHandler>]
//             public string Value { get; set; }

//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         /// <summary>
//         /// Test command for UX-001: alphabetically first (starts with 'A').
//         /// </summary>
//         [Command]
//         private class AlphaCommand : CommandBase
//         {
//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         /// <summary>
//         /// Test command for UX-001: alphabetically second (starts with 'B').
//         /// </summary>
//         [Command]
//         private class BetaCommand : CommandBase
//         {
//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         /// <summary>
//         /// Test command for UX-001: alphabetically last (starts with 'Z').
//         /// </summary>
//         [Command]
//         private class ZebraCommand : CommandBase
//         {
//             public void Execute(CommandExecutionContext ctx) { }
//         }

//         #endregion
//     }
// }
