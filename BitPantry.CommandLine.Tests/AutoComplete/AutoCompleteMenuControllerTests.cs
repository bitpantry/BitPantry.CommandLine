using System;
using System.Collections.Generic;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete
{
    /// <summary>
    /// Tests for AutoCompleteMenuController which consolidates AutoCompleteMenu,
    /// MenuController, and MenuRenderer into a single component.
    /// </summary>
    [TestClass]
    public class AutoCompleteMenuControllerTests
    {
        private BitPantry.VirtualConsole.VirtualConsole _virtualConsole;
        private VirtualConsoleAnsiAdapter _ansiAdapter;
        private AutoCompleteMenuController _controller;

        [TestInitialize]
        public void Setup()
        {
            _virtualConsole = new BitPantry.VirtualConsole.VirtualConsole(80, 24);
            _ansiAdapter = new VirtualConsoleAnsiAdapter(_virtualConsole);
            _controller = new AutoCompleteMenuController(_ansiAdapter);
        }

        private ConsoleLineMirror CreateLine(string text = "")
        {
            var line = new ConsoleLineMirror(_ansiAdapter);
            if (!string.IsNullOrEmpty(text))
            {
                line.Write(text);
            }
            return line;
        }

        private static List<AutoCompleteOption> CreateOptions(params string[] values)
        {
            var options = new List<AutoCompleteOption>();
            foreach (var value in values)
            {
                options.Add(new AutoCompleteOption(value));
            }
            return options;
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new AutoCompleteMenuController(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void IsVisible_Initially_IsFalse()
        {
            // Assert
            _controller.IsVisible.Should().BeFalse();
        }

        [TestMethod]
        public void SelectedOption_WhenNotVisible_IsNull()
        {
            // Assert
            _controller.SelectedOption.Should().BeNull();
        }

        [TestMethod]
        public void Menu_WhenNotVisible_IsNull()
        {
            // Assert
            _controller.Menu.Should().BeNull();
        }

        #endregion

        #region Show Tests

        [TestMethod]
        public void Show_WithValidOptions_SetsIsVisibleToTrue()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");

            // Act
            _controller.Show(options, line);

            // Assert
            _controller.IsVisible.Should().BeTrue();
        }

        [TestMethod]
        public void Show_WithValidOptions_SetsSelectedOptionToFirstOption()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");

            // Act
            _controller.Show(options, line);

            // Assert
            _controller.SelectedOption.Should().NotBeNull();
            _controller.SelectedOption.Value.Should().Be("Alpha");
        }

        [TestMethod]
        public void Show_WithValidOptions_SetsMenuProperty()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");

            // Act
            _controller.Show(options, line);

            // Assert
            _controller.Menu.Should().NotBeNull();
        }

        [TestMethod]
        public void Show_WithNullOptions_DoesNotThrow()
        {
            // Arrange
            var line = CreateLine("test");

            // Act
            Action act = () => _controller.Show(null, line);

            // Assert - should not throw, just not show menu
            act.Should().NotThrow();
            _controller.IsVisible.Should().BeFalse();
        }

        [TestMethod]
        public void Show_WithEmptyOptions_DoesNotShow()
        {
            // Arrange
            var options = CreateOptions();
            var line = CreateLine("test");

            // Act
            _controller.Show(options, line);

            // Assert
            _controller.IsVisible.Should().BeFalse();
        }

        [TestMethod]
        public void Show_WithNullLine_ThrowsArgumentNullException()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta");

            // Act
            Action act = () => _controller.Show(options, null);

            // Assert - MenuRenderer requires a line for rendering
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Hide Tests

        [TestMethod]
        public void Hide_WhenVisible_SetsIsVisibleToFalse()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");
            _controller.Show(options, line);

            // Act
            _controller.Hide();

            // Assert
            _controller.IsVisible.Should().BeFalse();
        }

        [TestMethod]
        public void Hide_WhenVisible_ClearsSelectedOption()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");
            _controller.Show(options, line);

            // Act
            _controller.Hide();

            // Assert
            _controller.SelectedOption.Should().BeNull();
        }

        [TestMethod]
        public void Hide_WhenVisible_ClearsMenu()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");
            _controller.Show(options, line);

            // Act
            _controller.Hide();

            // Assert
            _controller.Menu.Should().BeNull();
        }

        [TestMethod]
        public void Hide_WhenNotVisible_DoesNotThrow()
        {
            // Act
            Action act = () => _controller.Hide();

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region HandleKey Navigation Tests

        [TestMethod]
        public void HandleKey_DownArrow_MovesToNextOption()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");
            _controller.Show(options, line);

            // Act
            _controller.HandleKey(ConsoleKey.DownArrow);

            // Assert
            _controller.SelectedOption.Value.Should().Be("Beta");
        }

        [TestMethod]
        public void HandleKey_UpArrow_MovesToPreviousOption()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");
            _controller.Show(options, line);
            _controller.HandleKey(ConsoleKey.DownArrow); // Move to Beta

            // Act
            _controller.HandleKey(ConsoleKey.UpArrow);

            // Assert
            _controller.SelectedOption.Value.Should().Be("Alpha");
        }

        [TestMethod]
        public void HandleKey_DownArrow_WrapsAroundFromLastToFirst()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta");
            var line = CreateLine("test");
            _controller.Show(options, line);
            _controller.HandleKey(ConsoleKey.DownArrow); // Move to Beta

            // Act
            _controller.HandleKey(ConsoleKey.DownArrow); // Should wrap to Alpha

            // Assert
            _controller.SelectedOption.Value.Should().Be("Alpha");
        }

        [TestMethod]
        public void HandleKey_UpArrow_WrapsAroundFromFirstToLast()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta");
            var line = CreateLine("test");
            _controller.Show(options, line);

            // Act
            _controller.HandleKey(ConsoleKey.UpArrow); // Should wrap to Beta

            // Assert
            _controller.SelectedOption.Value.Should().Be("Beta");
        }

        [TestMethod]
        public void HandleKey_Tab_ReturnsSelected()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta");
            var line = CreateLine("test");
            _controller.Show(options, line);

            // Act
            var result = _controller.HandleKey(ConsoleKey.Tab);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.Selected);
        }

        [TestMethod]
        public void HandleKey_Enter_ReturnsSelected()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta");
            var line = CreateLine("test");
            _controller.Show(options, line);

            // Act
            var result = _controller.HandleKey(ConsoleKey.Enter);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.Selected);
        }

        [TestMethod]
        public void HandleKey_Escape_ReturnsEscape()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta");
            var line = CreateLine("test");
            _controller.Show(options, line);

            // Act
            var result = _controller.HandleKey(ConsoleKey.Escape);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.Dismissed);
        }

        [TestMethod]
        public void HandleKey_WhenNotVisible_ReturnsNoOp()
        {
            // Act
            var result = _controller.HandleKey(ConsoleKey.DownArrow);

            // Assert
            result.Should().Be(AutoCompleteMenuResult.NotHandled);
        }

        #endregion

        #region UpdateFilter Tests

        [TestMethod]
        public void UpdateFilter_WithMatchingOptions_UpdatesMenu()
        {
            // Arrange
            var initialOptions = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");
            _controller.Show(initialOptions, line);

            var filteredOptions = CreateOptions("Beta", "Gamma");

            // Act
            _controller.UpdateFilter(filteredOptions, line);

            // Assert
            _controller.IsVisible.Should().BeTrue();
            _controller.SelectedOption.Value.Should().Be("Beta");
        }

        [TestMethod]
        public void UpdateFilter_WithNoMatchingOptions_HidesMenu()
        {
            // Arrange
            var initialOptions = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");
            _controller.Show(initialOptions, line);

            var emptyOptions = CreateOptions();

            // Act
            _controller.UpdateFilter(emptyOptions, line);

            // Assert
            _controller.IsVisible.Should().BeFalse();
        }

        [TestMethod]
        public void UpdateFilter_PreservesSelectionIfStillAvailable()
        {
            // Arrange
            var initialOptions = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");
            _controller.Show(initialOptions, line);
            _controller.HandleKey(ConsoleKey.DownArrow); // Select Beta

            var filteredOptions = CreateOptions("Beta", "Gamma");

            // Act
            _controller.UpdateFilter(filteredOptions, line);

            // Assert
            _controller.SelectedOption.Value.Should().Be("Beta");
        }

        [TestMethod]
        public void UpdateFilter_WhenNotVisible_DoesNotThrow()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta");
            var line = CreateLine("test");

            // Act
            Action act = () => _controller.UpdateFilter(options, line);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Reset Tests

        [TestMethod]
        public void Reset_WhenVisible_HidesMenuAndClearsState()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var line = CreateLine("test");
            _controller.Show(options, line);

            // Act
            _controller.Reset();

            // Assert
            _controller.IsVisible.Should().BeFalse();
            _controller.SelectedOption.Should().BeNull();
            _controller.Menu.Should().BeNull();
        }

        [TestMethod]
        public void Reset_WhenNotVisible_DoesNotThrow()
        {
            // Act
            Action act = () => _controller.Reset();

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region SetPromptLength Tests

        [TestMethod]
        public void SetPromptLength_SetsPromptLengthForCursorCalculations()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta");
            var line = CreateLine("cmd");
            _controller.SetPromptLength(10);

            // Act
            _controller.Show(options, line);

            // Assert - verify menu appears correctly (no crash)
            _controller.IsVisible.Should().BeTrue();
        }

        #endregion

        #region GetCursorColumn Tests

        [TestMethod]
        public void GetCursorColumn_WithPromptLength_ReturnsCorrectColumn()
        {
            // Arrange
            _controller.SetPromptLength(5);
            var line = CreateLine("test");

            // Act
            var column = _controller.GetCursorColumn(line);

            // Assert - prompt (5) + buffer position (4) + 1 = 10
            column.Should().Be(10);
        }

        [TestMethod]
        public void GetCursorColumn_WithNullLine_ThrowsNullReferenceException()
        {
            // Arrange
            _controller.SetPromptLength(5);

            // Act
            Action act = () => _controller.GetCursorColumn(null);

            // Assert - GetCursorColumn requires a line
            act.Should().Throw<NullReferenceException>();
        }

        #endregion
    }
}
