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

        // Assert - two segments: group (cyan) and command (default)
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("server");
        result[0].Start.Should().Be(0);
        result[0].End.Should().Be(6);
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be("start");
        result[1].Start.Should().Be(7);
        result[1].End.Should().Be(12);
        result[1].Style.Should().Be(SyntaxColorScheme.Command);
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

        // Assert - three segments: group (cyan), command (default), arg (yellow)
        result.Should().HaveCount(3);
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be("start");
        result[1].Style.Should().Be(SyntaxColorScheme.Command);
        result[2].Text.Should().Be("--port");
        result[2].Start.Should().Be(13);
        result[2].End.Should().Be(19);
        result[2].Style.Should().Be(SyntaxColorScheme.ArgumentName);
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

        // Assert - four segments: group (cyan), command (default), arg (yellow), value (purple)
        result.Should().HaveCount(4);
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be("start");
        result[1].Style.Should().Be(SyntaxColorScheme.Command);
        result[2].Text.Should().Be("--port");
        result[2].Style.Should().Be(SyntaxColorScheme.ArgumentName);
        result[3].Text.Should().Be("8080");
        result[3].Start.Should().Be(20);
        result[3].End.Should().Be(24);
        result[3].Style.Should().Be(SyntaxColorScheme.ArgumentValue);
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

        // Assert - four segments: group (cyan), command (default), alias (yellow), value (purple)
        result.Should().HaveCount(4);
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be("start");
        result[1].Style.Should().Be(SyntaxColorScheme.Command);
        result[2].Text.Should().Be("-p");
        result[2].Style.Should().Be(SyntaxColorScheme.ArgumentAlias);
        result[3].Text.Should().Be("8080");
        result[3].Style.Should().Be(SyntaxColorScheme.ArgumentValue);
    }

    private static CommandInfo CreateCommandInfo(string name)
    {
        // Use reflection to set internal Name property
        var cmd = new CommandInfo();
        typeof(CommandInfo).GetProperty("Name").SetValue(cmd, name);
        return cmd;
    }
}
