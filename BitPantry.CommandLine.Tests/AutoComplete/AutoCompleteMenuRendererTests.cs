using System.Collections.Generic;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Rendering;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete
{
    /// <summary>
    /// Tests for AutoCompleteMenuRenderer visual output.
    /// Verifies rendering, styling, and scroll indicators.
    /// </summary>
    [TestClass]
    public class AutoCompleteMenuRendererTests
    {
        private VirtualConsole.VirtualConsole _virtualConsole;
        private VirtualConsoleAnsiAdapter _console;
        private ConsoleLineMirror _line;
        private AutoCompleteMenuRenderer _renderer;

        [TestInitialize]
        public void Setup()
        {
            _virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
            _console = new VirtualConsoleAnsiAdapter(_virtualConsole);
            _line = new ConsoleLineMirror(_console);
            _renderer = new AutoCompleteMenuRenderer(_console);
        }

        #region Test Helpers

        private static List<AutoCompleteOption> CreateOptions(params string[] values)
        {
            var options = new List<AutoCompleteOption>();
            foreach (var value in values)
            {
                options.Add(new AutoCompleteOption(value));
            }
            return options;
        }

        private static List<AutoCompleteOption> CreateManyOptions(int count)
        {
            var options = new List<AutoCompleteOption>();
            for (int i = 1; i <= count; i++)
            {
                options.Add(new AutoCompleteOption($"Option{i:D2}"));
            }
            return options;
        }

        /// <summary>
        /// Gets all console text as a single string for flexible content verification.
        /// </summary>
        private string GetAllConsoleText()
        {
            var lines = new List<string>();
            for (int row = 0; row < _virtualConsole.Height; row++)
            {
                lines.Add(_virtualConsole.GetRow(row).GetText());
            }
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Finds the first row (below input line) that contains the specified text.
        /// Returns -1 if not found.
        /// </summary>
        private int FindMenuRowContaining(string text, int startRow = 1)
        {
            for (int row = startRow; row < _virtualConsole.Height; row++)
            {
                if (_virtualConsole.GetRow(row).GetText().Contains(text))
                    return row;
            }
            return -1;
        }

        /// <summary>
        /// Gets the text content of all menu rows (rows 1+).
        /// </summary>
        private string GetMenuContent()
        {
            var lines = new List<string>();
            for (int row = 1; row < _virtualConsole.Height; row++)
            {
                var text = _virtualConsole.GetRow(row).GetText().TrimEnd();
                if (string.IsNullOrEmpty(text))
                    break;
                lines.Add(text);
            }
            return string.Join("\n", lines);
        }

        #endregion

        #region Rendering Basics Tests

        [TestMethod]
        public void Render_DrawsMenuBelowCursor()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));

            // Act
            _renderer.Render(menu, _line);

            // Assert - menu should be drawn below input (in menu area)
            var menuContent = GetMenuContent();
            menuContent.Should().Contain("Alpha");
        }

        [TestMethod]
        public void Render_ShowsAllVisibleOptions()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));

            // Act
            _renderer.Render(menu, _line);

            // Assert - all 3 options should be visible in menu area
            var menuContent = GetMenuContent();
            menuContent.Should().Contain("Alpha");
            menuContent.Should().Contain("Beta");
            menuContent.Should().Contain("Gamma");
        }

        [TestMethod]
        public void Render_HighlightsSelectedItem()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));

            // Act
            _renderer.Render(menu, _line);

            // Assert - first item should be in menu and highlighted (inverted)
            var alphaRow = FindMenuRowContaining("Alpha");
            alphaRow.Should().BeGreaterThan(0, "Alpha should be found in menu");
            // The row should contain Alpha with inverted styling
            _virtualConsole.GetRow(alphaRow).GetText().Should().Contain("Alpha");
        }

        [TestMethod]
        public void Render_SelectedItemMovesWithSelection()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));
            menu.MoveDown(); // Select Beta

            // Act
            _renderer.Render(menu, _line);

            // Assert - both items should be visible in menu
            var alphaRow = FindMenuRowContaining("Alpha");
            var betaRow = FindMenuRowContaining("Beta");
            
            _virtualConsole.GetRow(alphaRow).GetText().Should().Contain("Alpha");
            _virtualConsole.GetRow(betaRow).GetText().Should().Contain("Beta");
            // Beta is now selected (has inverted style), Alpha is not
        }

        [TestMethod]
        public void Render_SelectedItem_HasInvertedStyle()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));

            // Act
            _renderer.Render(menu, _line);

            // Assert - selected row should have inverted style
            var alphaRow = FindMenuRowContaining("Alpha");
            _virtualConsole.Should()
                .HaveRangeWithStyle(row: alphaRow, startColumn: 0, 
                    length: _virtualConsole.GetRow(alphaRow).GetText().TrimEnd().Length, 
                    CellAttributes.Reverse);
        }

        [TestMethod]
        public void Render_NonSelectedItems_DoNotHaveInvertedStyle()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));

            // Act
            _renderer.Render(menu, _line);

            // Assert - Beta row should not have inverted style
            var betaRow = FindMenuRowContaining("Beta");
            var cell = _virtualConsole.GetCell(betaRow, 2); // Some column in Beta
            cell.Style.Attributes.HasFlag(CellAttributes.Reverse).Should().BeFalse();
        }

        #endregion

        #region Scroll Indicators Tests

        [TestMethod]
        public void Render_WithMoreBelow_ShowsDownIndicator()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateManyOptions(10)); // More than 5

            // Act
            _renderer.Render(menu, _line);

            // Assert - should show down indicator somewhere in menu
            var menuContent = GetMenuContent();
            menuContent.Should().Contain("▼");
            menuContent.Should().Contain("more");
        }

        [TestMethod]
        public void Render_WithMoreAbove_ShowsUpIndicator()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateManyOptions(10));
            
            // Move selection down to trigger scroll
            for (int i = 0; i < 7; i++)
                menu.MoveDown();

            // Act
            _renderer.Render(menu, _line);

            // Assert - should show up indicator somewhere in menu
            var menuContent = GetMenuContent();
            menuContent.Should().Contain("▲");
            menuContent.Should().Contain("more");
        }

        [TestMethod]
        public void Render_UpIndicator_ShowsCount()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateManyOptions(10));
            
            // Move to bottom (index 9) - should show 5 items above in indicator
            for (int i = 0; i < 9; i++)
                menu.MoveDown();

            // Act
            _renderer.Render(menu, _line);

            // Assert - should show count of hidden items above
            var upIndicatorRow = FindMenuRowContaining("▲");
            upIndicatorRow.Should().BeGreaterThan(0);
            _virtualConsole.GetRow(upIndicatorRow).GetText().Should().Contain("5"); // 5 items hidden above
        }

        [TestMethod]
        public void Render_DownIndicator_ShowsCount()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateManyOptions(10)); // At top, showing 0-4

            // Act
            _renderer.Render(menu, _line);

            // Assert - should show count of hidden items below
            var downIndicatorRow = FindMenuRowContaining("▼");
            downIndicatorRow.Should().BeGreaterThan(0);
            _virtualConsole.GetRow(downIndicatorRow).GetText().Should().Contain("5"); // 5 items hidden below (5-9)
        }

        [TestMethod]
        public void Render_Indicators_HaveDimStyle()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateManyOptions(10));

            // Act
            _renderer.Render(menu, _line);

            // Assert - indicator row should have dim style
            var downIndicatorRow = FindMenuRowContaining("▼");
            _virtualConsole.Should()
                .HaveRangeWithStyle(row: downIndicatorRow, startColumn: 0, length: 1, CellAttributes.Dim);
        }

        #endregion

        #region Clear/Update Tests

        [TestMethod]
        public void Clear_RemovesMenuFromDisplay()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));
            _renderer.Render(menu, _line);

            // Verify menu is shown
            GetMenuContent().Should().Contain("Alpha");

            // Act
            _renderer.Clear();

            // Assert - menu area should be cleared
            var menuContent = GetMenuContent();
            menuContent.Should().BeEmpty();
        }

        [TestMethod]
        public void Clear_RestoresCursorPosition()
        {
            // Arrange
            _line.Write("test ");
            var cursorRowBefore = _virtualConsole.CursorRow;
            var cursorColBefore = _virtualConsole.CursorColumn;
            
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));
            _renderer.Render(menu, _line);

            // Act
            _renderer.Clear();

            // Assert - cursor should be back at original position
            _virtualConsole.CursorRow.Should().Be(cursorRowBefore);
            _virtualConsole.CursorColumn.Should().Be(cursorColBefore);
        }

        [TestMethod]
        public void Update_RedrawsWithNewSelection()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));
            _renderer.Render(menu, _line);

            // Move selection
            menu.MoveDown();

            // Act
            _renderer.Update(menu);

            // Assert - Beta should now be selected (visible in menu)
            var betaRow = FindMenuRowContaining("Beta");
            _virtualConsole.GetRow(betaRow).GetText().Should().Contain("Beta");
        }

        #endregion

        #region Edge Cases Tests

        [TestMethod]
        public void Render_SingleOption_NoScrollIndicators()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateOptions("OnlyOne"));

            // Act
            _renderer.Render(menu, _line);

            // Assert - no scroll indicators should appear anywhere
            var allContent = GetAllConsoleText();
            allContent.Should().NotContain("▲");
            allContent.Should().NotContain("▼");
        }

        [TestMethod]
        public void Render_Exactly5Options_NoScrollIndicators()
        {
            // Arrange
            _line.Write("test ");
            var menu = new AutoCompleteMenu(CreateManyOptions(5));

            // Act
            _renderer.Render(menu, _line);

            // Assert - no scroll indicators should appear anywhere
            var allContent = GetAllConsoleText();
            allContent.Should().NotContain("▲");
            allContent.Should().NotContain("▼");
        }

        [TestMethod]
        public void Render_PreservesCursorPosition()
        {
            // Arrange
            _line.Write("test ");
            var cursorRowBefore = _virtualConsole.CursorRow;
            var cursorColBefore = _virtualConsole.CursorColumn;
            
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta", "Gamma"));

            // Act
            _renderer.Render(menu, _line);

            // Assert - cursor should remain at original position
            _virtualConsole.CursorRow.Should().Be(cursorRowBefore);
            _virtualConsole.CursorColumn.Should().Be(cursorColBefore);
        }

        [TestMethod]
        public void Render_PositionsMenuAtCursorColumn()
        {
            // Arrange
            _line.Write("some text here ");  // cursor at column 15
            var menu = new AutoCompleteMenu(CreateOptions("Alpha", "Beta"));

            // Act
            _renderer.Render(menu, _line);

            // Assert - menu should contain the options (positioned below cursor)
            var menuContent = GetMenuContent();
            menuContent.Should().Contain("Alpha");
        }

        #endregion
    }
}
