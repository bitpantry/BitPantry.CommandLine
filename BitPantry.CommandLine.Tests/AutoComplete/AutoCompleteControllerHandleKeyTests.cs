using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Context;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete
{
    /// <summary>
    /// Tests for AutoCompleteController.HandleKey method which provides
    /// unified key handling for all autocomplete operations.
    /// </summary>
    [TestClass]
    public class AutoCompleteControllerHandleKeyTests
    {
        private ICommandRegistry _registry;
        private BitPantry.VirtualConsole.VirtualConsole _virtualConsole;
        private VirtualConsoleAnsiAdapter _ansiAdapter;
        private ConsoleLineMirror _line;
        private IAutoCompleteHandlerRegistry _handlerRegistry;
        private AutoCompleteHandlerActivator _handlerActivator;

        #region Test Commands

        [Command(Name = "greet")]
        [Description("A greeting command")]
        public class GreetCommand : CommandBase
        {
            public string Name { get; set; }
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "goodbye")]
        [Description("A goodbye command")]
        public class GoodbyeCommand : CommandBase
        {
            public string Name { get; set; }
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "configure")]
        [Description("Configure settings")]
        public class ConfigureCommand : CommandBase
        {
            [Argument]
            [Description("Verbose output")]
            public bool Verbose { get; set; }

            [Argument]
            [Description("Output path")]
            public string Output { get; set; }
            
            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        [TestInitialize]
        public void Setup()
        {
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<GreetCommand>();
            builder.RegisterCommand<GoodbyeCommand>();
            builder.RegisterCommand<ConfigureCommand>();
            _registry = builder.Build();

            _virtualConsole = new BitPantry.VirtualConsole.VirtualConsole(80, 24);
            _ansiAdapter = new VirtualConsoleAnsiAdapter(_virtualConsole);
            _line = new ConsoleLineMirror(_ansiAdapter);
        }

        private AutoCompleteController CreateController()
        {
            var services = new ServiceCollection();
            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            _handlerRegistry = handlerRegistryBuilder.Build(services);

            var serviceProvider = services.BuildServiceProvider();
            _handlerActivator = new AutoCompleteHandlerActivator(serviceProvider);

            return new AutoCompleteController(_registry, _ansiAdapter, _handlerRegistry, _handlerActivator);
        }

        #region Idle Mode HandleKey Tests

        [TestMethod]
        public void HandleKey_InIdleMode_UnhandledKey_ReturnsFalse()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("test");
            controller.Update(_line);

            // Act - press a key that shouldn't be handled
            var result = controller.HandleKey(ConsoleKey.A, _line);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void HandleKey_InIdleMode_Tab_WithNoOptions_ReturnsFalse()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("xyz"); // No matching commands
            controller.Update(_line);

            // Act
            var result = controller.HandleKey(ConsoleKey.Tab, _line);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GhostText Mode HandleKey Tests

        [TestMethod]
        public void HandleKey_InGhostTextMode_Tab_WithSingleOption_ReturnsTrue()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("gree"); // Matches "greet" only
            controller.Update(_line);

            // Verify ghost text is showing
            controller.Mode.Should().Be(AutoCompleteMode.GhostText);

            // Act
            var result = controller.HandleKey(ConsoleKey.Tab, _line);

            // Assert
            result.Should().BeTrue();
            _line.Buffer.Should().Contain("greet");
        }

        [TestMethod]
        public void HandleKey_InGhostTextMode_Tab_WithMultipleOptions_ShowsMenu()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);

            // Verify ghost text is showing
            controller.Mode.Should().Be(AutoCompleteMode.GhostText);

            // Act
            var result = controller.HandleKey(ConsoleKey.Tab, _line);

            // Assert
            result.Should().BeTrue();
            controller.Mode.Should().Be(AutoCompleteMode.Menu);
        }

        [TestMethod]
        public void HandleKey_InGhostTextMode_RightArrow_AcceptsGhostText()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("gree"); // Matches "greet" only
            controller.Update(_line);

            // Verify ghost text is showing
            controller.Mode.Should().Be(AutoCompleteMode.GhostText);

            // Act
            var result = controller.HandleKey(ConsoleKey.RightArrow, _line);

            // Assert
            result.Should().BeTrue();
            _line.Buffer.Should().Contain("greet");
        }

        [TestMethod]
        public void HandleKey_InGhostTextMode_Escape_SuppressesGhostText()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("gree"); // Matches "greet" only
            controller.Update(_line);

            // Verify ghost text is showing
            controller.Mode.Should().Be(AutoCompleteMode.GhostText);

            // Act
            var result = controller.HandleKey(ConsoleKey.Escape, _line);

            // Assert
            result.Should().BeTrue();
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HandleKey_InGhostTextMode_UpArrow_DismissesAndReturnsFalse()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("gree"); // Matches "greet" only
            controller.Update(_line);

            // Verify ghost text is showing
            controller.Mode.Should().Be(AutoCompleteMode.GhostText);

            // Act
            var result = controller.HandleKey(ConsoleKey.UpArrow, _line);

            // Assert
            result.Should().BeFalse(); // Let history handle
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HandleKey_InGhostTextMode_DownArrow_DismissesAndReturnsFalse()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("gree"); // Matches "greet" only
            controller.Update(_line);

            // Verify ghost text is showing
            controller.Mode.Should().Be(AutoCompleteMode.GhostText);

            // Act
            var result = controller.HandleKey(ConsoleKey.DownArrow, _line);

            // Assert
            result.Should().BeFalse(); // Let history handle
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        #endregion

        #region Menu Mode HandleKey Tests

        [TestMethod]
        public void HandleKey_InMenuMode_Tab_AcceptsSelection()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Verify menu is showing
            controller.Mode.Should().Be(AutoCompleteMode.Menu);

            // Act
            var result = controller.HandleKey(ConsoleKey.Tab, _line);

            // Assert
            result.Should().BeTrue();
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HandleKey_InMenuMode_Enter_AcceptsSelection()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Verify menu is showing
            controller.Mode.Should().Be(AutoCompleteMode.Menu);

            // Act
            var result = controller.HandleKey(ConsoleKey.Enter, _line);

            // Assert
            result.Should().BeTrue();
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HandleKey_InMenuMode_Spacebar_AcceptsSelection()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Verify menu is showing
            controller.Mode.Should().Be(AutoCompleteMode.Menu);

            // Act
            var result = controller.HandleKey(ConsoleKey.Spacebar, _line);

            // Assert
            result.Should().BeTrue();
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HandleKey_InMenuMode_Escape_DismissesMenu()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Verify menu is showing
            controller.Mode.Should().Be(AutoCompleteMode.Menu);

            // Act
            var result = controller.HandleKey(ConsoleKey.Escape, _line);

            // Assert
            result.Should().BeTrue();
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HandleKey_InMenuMode_DownArrow_NavigatesMenu()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);
            var firstOption = controller.MenuController.SelectedOption?.Value;

            // Act
            var result = controller.HandleKey(ConsoleKey.DownArrow, _line);

            // Assert
            result.Should().BeTrue();
            controller.Mode.Should().Be(AutoCompleteMode.Menu);
            controller.MenuController.SelectedOption?.Value.Should().NotBe(firstOption);
        }

        [TestMethod]
        public void HandleKey_InMenuMode_UpArrow_NavigatesMenu()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);
            
            // Navigate down first
            controller.HandleKey(ConsoleKey.DownArrow, _line);
            var secondOption = controller.MenuController.SelectedOption?.Value;

            // Act
            var result = controller.HandleKey(ConsoleKey.UpArrow, _line);

            // Assert
            result.Should().BeTrue();
            controller.Mode.Should().Be(AutoCompleteMode.Menu);
            controller.MenuController.SelectedOption?.Value.Should().NotBe(secondOption);
        }

        [TestMethod]
        public void HandleKey_InMenuMode_LeftArrow_DismissesMenuAndReturnsFalse()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Verify menu is showing
            controller.Mode.Should().Be(AutoCompleteMode.Menu);

            // Act
            var result = controller.HandleKey(ConsoleKey.LeftArrow, _line);

            // Assert
            result.Should().BeFalse(); // Let cursor move
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HandleKey_InMenuMode_RightArrow_DismissesMenuAndReturnsFalse()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Verify menu is showing
            controller.Mode.Should().Be(AutoCompleteMode.Menu);

            // Act
            var result = controller.HandleKey(ConsoleKey.RightArrow, _line);

            // Assert
            result.Should().BeFalse(); // Let cursor move
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HandleKey_InMenuMode_Backspace_ReturnsFalse()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Verify menu is showing
            controller.Mode.Should().Be(AutoCompleteMode.Menu);

            // Act - Backspace needs special handling in InputBuilder
            var result = controller.HandleKey(ConsoleKey.Backspace, _line);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Mode Transition Tests

        [TestMethod]
        public void HandleKey_ModeTransition_FromGhostTextToMenu_ViaTab()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.Mode.Should().Be(AutoCompleteMode.GhostText);

            // Act
            controller.HandleKey(ConsoleKey.Tab, _line);

            // Assert
            controller.Mode.Should().Be(AutoCompleteMode.Menu);
        }

        [TestMethod]
        public void HandleKey_ModeTransition_FromMenuToIdle_ViaAccept()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("g"); // Matches "greet" and "goodbye"
            controller.Update(_line);
            controller.ShowMenu(_line);
            controller.Mode.Should().Be(AutoCompleteMode.Menu);

            // Act
            controller.HandleKey(ConsoleKey.Tab, _line);

            // Assert
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HandleKey_ModeTransition_FromGhostTextToIdle_ViaDismiss()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("gree"); // Matches "greet" only
            controller.Update(_line);
            controller.Mode.Should().Be(AutoCompleteMode.GhostText);

            // Act
            controller.HandleKey(ConsoleKey.Escape, _line);

            // Assert
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        #endregion
    }
}
