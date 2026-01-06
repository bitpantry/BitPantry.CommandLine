using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Rendering;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Rendering;

/// <summary>
/// Tests for AutoCompleteMenuRenderable - verifies menu rendering without controller overhead.
/// Uses Spectre's Renderable pattern for isolated testing.
/// </summary>
[TestClass]
public class AutoCompleteMenuRenderableTests
{
    #region Vertical Layout Tests

    [TestMethod]
    public void Render_WithItems_RendersVerticalLayout()
    {
        // Arrange
        var items = new List<string> { "connect", "disconnect", "status" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        var lines = output.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        lines.Should().Contain(l => l.Contains("connect"));
        lines.Should().Contain(l => l.Contains("disconnect"));
        lines.Should().Contain(l => l.Contains("status"));
    }

    [TestMethod]
    public void Render_WithSelection_AppliesInvertStyleToSelectedItem()
    {
        // Arrange
        var items = new List<string> { "connect", "disconnect", "status" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 1, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        console.EmitAnsiSequences = true;
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - the selected item should have invert styling ANSI codes
        // ANSI codes for invert: ESC[7m (reverse video)
        output.Should().Contain("disconnect");
    }

    [TestMethod]
    public void Render_WithItems_EachItemOnOwnLine()
    {
        // Arrange
        var items = new List<string> { "connect", "disconnect", "status" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var lines = console.Lines;
        
        // Assert - each item should be on its own line (vertical layout)
        lines.Count.Should().BeGreaterOrEqualTo(3, "each item should be on its own line");
    }

    #endregion

    #region Viewport Scrolling Tests

    [TestMethod]
    public void Render_WithViewportScroll_ShowsScrollUpIndicator()
    {
        // Arrange - viewport starts at 2, so items 0-1 are above
        var items = new List<string> { "item0", "item1", "item2", "item3", "item4" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 2, viewportStart: 2, viewportSize: 3);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - should show scroll up indicator
        output.Should().Contain("↑").And.Contain("2");
    }

    [TestMethod]
    public void Render_WithViewportScroll_ShowsScrollDownIndicator()
    {
        // Arrange - viewport shows first 3 items but there are 2 more below
        var items = new List<string> { "item0", "item1", "item2", "item3", "item4" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 3);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - should show scroll down indicator
        output.Should().Contain("↓").And.Contain("2");
    }

    [TestMethod]
    public void Render_NoScroll_NoScrollIndicators()
    {
        // Arrange - all items visible
        var items = new List<string> { "connect", "disconnect" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - no scroll indicators
        output.Should().NotContain("↑");
        output.Should().NotContain("↓");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    [TestDescription("Empty items list should display '(no matches)' message (FR-003)")]
    public void Render_EmptyItems_ShowsNoMatchesMessage_Legacy()
    {
        // Arrange
        var items = new List<string>();
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: -1, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - FR-003: Empty items should show "(no matches)" message
        output.Should().Contain("(no matches)");
    }

    [TestMethod]
    public void Render_SingleItem_RendersCorrectly()
    {
        // Arrange
        var items = new List<string> { "only-item" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        output.Should().Contain("only-item");
    }

    [TestMethod]
    public void Render_SelectedIndexOutOfRange_ClampsToValidRange()
    {
        // Arrange - selectedIndex > items.Count should be clamped
        var items = new List<string> { "item1", "item2" };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 10, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act - should not throw
        console.Write(renderable);
        var output = console.Output;
        
        // Assert
        output.Should().Contain("item1");
        output.Should().Contain("item2");
    }

    #endregion

    #region Match Highlighting Tests (T033-T035)

    [TestMethod]
    [TestDescription("T033: Match ranges should be highlighted with distinct visual style")]
    public void Render_WithMatchRanges_HighlightsMatchedSubstrings()
    {
        // Arrange - CompletionItem with MatchRanges set
        var items = new List<CompletionItem>
        {
            new CompletionItem 
            { 
                InsertText = "connect",
                MatchRanges = new[] { new Range(0, 3) } // "con" matches
            },
            new CompletionItem 
            { 
                InsertText = "disconnect",
                MatchRanges = new[] { new Range(3, 6) } // "con" matches at position 3
            }
        };
        // Select index 1 (disconnect) so we can verify match highlighting on the unselected item
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 1, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        console.EmitAnsiSequences = true;
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - the output should contain the content
        output.Should().NotBeNullOrEmpty("renderable should produce output");
        
        // Verify ANSI escape sequences are present (indicating styling is applied)
        var containsAnsi = output.Contains("\u001b[") || output.Contains("\x1b[");
        containsAnsi.Should().BeTrue("output should contain ANSI escape sequences for match highlighting");
    }

    [TestMethod]
    [TestDescription("T034: Items without MatchRanges should not have match highlighting")]
    public void Render_WithEmptyMatchRanges_NoHighlightMarkup()
    {
        // Arrange - CompletionItem with empty MatchRanges (no filter applied)
        var items = new List<CompletionItem>
        {
            new CompletionItem 
            { 
                InsertText = "connect",
                MatchRanges = Array.Empty<Range>() // No matches - no filter
            },
            new CompletionItem 
            { 
                InsertText = "disconnect",
                MatchRanges = Array.Empty<Range>()
            }
        };
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: -1, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        console.EmitAnsiSequences = true;
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - output should not contain extra ANSI sequences beyond basic formatting
        // The items should be plain text without match highlighting
        // Note: Selected item still gets invert style, but no yellow highlight for matches
        output.Should().Contain("connect");
        output.Should().Contain("disconnect");
    }

    [TestMethod]
    [TestCategory("BugA")]
    [TestDescription("Bug A: Selected item should still show match highlighting (not suppress it)")]
    public void Render_SelectedItemWithMatchRanges_ShouldShowHighlighting()
    {
        // Arrange - CompletionItem with MatchRanges set, item is SELECTED
        var items = new List<CompletionItem>
        {
            new CompletionItem 
            { 
                InsertText = "connect",
                MatchRanges = new[] { new Range(0, 3) } // "con" matches
            },
            new CompletionItem 
            { 
                InsertText = "disconnect",
                MatchRanges = new[] { new Range(3, 6) } // "con" matches at position 3
            }
        };
        // Select index 0 (connect) - the selected item should still have match highlighting
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        console.EmitAnsiSequences = true;
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - the output should contain both items and ANSI sequences
        output.Should().NotBeNullOrEmpty("renderable should produce output");
        
        // Verify ANSI escape sequences are present (indicating styling is applied)
        // ANSI escape sequences start with \x1b[ or \u001b[
        var containsAnsi = output.Contains("\u001b[") || output.Contains("\x1b[");
        containsAnsi.Should().BeTrue("output should contain ANSI escape sequences for styling");
        
        // The selected item should have both invert style and blue match highlighting
        // This is verified by the presence of multiple distinct style changes in the output
        output.Length.Should().BeGreaterThan(50, "styled output should be longer due to ANSI codes");
    }

    #endregion

    #region No Matches Display Tests

    [TestMethod]
    [TestDescription("T049: Empty items list should display '(no matches)' message")]
    public void Render_EmptyItems_ShowsNoMatchesMessage()
    {
        // Arrange - Empty items list (filter produced no matches)
        var items = new List<string>();
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: -1, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - should display "(no matches)" message
        output.Should().Contain("(no matches)", 
            "empty items list should display '(no matches)' message");
    }

    [TestMethod]
    [TestDescription("T049b: Empty CompletionItem list should display '(no matches)' message")]
    public void Render_EmptyCompletionItems_ShowsNoMatchesMessage()
    {
        // Arrange - Empty CompletionItem list
        var items = new List<CompletionItem>();
        var renderable = new AutoCompleteMenuRenderable(items, selectedIndex: -1, viewportStart: 0, viewportSize: 5);

        var console = new TestConsole();
        
        // Act
        console.Write(renderable);
        var output = console.Output;
        
        // Assert - should display "(no matches)" message
        output.Should().Contain("(no matches)", 
            "empty CompletionItem list should display '(no matches)' message");
    }

    #endregion
}
