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

    // Implements: UX-004
    [TestMethod]
    public void Highlight_ArgumentAlias_DisplaysYellow()
    {
        // Arrange - "help" command with "-h" alias argument
        var helpCommand = CreateCommandInfo("help");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand });

        var virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        virtualConsole.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(virtualConsole);

        // Act - highlight and render through Spectre
        var segments = _highlighter.Highlight("help -h");
        segments.Should().HaveCount(2, "should have two segments: command and alias argument");
        segments[1].Text.Should().Be("-h");
        
        // Render segments through Spectre's Text class
        foreach (var segment in segments)
        {
            adapter.Write(new Text(segment.Text, segment.Style));
        }

        // Assert - alias argument should display in yellow
        // Spectre's Color.Yellow = 256-color index 11
        virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("help-h");
        // Find the '-h' start position (after "help" which is 4 chars)
        virtualConsole.GetCell(0, 4).Style.Foreground256.Should().Be(11, "Yellow is 256-color index 11");
    }

    // Implements: UX-005
    [TestMethod]
    public void Highlight_ArgumentValue_DisplaysPurple()
    {
        // Arrange - "help" command with positional argument value "myvalue"
        var helpCommand = CreateCommandInfo("help");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand });

        var virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        virtualConsole.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(virtualConsole);

        // Act - highlight and render through Spectre
        var segments = _highlighter.Highlight("help myvalue");
        segments.Should().HaveCount(2, "should have two segments: command and argument value");
        segments[1].Text.Should().Be("myvalue");
        
        // Render segments through Spectre's Text class
        foreach (var segment in segments)
        {
            adapter.Write(new Text(segment.Text, segment.Style));
        }

        // Assert - argument value should display in purple
        // Spectre's Color.Purple = 256-color index 5
        virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("helpmyvalue");
        // Find the 'myvalue' start position (after "help" which is 4 chars)
        virtualConsole.GetCell(0, 4).Style.Foreground256.Should().Be(5, "Purple is 256-color index 5");
    }

    // Implements: UX-006
    [TestMethod]
    public void Highlight_PartialGroupUniqueMatch_DisplaysCyan()
    {
        // Arrange - "ser" is a partial match for "server" group (unique match)
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        var virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        virtualConsole.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(virtualConsole);

        // Act - highlight and render through Spectre
        var segments = _highlighter.Highlight("ser");
        segments.Should().HaveCount(1, "should have exactly one segment for partial group match");
        
        // Render segment through Spectre's Text class
        foreach (var segment in segments)
        {
            adapter.Write(new Text(segment.Text, segment.Style));
        }

        // Assert - partial group match should display in cyan
        // Spectre's Color.Cyan = 256-color index 14
        virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("ser");
        virtualConsole.GetCell(0, 0).Style.Foreground256.Should().Be(14, "Cyan is 256-color index 14");
    }

    // Implements: UX-007
    [TestMethod]
    public void Highlight_AmbiguousPartial_DisplaysDefault()
    {
        // Arrange - "c" matches both "connect" and "config" commands (ambiguous)
        var connectCommand = CreateCommandInfo("connect");
        var configCommand = CreateCommandInfo("config");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { connectCommand, configCommand });

        var virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        virtualConsole.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(virtualConsole);

        // Act - highlight and render through Spectre
        var segments = _highlighter.Highlight("c");
        segments.Should().HaveCount(1, "should have exactly one segment for ambiguous match");
        
        // Render segment through Spectre's Text class
        foreach (var segment in segments)
        {
            adapter.Write(new Text(segment.Text, segment.Style));
        }

        // Assert - ambiguous match should display in default (no color)
        virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("c");
        var cell = virtualConsole.GetCell(0, 0);
        cell.Style.ForegroundColor.Should().BeNull("Default style has no foreground color");
        cell.Style.Foreground256.Should().BeNull("Default style has no 256-color");
    }

    // Implements: UX-008
    [TestMethod]
    public void Highlight_PartialUniqueCommand_DisplaysDefault()
    {
        // Arrange - "con" uniquely matches "connect" command (but commands use default color)
        var connectCommand = CreateCommandInfo("connect");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { connectCommand });

        var virtualConsole = new VirtualConsole.VirtualConsole(80, 24);
        virtualConsole.StrictMode = true;
        var adapter = new VirtualConsoleAnsiAdapter(virtualConsole);

        // Act - highlight and render through Spectre
        var segments = _highlighter.Highlight("con");
        segments.Should().HaveCount(1, "should have exactly one segment for unique command match");
        
        // Render segment through Spectre's Text class
        foreach (var segment in segments)
        {
            adapter.Write(new Text(segment.Text, segment.Style));
        }

        // Assert - unique command match should display in default (commands don't get colored)
        virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("con");
        var cell = virtualConsole.GetCell(0, 0);
        cell.Style.ForegroundColor.Should().BeNull("Command style has no foreground color");
        cell.Style.Foreground256.Should().BeNull("Command style has no 256-color");
    }

    private static CommandInfo CreateCommandInfo(string name)
    {
        var cmd = new CommandInfo();
        typeof(CommandInfo).GetProperty("Name").SetValue(cmd, name);
        return cmd;
    }
}
