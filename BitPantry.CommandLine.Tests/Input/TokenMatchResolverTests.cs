using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Tests for TokenMatchResolver class.
/// </summary>
[TestClass]
public class TokenMatchResolverTests
{
    private Mock<ICommandRegistry> _mockRegistry;
    private TokenMatchResolver _resolver;

    [TestInitialize]
    public void Setup()
    {
        _mockRegistry = new Mock<ICommandRegistry>();
        _mockRegistry.Setup(r => r.CaseSensitive).Returns(false);
        _resolver = new TokenMatchResolver(_mockRegistry.Object);
    }

    // Implements: CV-030
    [TestMethod]
    public void ResolveMatch_ExactGroupName_ReturnsUniqueGroup()
    {
        // Arrange - "server" is a group
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _resolver.ResolveMatch("server", null);

        // Assert
        result.Should().Be(TokenMatchResult.UniqueGroup);
    }

    // Implements: CV-031
    [TestMethod]
    public void ResolveMatch_ExactCommandName_ReturnsUniqueCommand()
    {
        // Arrange - "help" is a command (no groups)
        var helpCommand = CreateCommandInfo("help");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo>());
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand });

        // Act
        var result = _resolver.ResolveMatch("help", null);

        // Assert
        result.Should().Be(TokenMatchResult.UniqueCommand);
    }

    // Implements: CV-032
    [TestMethod]
    public void ResolveMatch_UniquePartialGroupMatch_ReturnsUniqueGroup()
    {
        // Arrange - "ser" uniquely matches "server" group
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act
        var result = _resolver.ResolveMatch("ser", null);

        // Assert
        result.Should().Be(TokenMatchResult.UniqueGroup);
    }

    // Implements: CV-033
    [TestMethod]
    public void ResolveMatch_MultipleMatches_ReturnsAmbiguous()
    {
        // Arrange - "s" matches both "server" group and "status" command
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var statusCommand = CreateCommandInfo("status");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { statusCommand });

        // Act
        var result = _resolver.ResolveMatch("s", null);

        // Assert
        result.Should().Be(TokenMatchResult.Ambiguous);
    }

    // Implements: CV-034
    [TestMethod]
    public void ResolveMatch_NoMatches_ReturnsNoMatch()
    {
        // Arrange - "xyz" doesn't match anything
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var helpCommand = CreateCommandInfo("help");
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo> { helpCommand });

        // Act
        var result = _resolver.ResolveMatch("xyz", null);

        // Assert
        result.Should().Be(TokenMatchResult.NoMatch);
    }

    // Implements: CV-035
    [TestMethod]
    public void ResolveMatch_WithGroupContext_FindsCommandInGroup()
    {
        // Arrange - "connect" is a command inside "server" group
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var connectCommand = CreateCommandInfo("connect");
        serverGroup.AddCommand(connectCommand);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act - resolve "connect" within server group context
        var result = _resolver.ResolveMatch("connect", serverGroup);

        // Assert
        result.Should().Be(TokenMatchResult.UniqueCommand);
    }

    // Implements: CV-036
    [TestMethod]
    public void ResolveMatch_SubgroupWithinParentGroup_ReturnsUniqueGroup()
    {
        // Arrange - "files" is a subgroup inside "server" group
        var serverGroup = new GroupInfo("server", "Server commands", null, typeof(object));
        var filesGroup = new GroupInfo("files", "File operations", serverGroup, typeof(object));
        serverGroup.AddChildGroup(filesGroup);
        _mockRegistry.Setup(r => r.RootGroups).Returns(new List<GroupInfo> { serverGroup });
        _mockRegistry.Setup(r => r.RootCommands).Returns(new List<CommandInfo>());

        // Act - resolve "files" within server group context
        var result = _resolver.ResolveMatch("files", serverGroup);

        // Assert
        result.Should().Be(TokenMatchResult.UniqueGroup);
    }

    private static CommandInfo CreateCommandInfo(string name)
    {
        // Use reflection to set internal Name property
        var cmd = new CommandInfo();
        var nameProperty = typeof(CommandInfo).GetProperty("Name");
        nameProperty.SetValue(cmd, name);
        return cmd;
    }
}
