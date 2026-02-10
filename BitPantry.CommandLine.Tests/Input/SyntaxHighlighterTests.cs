using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spectre.Console;
using System.Collections.Generic;
using System.Linq;

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

    // Implements: CV-021
    [TestMethod]
    public void Highlight_NestedGroups_ReturnsCorrectSegments()
    {
        // Arrange - "server files download" where "server" is a group, "files" is a subgroup, "download" is a command
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var filesGroup = new GroupInfo("files", "File operations", serverGroup, typeof(object));
        serverGroup.AddChildGroup(filesGroup);
        var downloadCommand = CreateCommandInfo("download");
        filesGroup.AddCommand(downloadCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server files download");

        // Assert - five segments: group(cyan), ws, group(cyan), ws, command(default)
        result.Should().HaveCount(5);
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[1].Text.Should().Be(" ");
        result[1].Style.Should().Be(SyntaxColorScheme.Default);
        result[2].Text.Should().Be("files");
        result[2].Style.Should().Be(SyntaxColorScheme.Group, "Nested group 'files' should be styled as Group (cyan)");
        result[3].Text.Should().Be(" ");
        result[3].Style.Should().Be(SyntaxColorScheme.Default);
        result[4].Text.Should().Be("download");
        result[4].Style.Should().Be(SyntaxColorScheme.Command);
    }

    // Implements: UX-016 (CV for quoted values)
    [TestMethod]
    public void Highlight_QuotedValue_ReturnsSinglePurpleSegment()
    {
        // Arrange - "server connect --host \"hello world\"" where quoted value is a single segment
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var connectCommand = CreateCommandInfo("connect");
        serverGroup.AddCommand(connectCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server connect --host \"hello world\"");

        // Assert - segments: group, ws, command, ws, argname, ws, quoted-value
        result.Should().HaveCount(7);
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[2].Text.Should().Be("connect");
        result[2].Style.Should().Be(SyntaxColorScheme.Command);
        result[4].Text.Should().Be("--host");
        result[4].Style.Should().Be(SyntaxColorScheme.ArgumentName);
        result[6].Text.Should().Be("\"hello world\"");
        result[6].Style.Should().Be(SyntaxColorScheme.ArgumentValue, "Quoted value should be styled as ArgumentValue (purple)");
    }

    // Implements: EH-001
    [TestMethod]
    public void Highlight_MalformedInput_ReturnsDefaultStyledSegments()
    {
        // Arrange - input with special characters that don't match anything
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act - malformed input with special characters
        var result = _highlighter.Highlight("@#$%^& !!! ???");

        // Assert - should not throw and should return default-styled segments
        result.Should().NotBeEmpty();
        foreach (var segment in result)
        {
            segment.Style.Should().Be(SyntaxColorScheme.Default, 
                $"Malformed token '{segment.Text}' should have default style");
        }
    }

    // Implements: EH-002
    [TestMethod]
    public void Highlight_WorksWithoutConsole_ReturnsStyledSegments()
    {
        // EH-002: Verify highlighting works independently of console capabilities
        // SyntaxHighlighter produces StyledSegments - console color support is handled at render time
        // This test verifies the highlighter itself is decoupled from console color support
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var startCommand = CreateCommandInfo("start");
        serverGroup.AddCommand(startCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server start");

        // Assert - returns Style objects regardless of console capabilities
        result.Should().HaveCount(3);
        result[0].Style.Should().NotBeNull("Style objects should be created even without color support");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
    }

    // Implements: EH-003
    [TestMethod]
    public void Highlight_LongInput_HandlesWithoutHanging()
    {
        // Arrange - 1000+ character input
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());
        
        var longInput = string.Join(" ", System.Linq.Enumerable.Repeat("token", 250)); // ~1500 chars

        // Act - should complete without hanging
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _highlighter.Highlight(longInput);
        stopwatch.Stop();

        // Assert - completes in reasonable time and returns segments
        result.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Highlighting 1000+ chars should complete in under 1 second");
    }

    // Implements: EH-004
    [TestMethod]
    public void Highlight_InputWithEscapeSequences_HandlesCorrectly()
    {
        // Arrange
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act - input with escape-like sequences
        var result = _highlighter.Highlight("test\\nvalue tab\\there");

        // Assert - should not throw, returns segments
        result.Should().NotBeEmpty();
        // Escape sequences are treated as regular characters in the tokenizer
        var allText = string.Concat(result.Select(s => s.Text));
        allText.Should().Be("test\\nvalue tab\\there");
    }

    // Implements: EH-005
    [TestMethod]
    public void Highlight_WhitespaceOnly_ReturnsDefaultSegments()
    {
        // Arrange
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("   \t  ");

        // Assert - whitespace-only input should return default-styled segments
        result.Should().NotBeEmpty();
        foreach (var segment in result)
        {
            segment.Style.Should().Be(SyntaxColorScheme.Default, "Whitespace should have default style");
        }
    }

    // Implements: EH-006
    [TestMethod]
    public void Highlight_UnclosedQuote_HandlesGracefully()
    {
        // Arrange
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var connectCommand = CreateCommandInfo("connect");
        serverGroup.AddCommand(connectCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act - unclosed quote
        var result = _highlighter.Highlight("server connect --host \"hello world");

        // Assert - should not throw, the unclosed quote portion should be treated as a token
        result.Should().NotBeEmpty();
        // The unclosed quote creates a single token that includes everything from the opening quote
        var lastSegment = result[result.Count - 1];
        lastSegment.Text.Should().Contain("hello world", "Unclosed quote content should be preserved");
        lastSegment.Style.Should().Be(SyntaxColorScheme.ArgumentValue, "Unclosed quote after argument should be argument value style");
    }

    // Implements: EH-007
    [TestMethod]
    public void Highlight_EmptyRegistry_ReturnsAllDefaultStyle()
    {
        // Arrange - empty registry with no commands or groups
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server connect --host value");

        // Assert - all tokens should have default style since nothing matches
        result.Should().NotBeEmpty();
        foreach (var segment in result)
        {
            if (!string.IsNullOrWhiteSpace(segment.Text))
            {
                segment.Style.Should().Be(SyntaxColorScheme.Default, 
                    $"Token '{segment.Text}' should have default style in empty registry");
            }
        }
    }

    // Implements: EH-008
    [TestMethod]
    public void Highlight_ArgumentAfterCommand_StillGetsArgumentStyle()
    {
        // Arrange - command "help" that doesn't take arguments, but user types args anyway
        var helpCommand = CreateCommandInfo("help");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand });

        // Act - type "help --verbose" where "help" doesn't have a --verbose argument
        var result = _highlighter.Highlight("help --verbose");

        // Assert - "help" is command style, "--verbose" gets argument style 
        // (syntax highlighting is visual only, doesn't validate against command definition)
        result.Should().HaveCount(3);
        result[0].Text.Should().Be("help");
        result[0].Style.Should().Be(SyntaxColorScheme.Command);
        result[2].Text.Should().Be("--verbose");
        // After a command is seen, tokens with -- prefix get argument name styling
        result[2].Style.Should().Be(SyntaxColorScheme.ArgumentName,
            "Argument-like token should still get argument styling for visual consistency");
    }

    // Implements: EH-009 (rapid typing = same as paste - final state matters)
    [TestMethod]
    public void Highlight_CalledRepeatedly_ReturnsConsistentResults()
    {
        // Arrange - simulate rapid typing by calling Highlight multiple times with growing input
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var connectCommand = CreateCommandInfo("connect");
        serverGroup.AddCommand(connectCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act - simulate typing "server connect" one character at a time
        IReadOnlyList<StyledSegment> result = null;
        var input = "server connect";
        for (int i = 1; i <= input.Length; i++)
        {
            result = _highlighter.Highlight(input.Substring(0, i));
        }

        // Assert - final result should be properly highlighted
        result.Should().NotBeNull();
        result.Should().HaveCount(3); // group, ws, command
        result[0].Text.Should().Be("server");
        result[0].Style.Should().Be(SyntaxColorScheme.Group);
        result[2].Text.Should().Be("connect");
        result[2].Style.Should().Be(SyntaxColorScheme.Command);
    }

    #region Pipe Support Tests

    // Implements: SH-P1
    [TestMethod]
    public void Highlight_PipedCommands_HighlightsBothCommandsIndependently()
    {
        // Arrange - "help" and "exit" are root commands
        var helpCommand = CreateCommandInfo("help");
        var exitCommand = CreateCommandInfo("exit");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand, exitCommand });

        // Act
        var result = _highlighter.Highlight("help | exit");

        // Assert - segments: help, ws, |, ws, exit
        // Both commands should get Command style, pipe should get Default style
        var nonWhitespace = result.Where(s => !string.IsNullOrWhiteSpace(s.Text)).ToList();
        nonWhitespace.Should().HaveCount(3);
        nonWhitespace[0].Text.Should().Be("help");
        nonWhitespace[0].Style.Should().Be(SyntaxColorScheme.Command);
        nonWhitespace[1].Text.Should().Be("|");
        nonWhitespace[1].Style.Should().Be(SyntaxColorScheme.Default);
        nonWhitespace[2].Text.Should().Be("exit");
        nonWhitespace[2].Style.Should().Be(SyntaxColorScheme.Command);
    }

    // Implements: SH-P2
    [TestMethod]
    public void Highlight_PipedGroupCommands_HighlightsGroupsAndCommandsSeparately()
    {
        // Arrange - "server" is a group with "connect" and "disconnect" commands
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var connectCommand = CreateCommandInfo("connect");
        var disconnectCommand = CreateCommandInfo("disconnect");
        serverGroup.AddCommand(connectCommand);
        serverGroup.AddCommand(disconnectCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("server connect | server disconnect");

        // Assert - each pipe segment should highlight independently with fresh group state
        var nonWhitespace = result.Where(s => !string.IsNullOrWhiteSpace(s.Text)).ToList();
        nonWhitespace.Should().HaveCount(5); // server, connect, |, server, disconnect
        nonWhitespace[0].Text.Should().Be("server");
        nonWhitespace[0].Style.Should().Be(SyntaxColorScheme.Group);
        nonWhitespace[1].Text.Should().Be("connect");
        nonWhitespace[1].Style.Should().Be(SyntaxColorScheme.Command);
        nonWhitespace[2].Text.Should().Be("|");
        nonWhitespace[3].Text.Should().Be("server");
        nonWhitespace[3].Style.Should().Be(SyntaxColorScheme.Group, "Group in second pipe segment should be highlighted as Group");
        nonWhitespace[4].Text.Should().Be("disconnect");
        nonWhitespace[4].Style.Should().Be(SyntaxColorScheme.Command, "Command in second pipe segment should be highlighted as Command");
    }

    // Implements: SH-P3
    [TestMethod]
    public void Highlight_PipedCommandWithArgs_HighlightsArgsSeparately()
    {
        // Arrange - "server connect --host foo | exit"
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var connectCommand = CreateCommandInfo("connect");
        serverGroup.AddCommand(connectCommand);
        var exitCommand = CreateCommandInfo("exit");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { exitCommand });

        // Act
        var result = _highlighter.Highlight("server connect --host foo | exit");

        // Assert - first segment: server(group), connect(cmd), --host(argname), foo(argvalue)
        // pipe, then exit(cmd) in second segment
        var nonWhitespace = result.Where(s => !string.IsNullOrWhiteSpace(s.Text)).ToList();
        nonWhitespace[0].Style.Should().Be(SyntaxColorScheme.Group);
        nonWhitespace[1].Style.Should().Be(SyntaxColorScheme.Command);
        nonWhitespace[2].Text.Should().Be("--host");
        nonWhitespace[2].Style.Should().Be(SyntaxColorScheme.ArgumentName);
        nonWhitespace[3].Text.Should().Be("foo");
        nonWhitespace[3].Style.Should().Be(SyntaxColorScheme.ArgumentValue);
        // After the pipe, "exit" should be highlighted as a command (not an argument value)
        var exitSegment = nonWhitespace.Last();
        exitSegment.Text.Should().Be("exit");
        exitSegment.Style.Should().Be(SyntaxColorScheme.Command, "Command after pipe should not inherit argument mode from previous segment");
    }

    // Implements: SH-P4
    [TestMethod]
    public void Highlight_PipePositions_AreRelativeToFullInput()
    {
        // Arrange
        var helpCommand = CreateCommandInfo("help");
        var exitCommand = CreateCommandInfo("exit");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand, exitCommand });

        // Act - "help | exit"
        //        0123456789A
        var result = _highlighter.Highlight("help | exit");

        // Assert - the "exit" segment's Start/End should be relative to the FULL input string
        var exitSegment = result.Where(s => s.Text == "exit").FirstOrDefault();
        exitSegment.Should().NotBeNull();
        exitSegment.Start.Should().Be(7, "exit starts at position 7 in the full input");
        exitSegment.End.Should().Be(11, "exit ends at position 11 in the full input");
    }

    // Implements: SH-P5
    [TestMethod]
    public void Highlight_PipeOnly_ReturnsDefaultSegment()
    {
        // Arrange
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _highlighter.Highlight("|");

        // Assert - single pipe character gets Default style
        result.Should().NotBeEmpty();
        var pipeSegment = result.Where(s => s.Text == "|").FirstOrDefault();
        pipeSegment.Should().NotBeNull();
        pipeSegment.Style.Should().Be(SyntaxColorScheme.Default);
    }

    // Implements: SH-P6
    [TestMethod]
    public void Highlight_MultiplePipes_EachSegmentHighlightedIndependently()
    {
        // Arrange - "help | server connect | exit" - three pipe segments
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var connectCommand = CreateCommandInfo("connect");
        serverGroup.AddCommand(connectCommand);
        var helpCommand = CreateCommandInfo("help");
        var exitCommand = CreateCommandInfo("exit");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand, exitCommand });

        // Act
        var result = _highlighter.Highlight("help | server connect | exit");

        // Assert - each segment is independently highlighted
        var nonWhitespace = result.Where(s => !string.IsNullOrWhiteSpace(s.Text)).ToList();
        nonWhitespace[0].Text.Should().Be("help");
        nonWhitespace[0].Style.Should().Be(SyntaxColorScheme.Command);
        nonWhitespace[1].Text.Should().Be("|");
        nonWhitespace[2].Text.Should().Be("server");
        nonWhitespace[2].Style.Should().Be(SyntaxColorScheme.Group, "server in second segment should be Group");
        nonWhitespace[3].Text.Should().Be("connect");
        nonWhitespace[3].Style.Should().Be(SyntaxColorScheme.Command, "connect in second segment should be Command");
        nonWhitespace[4].Text.Should().Be("|");
        nonWhitespace[5].Text.Should().Be("exit");
        nonWhitespace[5].Style.Should().Be(SyntaxColorScheme.Command, "exit in third segment should be Command");
    }

    #endregion

    private static CommandInfo CreateCommandInfo(string name)
    {
        // Use reflection to set internal Name property
        var cmd = new CommandInfo();
        typeof(CommandInfo).GetProperty("Name").SetValue(cmd, name);
        return cmd;
    }
}
