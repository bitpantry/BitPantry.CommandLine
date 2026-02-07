using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spectre.Console;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Tests for SyntaxHighlighter class.
/// </summary>
[TestClass]
public class SyntaxHighlighterTests
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

    // Implements: CV-010
    [TestMethod]
    public void Highlight_EmptyString_ReturnsEmptyList()
    {
        // Act
        var result = _highlighter.Highlight(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    // Implements: CV-011
    [TestMethod]
    public void Highlight_NullInput_ReturnsEmptyList()
    {
        // Act
        var result = _highlighter.Highlight(null);

        // Assert
        result.Should().BeEmpty();
    }

    // Implements: CV-012
    [TestMethod]
    public void Highlight_KnownGroup_ReturnsCyanSegment()
    {
        // Arrange - "server" is a known group
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server");

        // Assert - single segment with cyan style (Group style)
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("server");
        result[0].Start.Should().Be(0);
        result[0].End.Should().Be(6);
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
    }

    // Implements: CV-013
    [TestMethod]
    public void Highlight_RootCommand_ReturnsDefaultSegment()
    {
        // Arrange - "help" is a root command (no groups)
        var helpCommand = CreateCommandInfo("help");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand });

        // Act
        var result = _highlighter.Highlight("help");

        // Assert - single segment with Command style (default/plain)
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("help");
        result[0].Start.Should().Be(0);
        result[0].End.Should().Be(4);
        result[0].Style.Should().Be(SyntaxColorScheme.Command);
    }

    // Implements: CV-014
    [TestMethod]
    public void Highlight_GroupCommand_ReturnsTwoSegments()
    {
        // Arrange - "server" is a group, "start" is a command within server
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var startCommand = CreateCommandInfo("start");
        serverGroup.AddCommand(startCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server start");

        // Assert - three segments: group (cyan), whitespace (default), command (default)
        result.Should().HaveCount(3);
        result[0].Text.Should().Be("server");
        result[0].Start.Should().Be(0);
        result[0].End.Should().Be(6);
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be(" ");
        result[1].Style.Should().Be(SyntaxColorScheme.Default);
        result[2].Text.Should().Be("start");
        result[2].Start.Should().Be(7);
        result[2].End.Should().Be(12);
        result[2].Style.Should().Be(SyntaxColorScheme.Command);
    }

    // Implements: CV-015
    [TestMethod]
    public void Highlight_GroupCommandArg_ReturnsThreeSegments()
    {
        // Arrange - "server start --port" 
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var startCommand = CreateCommandInfo("start");
        serverGroup.AddCommand(startCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server start --port");

        // Assert - five segments: group, ws, command, ws, arg
        result.Should().HaveCount(5);
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be(" ");
        result[1].Style.Should().Be(SyntaxColorScheme.Default);
        result[2].Text.Should().Be("start");
        result[2].Style.Should().Be(SyntaxColorScheme.Command);
        result[3].Text.Should().Be(" ");
        result[3].Style.Should().Be(SyntaxColorScheme.Default);
        result[4].Text.Should().Be("--port");
        result[4].Start.Should().Be(13);
        result[4].End.Should().Be(19);
        result[4].Style.Should().Be(SyntaxColorScheme.ArgumentName);
    }

    // Implements: CV-016
    [TestMethod]
    public void Highlight_FullCommandWithArgValue_ReturnsFourSegments()
    {
        // Arrange - "server start --port 8080" 
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var startCommand = CreateCommandInfo("start");
        serverGroup.AddCommand(startCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server start --port 8080");

        // Assert - seven segments: group, ws, command, ws, arg, ws, value
        result.Should().HaveCount(7);
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be(" ");
        result[1].Style.Should().Be(SyntaxColorScheme.Default);
        result[2].Text.Should().Be("start");
        result[2].Style.Should().Be(SyntaxColorScheme.Command);
        result[3].Text.Should().Be(" ");
        result[3].Style.Should().Be(SyntaxColorScheme.Default);
        result[4].Text.Should().Be("--port");
        result[4].Style.Should().Be(SyntaxColorScheme.ArgumentName);
        result[5].Text.Should().Be(" ");
        result[5].Style.Should().Be(SyntaxColorScheme.Default);
        result[6].Text.Should().Be("8080");
        result[6].Start.Should().Be(20);
        result[6].End.Should().Be(24);
        result[6].Style.Should().Be(SyntaxColorScheme.ArgumentValue);
    }

    // Implements: CV-017
    [TestMethod]
    public void Highlight_AliasWithValue_ReturnsFourSegments()
    {
        // Arrange - "server start -p 8080" (using alias -p instead of --port)
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var startCommand = CreateCommandInfo("start");
        serverGroup.AddCommand(startCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server start -p 8080");

        // Assert - seven segments: group, ws, command, ws, alias, ws, value
        result.Should().HaveCount(7);
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be(" ");
        result[1].Style.Should().Be(SyntaxColorScheme.Default);
        result[2].Text.Should().Be("start");
        result[2].Style.Should().Be(SyntaxColorScheme.Command);
        result[3].Text.Should().Be(" ");
        result[3].Style.Should().Be(SyntaxColorScheme.Default);
        result[4].Text.Should().Be("-p");
        result[4].Style.Should().Be(SyntaxColorScheme.ArgumentAlias);
        result[5].Text.Should().Be(" ");
        result[5].Style.Should().Be(SyntaxColorScheme.Default);
        result[6].Text.Should().Be("8080");
        result[6].Style.Should().Be(SyntaxColorScheme.ArgumentValue);
    }

    // Implements: CV-018
    [TestMethod]
    public void Highlight_PartialUniqueMatchingGroup_ReturnsCyanSegment()
    {
        // Arrange - "ser" is a partial match for "server" group (unique match)
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("ser");

        // Assert - single segment with cyan style (Group style) for partial unique match
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("ser");
        result[0].Start.Should().Be(0);
        result[0].End.Should().Be(3);
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
    }

    // Implements: CV-019
    [TestMethod]
    public void Highlight_PartialMatchingNothing_ReturnsDefaultSegment()
    {
        // Arrange - "xyz" doesn't match any group or command
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("xyz");

        // Assert - single segment with default style (no match)
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("xyz");
        result[0].Start.Should().Be(0);
        result[0].End.Should().Be(3);
        result[0].Style.Should().Be(SyntaxColorScheme.Default);
    }

    // Implements: CV-020
    [TestMethod]
    public void Highlight_WithWhitespace_WhitespaceHasDefaultStyle()
    {
        // Arrange - "server start" with space between tokens
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var startCommand = CreateCommandInfo("start");
        serverGroup.AddCommand(startCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server start");

        // Assert - three segments: group, whitespace, command
        result.Should().HaveCount(3);
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be(" ");  // whitespace segment
        result[1].Start.Should().Be(6);
        result[1].End.Should().Be(7);
        result[1].Style.Should().Be(SyntaxColorScheme.Default);
        result[2].Text.Should().Be("start");
        result[2].Style.Should().Be(SyntaxColorScheme.Command);
    }

    private static CommandInfo CreateCommandInfo(string name)
    {
        // Use reflection to set internal Name property
        var cmd = new CommandInfo();
        typeof(CommandInfo).GetProperty("Name").SetValue(cmd, name);
        return cmd;
    }
}
