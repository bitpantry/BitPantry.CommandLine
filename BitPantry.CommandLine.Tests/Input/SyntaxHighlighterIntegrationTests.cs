using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spectre.Console;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Integration tests for SyntaxHighlighter verifying actual console color output.
/// These tests verify the end-to-end rendering of styled segments.
/// </summary>
[TestClass]
public class SyntaxHighlighterIntegrationTests
{
    private Mock<ICommandRegistry> _mockRegistry;
    private SyntaxHighlighter _highlighter;

    [TestInitialize]
    public void Setup()
    {
        _mockRegistry = new Mock<ICommandRegistry>();
        _mockRegistry.Setup(r => r.CaseSensitive).Returns(false);
        _highlighter = new SyntaxHighlighter(_mockRegistry.Object);
    }

    // Diagnostic test: Does Spectre's Text class render colors through adapter?
    [TestMethod]
    public void SpectreText_ThroughAdapter_RendersColor()
    {
        var virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        virtualConsole.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(virtualConsole);
        adapter.WriteLogEnabled = true;

        // Write styled text using Spectre's Text class
        var cyanStyle = new Style(foreground: Color.Cyan);
        adapter.Write(new Text("hello", cyanStyle));

        // Check what we got
        var cell = virtualConsole.GetCell(0, 0);
        var text = virtualConsole.GetRow(0).GetText().TrimEnd();
        
        text.Should().Be("hello", "text should be written");
        
        // Spectre emits 256-color ANSI codes (38;5;N), not basic 16-color codes
        // Color.Cyan = index 14 in 256-color palette
        cell.Style.Foreground256.Should().Be(14, "Spectre's Color.Cyan is 256-color index 14");
    }

    // Implements: UX-001
    [TestMethod]
    public void Highlight_GroupName_DisplaysCyan()
    {
        // Arrange - "server" is a known group
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        var virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        virtualConsole.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(virtualConsole);

        // Act - highlight and render through Spectre
        var segments = _highlighter.Highlight("server");
        segments.Should().HaveCount(1, "should have exactly one segment");
        
        // Render segment through Spectre's Text class (the real integration path)
        foreach (var segment in segments)
        {
            adapter.Write(new Text(segment.Text, segment.Style));
        }

        // Assert - text should display in cyan
        // Spectre emits 256-color codes (38;5;N). Color.Cyan = index 14
        virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("server");
        virtualConsole.GetCell(0, 0).Style.Foreground256.Should().Be(14, "Cyan is 256-color index 14");
    }

    // Implements: UX-002
    [TestMethod]
    public void Highlight_CommandName_DisplaysDefault()
    {
        // Arrange - "help" is a root command (no groups)
        var helpCommand = CreateCommandInfo("help");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand });

        var virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        virtualConsole.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(virtualConsole);

        // Act - highlight and render through Spectre
        var segments = _highlighter.Highlight("help");
        segments.Should().HaveCount(1, "should have exactly one segment");
        
        // Render segment through Spectre's Text class
        foreach (var segment in segments)
        {
            adapter.Write(new Text(segment.Text, segment.Style));
        }

        // Assert - Style.Plain means no ANSI codes, so default colors
        virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("help");
        var cell = virtualConsole.GetCell(0, 0);
        cell.Style.ForegroundColor.Should().BeNull("Plain style has no foreground color");
        cell.Style.Foreground256.Should().BeNull("Plain style has no 256-color");
    }

    // Implements: UX-003
    [TestMethod]
    public void Highlight_ArgumentFlag_DisplaysYellow()
    {
        // Arrange - "help" command with "--verbose" flag
        var helpCommand = CreateCommandInfo("help");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand });

        var virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        virtualConsole.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(virtualConsole);

        // Act - highlight and render through Spectre
        var segments = _highlighter.Highlight("help --verbose");
        segments.Should().HaveCount(2, "should have two segments: command and argument");
        segments[1].Text.Should().Be("--verbose");
        
        // Render segments through Spectre's Text class
        foreach (var segment in segments)
        {
            adapter.Write(new Text(segment.Text, segment.Style));
        }

        // Assert - argument should display in yellow
        // Spectre's Color.Yellow = 256-color index 11
        virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("help--verbose");
        // Find the '--' start position (after "help" which is 4 chars)
        virtualConsole.GetCell(0, 4).Style.Foreground256.Should().Be(11, "Yellow is 256-color index 11");
    }

    private static CommandInfo CreateCommandInfo(string name)
    {
        var cmd = new CommandInfo();
        typeof(CommandInfo).GetProperty("Name").SetValue(cmd, name);
        return cmd;
    }
}
