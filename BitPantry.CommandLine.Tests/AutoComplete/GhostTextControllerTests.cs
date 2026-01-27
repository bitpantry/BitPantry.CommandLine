using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete
{
    /// <summary>
    /// Tests for GhostTextController.
    /// Verifies state management, rendering, and cursor positioning.
    /// </summary>
    [TestClass]
    public class GhostTextControllerTests
    {
        private BitPantry.VirtualConsole.VirtualConsole _virtualConsole;
        private VirtualConsoleAnsiAdapter _console;
        private ConsoleLineMirror _line;
        private GhostTextController _controller;

        [TestInitialize]
        public void Setup()
        {
            _virtualConsole = new BitPantry.VirtualConsole.VirtualConsole(80, 24);
            _console = new VirtualConsoleAnsiAdapter(_virtualConsole);
            _line = new ConsoleLineMirror(_console);
            _controller = new GhostTextController(_console);
        }

        #region State Management Tests

        [TestMethod]
        public void InitialState_IsNotShowing()
        {
            // Assert
            _controller.IsShowing.Should().BeFalse();
            _controller.Text.Should().BeNull();
        }

        [TestMethod]
        public void Show_EnablesIsShowing()
        {
            // Act
            _controller.Show("suggestion", _line);

            // Assert
            _controller.IsShowing.Should().BeTrue();
        }

        [TestMethod]
        public void Show_StoresText()
        {
            // Act
            _controller.Show("connect", _line);

            // Assert
            _controller.Text.Should().Be("connect");
        }

        [TestMethod]
        public void Show_WithEmptyString_HidesGhostText()
        {
            // Arrange
            _controller.Show("something", _line);

            // Act
            _controller.Show("", _line);

            // Assert
            _controller.IsShowing.Should().BeFalse();
            _controller.Text.Should().BeNull();
        }

        [TestMethod]
        public void Show_WithNull_HidesGhostText()
        {
            // Arrange
            _controller.Show("something", _line);

            // Act
            _controller.Show(null, _line);

            // Assert
            _controller.IsShowing.Should().BeFalse();
            _controller.Text.Should().BeNull();
        }

        [TestMethod]
        public void Hide_DisablesIsShowing()
        {
            // Arrange
            _controller.Show("suggestion", _line);

            // Act
            _controller.Hide(_line);

            // Assert
            _controller.IsShowing.Should().BeFalse();
        }

        [TestMethod]
        public void Hide_ClearsText()
        {
            // Arrange
            _controller.Show("suggestion", _line);

            // Act
            _controller.Hide(_line);

            // Assert
            _controller.Text.Should().BeNull();
        }

        [TestMethod]
        public void Text_WhenHidden_ReturnsNull()
        {
            // Arrange - show and then hide
            _controller.Show("suggestion", _line);
            _controller.Hide(_line);

            // Assert
            _controller.Text.Should().BeNull();
        }

        #endregion

        #region Show Tests (combines Set + Render)

        [TestMethod]
        public void Show_WritesTextAfterCursor()
        {
            // Arrange
            _line.Write("conn");

            // Act
            _controller.Show("ect", _line);

            // Assert - "conn" + "ect" should be visible
            _virtualConsole.Should()
                .HaveTextAt(row: 0, column: 0, "connect");
        }

        [TestMethod]
        public void Show_CursorRemainsAtOriginalPosition()
        {
            // Arrange
            _line.Write("conn");
            var cursorBeforeShow = _virtualConsole.CursorColumn;

            // Act
            _controller.Show("ect", _line);

            // Assert - cursor should be at position 4 (after "conn"), not 7
            _virtualConsole.Should()
                .HaveCursorAt(row: 0, column: cursorBeforeShow);
        }

        [TestMethod]
        public void Show_AppliesDimStyle()
        {
            // Arrange
            _line.Write("conn");

            // Act
            _controller.Show("ect", _line);

            // Assert - ghost text should have dim attribute
            _virtualConsole.Should()
                .HaveRangeWithStyle(row: 0, startColumn: 4, length: 3, CellAttributes.Dim);
        }

        [TestMethod]
        public void Show_ReplacingExisting_DoesNotDuplicateText()
        {
            // Arrange
            _line.Write("ser");
            _controller.Show("ver", _line);

            // Act - show again with same text
            _controller.Show("ver", _line);

            // Assert - should still just be "server", not "serververver"
            _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server");
        }

        #endregion

        #region Accept Tests

        [TestMethod]
        public void Accept_CommitsTextToBuffer()
        {
            // Arrange
            _line.Write("conn");
            _controller.Show("ect", _line);

            // Act
            _controller.Accept(_line);

            // Assert - buffer should contain the full word
            _line.Buffer.Should().Be("connect");
        }

        [TestMethod]
        public void Accept_HidesGhostText()
        {
            // Arrange
            _line.Write("conn");
            _controller.Show("ect", _line);

            // Act
            _controller.Accept(_line);

            // Assert
            _controller.IsShowing.Should().BeFalse();
        }

        [TestMethod]
        public void Accept_MovesCursorToEndOfAcceptedText()
        {
            // Arrange
            _line.Write("conn");
            _controller.Show("ect", _line);

            // Act
            _controller.Accept(_line);

            // Assert - cursor should now be at position 7 (end of "connect")
            _virtualConsole.Should()
                .HaveCursorAt(row: 0, column: 7);
        }

        [TestMethod]
        public void Accept_WhenNotShowing_DoesNothing()
        {
            // Arrange
            _line.Write("test");
            var bufferBefore = _line.Buffer;
            var cursorBefore = _virtualConsole.CursorColumn;

            // Act
            _controller.Accept(_line);

            // Assert
            _line.Buffer.Should().Be(bufferBefore);
            _virtualConsole.CursorColumn.Should().Be(cursorBefore);
        }

        [TestMethod]
        public void Accept_ClearsGhostTextFromDisplay()
        {
            // Arrange
            _line.Write("hel");
            _controller.Show("p", _line);

            // Act
            _controller.Accept(_line);

            // Assert - text should be "help" with normal style (not dim)
            // First verify the text
            _virtualConsole.Should().HaveTextAt(row: 0, column: 0, "help");
            
            // The accepted text should NOT have dim style
            var cell = _virtualConsole.GetCell(0, 3);
            cell.Style.Attributes.HasFlag(CellAttributes.Dim).Should().BeFalse(
                "accepted text should not retain dim style");
        }

        #endregion

        #region Hide Tests

        [TestMethod]
        public void Hide_RemovesGhostTextFromDisplay()
        {
            // Arrange
            _line.Write("conn");
            _controller.Show("ect", _line);

            // Act
            _controller.Hide(_line);

            // Assert - ghost text should be cleared (replaced with spaces then trimmed)
            _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("conn");
        }

        [TestMethod]
        public void Hide_PreservesCursorPosition()
        {
            // Arrange
            _line.Write("conn");
            var cursorBefore = _virtualConsole.CursorColumn;
            _controller.Show("ect", _line);

            // Act
            _controller.Hide(_line);

            // Assert
            _virtualConsole.CursorColumn.Should().Be(cursorBefore);
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public void Show_UpdatesExistingGhostText()
        {
            // Arrange
            _controller.Show("first", _line);

            // Act
            _controller.Show("second", _line);

            // Assert
            _controller.Text.Should().Be("second");
            _controller.IsShowing.Should().BeTrue();
        }

        [TestMethod]
        public void Show_WithSpecialCharacters_EscapesProperly()
        {
            // Arrange - text with markup-like characters
            _line.Write("test");

            // Act
            _controller.Show("[value]", _line);

            // Assert - should render literally, not interpret as markup
            _virtualConsole.Should()
                .HaveTextAt(row: 0, column: 4, "[value]");
        }

        [TestMethod]
        public void Show_PreservesBufferContent()
        {
            // Arrange
            _line.Write("original");

            // Act
            _controller.Show("_suffix", _line);

            // Assert - buffer should NOT contain the ghost text
            _line.Buffer.Should().Be("original");
        }

        #endregion
    }
}
