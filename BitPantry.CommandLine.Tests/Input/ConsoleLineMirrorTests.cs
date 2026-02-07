using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Tests for ConsoleLineMirror.RenderWithStyles functionality.
/// </summary>
[TestClass]
public class ConsoleLineMirrorTests
{
    private VirtualConsole.VirtualConsole _virtualConsole;
    private VirtualConsoleAnsiAdapter _adapter;
    private ConsoleLineMirror _mirror;

    [TestInitialize]
    public void Setup()
    {
        _virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        _virtualConsole.StrictMode = true;
        _adapter = new VirtualConsoleAnsiAdapter(_virtualConsole);
        _mirror = new ConsoleLineMirror(_adapter);
    }

    // Implements: CV-040
    [TestMethod]
    public void RenderWithStyles_SingleSegment_DisplaysCorrectColor()
    {
        // Arrange - single segment with cyan style
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group) // Cyan
        };

        // Act
        _mirror.RenderWithStyles(segments, 6);

        // Assert - text should display in cyan
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server");
        // Color.Cyan = 256-color index 14
        _virtualConsole.GetCell(0, 0).Style.Foreground256.Should().Be(14, "Group style is Cyan (256-color index 14)");
    }

    // Implements: CV-041
    [TestMethod]
    public void RenderWithStyles_MultipleSegments_DisplaysCorrectColors()
    {
        // Arrange - multiple segments: group (cyan), command (default), argument (yellow)
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),      // Cyan
            new StyledSegment("start", 7, 12, SyntaxColorScheme.Command),    // Default
            new StyledSegment("--port", 13, 19, SyntaxColorScheme.ArgumentName) // Yellow
        };

        // Act
        _mirror.RenderWithStyles(segments, 19);

        // Assert - text should display with correct colors
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("serverstart--port");
        // Color.Cyan = index 14
        _virtualConsole.GetCell(0, 0).Style.Foreground256.Should().Be(14, "server should be Cyan");
        // Default style = null
        _virtualConsole.GetCell(0, 6).Style.Foreground256.Should().BeNull("start should be default");
        // Color.Yellow = index 11
        _virtualConsole.GetCell(0, 11).Style.Foreground256.Should().Be(11, "--port should be Yellow");
    }

    // Implements: CV-042
    [TestMethod]
    public void RenderWithStyles_CursorAtEnd_PositionedCorrectly()
    {
        // Arrange - single segment, cursor at end
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group)
        };

        // Act
        _mirror.RenderWithStyles(segments, 6); // cursor at end

        // Assert - cursor should be at position 6
        _mirror.BufferPosition.Should().Be(6);
        _virtualConsole.CursorColumn.Should().Be(6);
    }

    // Implements: CV-043
    [TestMethod]
    public void RenderWithStyles_CursorMidLine_PositionedCorrectly()
    {
        // Arrange - segments with cursor in the middle
        var segments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),
            new StyledSegment("start", 7, 12, SyntaxColorScheme.Command)
        };

        // Act - cursor at position 3 (middle of "server")
        _mirror.RenderWithStyles(segments, 3);

        // Assert - cursor should be at position 3
        _mirror.BufferPosition.Should().Be(3);
        _virtualConsole.CursorColumn.Should().Be(3);
    }

    // Implements: CV-044
    [TestMethod]
    public void RenderWithStyles_ClearsExistingContent()
    {
        // Arrange - write some initial content
        _mirror.Write("initial content here");
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("initial content here");

        // New shorter content
        var segments = new List<StyledSegment>
        {
            new StyledSegment("hello", 0, 5, SyntaxColorScheme.Group)
        };

        // Act
        _mirror.RenderWithStyles(segments, 5);

        // Assert - old content should be cleared, only new content visible
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("hello");
        _mirror.Buffer.Should().Be("hello");
    }

    // Implements: UX-010
    [TestMethod]
    public void RenderWithStyles_MidLineEdit_CursorReturnsToSamePosition()
    {
        // Arrange - simulate user typing "server start" with cursor at position 10 (middle of "start")
        // Initial typing
        _mirror.Write("server start");
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server start");
        _mirror.BufferPosition.Should().Be(12); // cursor at end after typing

        // Move cursor to position 10 (middle of "start")
        _mirror.MoveToPosition(10);
        _mirror.BufferPosition.Should().Be(10);
        _virtualConsole.CursorColumn.Should().Be(10);

        // Simulate inserting a character 'x' at position 10, making "server stxart"
        // In real scenario, the input would be re-highlighted and re-rendered
        var newSegments = new List<StyledSegment>
        {
            new StyledSegment("server", 0, 6, SyntaxColorScheme.Group),
            new StyledSegment(" ", 6, 7, SyntaxColorScheme.Default),
            new StyledSegment("stxart", 7, 13, SyntaxColorScheme.Default)
        };

        // Act - re-render with cursor at position 11 (after the inserted 'x')
        _mirror.RenderWithStyles(newSegments, 11);

        // Assert - cursor should be at position 11 after re-render
        _mirror.BufferPosition.Should().Be(11);
        _virtualConsole.CursorColumn.Should().Be(11);
        _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server stxart");
    }
}
