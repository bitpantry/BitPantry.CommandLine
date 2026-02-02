using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete
{
    /// <summary>
    /// Tests to verify that built-in autocomplete handlers (BooleanAutoCompleteHandler, 
    /// EnumAutoCompleteHandler) are properly registered in DI and can be activated.
    /// 
    /// Bug reproduction: When using the sandbox, typing "server " → Tab → Down → Enter 
    /// on a command with a boolean argument throws:
    /// "InvalidOperationException: No service for type 'BooleanAutoCompleteHandler' has been registered"
    /// </summary>
    [TestClass]
    public class BooleanAutoCompleteRegistrationTests
    {
        #region Test Commands

        public enum TestLogLevel { Debug, Info, Warning, Error }

        [Command(Name = "testcmd")]
        [Description("Test command with boolean argument")]
        public class CommandWithBoolArg : CommandBase
        {
            [Argument]
            [Description("Enable verbose output")]
            public bool Verbose { get; set; }

            [Argument]
            [Description("Log level")]
            public TestLogLevel Level { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        /// <summary>
        /// Tests the BUG scenario: When service provider is built BEFORE handler registry,
        /// the handlers are not registered and activation throws.
        /// This test verifies the bug exists - it's a documentation test.
        /// </summary>
        [TestMethod]
        public void AutoComplete_BooleanArgument_ThrowsWhenBuildOrderIsWrong()
        {
            // Arrange - set up command registry with boolean argument command
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<CommandWithBoolArg>();
            var registry = builder.Build(services);

            // BUG SCENARIO: Build service provider BEFORE handler registry
            // This is the WRONG order that causes the bug
            var serviceProvider = services.BuildServiceProvider();

            // Now build handler registry - but it's too late, handlers aren't in the already-built provider
            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerRegistryBuilder.Build(services); // Adds handlers to services AFTER provider is built

            var activator = new AutoCompleteHandlerActivator(serviceProvider);

            // Act - try to activate BooleanAutoCompleteHandler
            // This FAILS because the service provider doesn't have the handlers
            var act = () => activator.Activate<BooleanAutoCompleteHandler>();

            // Assert - demonstrates the bug exists when build order is wrong
            act.Should().Throw<System.InvalidOperationException>()
                .WithMessage("*BooleanAutoCompleteHandler*");
        }

        /// <summary>
        /// Correct scenario: Build handler registry BEFORE building service provider.
        /// This is the fixed behavior and what CommandLineApplicationBuilder should do.
        /// </summary>
        [TestMethod]
        public void AutoComplete_BooleanArgument_HandlerCanBeActivated()
        {
            // Arrange - set up command registry with boolean argument command
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<CommandWithBoolArg>();
            var registry = builder.Build(services);

            // Build handler registry - this should register handlers with DI
            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerRegistryBuilder.Build(services);

            // Build service provider AFTER handler registry is built
            var serviceProvider = services.BuildServiceProvider();
            var activator = new AutoCompleteHandlerActivator(serviceProvider);

            // Act - try to activate BooleanAutoCompleteHandler
            // This is what happens internally when autocomplete triggers for a boolean argument
            var act = () => activator.Activate<BooleanAutoCompleteHandler>();

            // Assert - should not throw, handler should be registered
            act.Should().NotThrow<System.InvalidOperationException>();
            
            using var result = activator.Activate<BooleanAutoCompleteHandler>();
            result.Handler.Should().NotBeNull();
            result.Handler.Should().BeOfType<BooleanAutoCompleteHandler>();
        }

        /// <summary>
        /// Same test for EnumAutoCompleteHandler.
        /// </summary>
        [TestMethod]
        public void AutoComplete_EnumArgument_HandlerCanBeActivated()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<CommandWithBoolArg>();
            var registry = builder.Build(services);

            // Build handler registry - this should register handlers with DI
            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerRegistryBuilder.Build(services);

            // Build service provider AFTER handler registry is built
            var serviceProvider = services.BuildServiceProvider();
            var activator = new AutoCompleteHandlerActivator(serviceProvider);

            // Act - try to activate EnumAutoCompleteHandler
            var act = () => activator.Activate<EnumAutoCompleteHandler>();

            // Assert - should not throw
            act.Should().NotThrow<System.InvalidOperationException>();
            
            using var result = activator.Activate<EnumAutoCompleteHandler>();
            result.Handler.Should().NotBeNull();
            result.Handler.Should().BeOfType<EnumAutoCompleteHandler>();
        }

        /// <summary>
        /// Integration test: Type command with boolean arg and trigger autocomplete.
        /// </summary>
        [TestMethod]
        public async Task AutoComplete_BooleanArgument_ShowsOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<CommandWithBoolArg>();
            var registry = builder.Build(services);

            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerRegistryBuilder.Build(services);

            var serviceProvider = services.BuildServiceProvider();
            var activator = new AutoCompleteHandlerActivator(serviceProvider);

            var virtualConsole = new BitPantry.VirtualConsole.VirtualConsole(80, 24);
            var ansiAdapter = new VirtualConsoleAnsiAdapter(virtualConsole);
            var line = new ConsoleLineMirror(ansiAdapter);

            var controller = new AutoCompleteController(registry, ansiAdapter, handlerRegistry, activator, new NoopServerProxy(), NullLogger<AutoCompleteSuggestionProvider>.Instance);

            // Type command with --Verbose and space (triggers value autocomplete)
            line.Write("testcmd --Verbose ");
            
            // Act - update controller to trigger autocomplete
            var act = () => controller.Update(line);

            // Assert - should not throw
            act.Should().NotThrow();
        }
    }
}
