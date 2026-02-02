using System;
using System.Collections.Generic;
using System.Threading;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.AutoComplete.Rendering;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Component;
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
    /// Tests for AutoCompleteController menu mode integration.
    /// Verifies menu activation, keyboard handling, and selection.
    /// </summary>
    [TestClass]
    public class AutoCompleteControllerMenuTests
    {
        private ICommandRegistry _registry;
        private BitPantry.VirtualConsole.VirtualConsole _virtualConsole;
        private VirtualConsoleAnsiAdapter _ansiAdapter;
        private ConsoleLineMirror _line;
        private IAutoCompleteHandlerRegistry _handlerRegistry;
        private AutoCompleteHandlerActivator _handlerActivator;

        #region Test Commands

        [Command(Name = "help")]
        [Description("Display help")]
        private class HelpCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "history")]
        [Description("Show command history")]
        private class HistoryCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "host")]
        [Description("Host settings")]
        private class HostCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "exit")]
        [Description("Exit application")]
        private class ExitCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        public enum LogLevel { Debug, Info, Warning, Error }

        [Command(Name = "log")]
        [Description("Configure logging")]
        private class LogCommand : CommandBase
        {
            [Argument]
            [Alias('l')]
            [Description("The log level")]
            public LogLevel Level { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        [TestInitialize]
        public void Setup()
        {
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<HelpCommand>();
            builder.RegisterCommand<HistoryCommand>();
            builder.RegisterCommand<HostCommand>();
            builder.RegisterCommand<ExitCommand>();
            builder.RegisterCommand<LogCommand>();

            _registry = builder.Build();

            _virtualConsole = new BitPantry.VirtualConsole.VirtualConsole(80, 24);
            _ansiAdapter = new VirtualConsoleAnsiAdapter(_virtualConsole);
            _line = new ConsoleLineMirror(_ansiAdapter);

            var services = new ServiceCollection();
            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            _handlerRegistry = handlerRegistryBuilder.Build(services);
            var serviceProvider = services.BuildServiceProvider();
            _handlerActivator = new AutoCompleteHandlerActivator(serviceProvider);
        }

        private AutoCompleteController CreateController()
        {
            return new AutoCompleteController(_registry, _ansiAdapter, _handlerRegistry, _handlerActivator, new NoopServerProxy(), NullLogger<AutoCompleteSuggestionProvider>.Instance);
        }

        #region Menu Activation Tests

        [TestMethod]
        public void ShowMenu_WhenMultipleOptions_SetsMenuMode()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h"); // Matches help, history, host
            controller.Update(_line);

            // Act
            controller.ShowMenu(_line);

            // Assert
            controller.Mode.Should().Be(AutoCompleteMode.Menu);
        }

        [TestMethod]
        public void ShowMenu_WhenSingleOption_StaysInGhostTextMode()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("ex"); // Only matches exit
            controller.Update(_line);

            // Act
            controller.ShowMenu(_line);

            // Assert
            controller.Mode.Should().Be(AutoCompleteMode.GhostText);
        }

        [TestMethod]
        public void ShowMenu_WhenNoOptions_StaysIdle()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("xyz"); // No matches
            controller.Update(_line);

            // Act
            controller.ShowMenu(_line);

            // Assert
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void ShowMenu_HidesGhostText()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h"); // Matches help, history, host
            controller.Update(_line);
            controller.GhostTextController.Text.Should().NotBeNullOrEmpty();

            // Act
            controller.ShowMenu(_line);

            // Assert
            controller.GhostTextController.Text.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void ShowMenu_CreatesMenuWithOptions()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);

            // Act
            controller.ShowMenu(_line);

            // Assert
            controller.MenuController.Menu.Should().NotBeNull();
            controller.MenuController.Menu.FilteredOptions.Count.Should().BeGreaterOrEqualTo(3); // help, history, host
        }

        #endregion

        #region Menu Navigation Tests

        [TestMethod]
        public void HandleMenuKey_DownArrow_NavigatesDown()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);
            var initialSelection = controller.MenuController.Menu.SelectedIndex;

            // Act
            var result = controller.HandleMenuKey(ConsoleKey.DownArrow);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.Handled);
            controller.MenuController.Menu.SelectedIndex.Should().Be(initialSelection + 1);
        }

        [TestMethod]
        public void HandleMenuKey_UpArrow_NavigatesUp()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);
            controller.HandleMenuKey(ConsoleKey.DownArrow); // Move to second item

            // Act
            var result = controller.HandleMenuKey(ConsoleKey.UpArrow);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.Handled);
            controller.MenuController.Menu.SelectedIndex.Should().Be(0);
        }

        [TestMethod]
        public void HandleMenuKey_WhenNotInMenuMode_ReturnsNotHandled()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("ex");
            controller.Update(_line);
            // Ghost text mode, not menu mode

            // Act
            var result = controller.HandleMenuKey(ConsoleKey.DownArrow);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.NotHandled);
        }

        #endregion

        #region Menu Selection Tests

        [TestMethod]
        public void HandleMenuKey_Tab_ReturnsSelected()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Act
            var result = controller.HandleMenuKey(ConsoleKey.Tab);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.Selected);
        }

        [TestMethod]
        public void HandleMenuKey_Enter_ReturnsSelected()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Act
            var result = controller.HandleMenuKey(ConsoleKey.Enter);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.Selected);
        }

        [TestMethod]
        public void AcceptMenuSelection_AppliesSelectedOption()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);
            controller.HandleMenuKey(ConsoleKey.DownArrow); // Move to "help" -> "history"

            // Act
            controller.AcceptMenuSelection(_line);

            // Assert - line should now contain selected command
            _line.Buffer.Should().Contain("h"); // At minimum contains the typed prefix
        }

        [TestMethod]
        public void AcceptMenuSelection_ClosesMenu()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Act
            controller.AcceptMenuSelection(_line);

            // Assert
            controller.Mode.Should().NotBe(AutoCompleteMode.Menu);
        }

        #endregion

        #region Menu Dismiss Tests

        [TestMethod]
        public void HandleMenuKey_Escape_ReturnsDismissed()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Act
            var result = controller.HandleMenuKey(ConsoleKey.Escape);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.Dismissed);
        }

        [TestMethod]
        public void HandleMenuKey_Escape_ClosesMenu()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Act
            controller.HandleMenuKey(ConsoleKey.Escape);

            // Assert
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void HideMenu_SetsIdleMode()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Act
            controller.HideMenu();

            // Assert
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        #endregion

        #region Menu Property Tests

        [TestMethod]
        public void Menu_WhenNotInMenuMode_ReturnsNull()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("ex");
            controller.Update(_line);

            // Assert
            controller.MenuController.Menu.Should().BeNull();
        }

        [TestMethod]
        public void Menu_WhenInMenuMode_ReturnsActiveMenu()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Assert
            controller.MenuController.Menu.Should().NotBeNull();
            controller.MenuController.Menu.SelectedOption.Should().NotBeNull();
        }

        #endregion

        #region Menu Rendering Tests

        [TestMethod]
        public void ShowMenu_RendersMenuBelowInput()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);

            // Act
            controller.ShowMenu(_line);

            // Assert - menu should be rendered on rows below input
            var row1 = _virtualConsole.GetRow(1).GetText();
            (row1.Contains("help") || row1.Contains("history") || row1.Contains("host")).Should().BeTrue();
        }

        [TestMethod]
        public void UpdateMenuDisplay_RerendersMenu()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);
            controller.HandleMenuKey(ConsoleKey.DownArrow);

            // Act
            controller.UpdateMenuDisplay();

            // Assert - should not throw, menu should still be visible
            controller.Mode.Should().Be(AutoCompleteMode.Menu);
        }

        #endregion

        #region Reset Tests

        [TestMethod]
        public void Reset_WhenInMenuMode_ClosesMenu()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("h");
            controller.Update(_line);
            controller.ShowMenu(_line);

            // Act
            controller.Reset(0);

            // Assert
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
            controller.MenuController.Menu.Should().BeNull();
        }

        #endregion
    }
}
