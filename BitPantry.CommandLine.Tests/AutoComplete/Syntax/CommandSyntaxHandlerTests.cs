using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.AutoComplete.Syntax;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete.Syntax;

/// <summary>
/// Tests for CommandSyntaxHandler.
/// </summary>
[TestClass]
public class CommandSyntaxHandlerTests
{
    #region Spec 008-autocomplete-extensions

    /// <summary>
    /// Implements: 008:SYN-001
    /// When cursor is at command position with partial group name typed,
    /// matching group names are suggested.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_PartialGroupName_ReturnsMatchingGroups()
    {
        // Arrange
        var registry = BuildRegistryWithGroups();
        var handler = new CommandSyntaxHandler(registry);
        var context = CreateSyntaxContext(queryString: "fil"); // partial match for "files"

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should match "files" group
        options.Should().ContainSingle();
        options.Select(o => o.Value).Should().Contain("files");
    }

    /// <summary>
    /// Implements: 008:SYN-001 (empty query variant)
    /// When query is empty at command position, all groups are suggested.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_EmptyQuery_ReturnsAllGroups()
    {
        // Arrange
        var registry = BuildRegistryWithGroups();
        var handler = new CommandSyntaxHandler(registry);
        var context = CreateSyntaxContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return all groups
        options.Should().HaveCountGreaterOrEqualTo(2);
        options.Select(o => o.Value).Should().Contain(new[] { "files", "config" });
    }

    /// <summary>
    /// Implements: 008:SYN-002
    /// When user has typed a group name and cursor is at command position within that group,
    /// commands within that group are suggested.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_WithinGroup_ReturnsCommandsInGroup()
    {
        // Arrange
        var registry = BuildRegistryWithGroups();
        var handler = new CommandSyntaxHandler(registry);
        // Simulate user typing "files " - the group name followed by space for command
        var context = CreateSyntaxContext(fullInput: "files ", queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return commands in "files" group, not other groups' commands
        options.Should().ContainSingle();
        // Command name derived from class name - case-insensitive check
        options.Select(o => o.Value.ToLowerInvariant()).Should().Contain("listfilescommand");
    }

    /// <summary>
    /// Implements: 008:SYN-003
    /// When at root level (no group context), root-level commands are suggested alongside groups.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_AtRootLevel_ReturnsRootCommands()
    {
        // Arrange
        var registry = BuildRegistryWithRootCommands();
        var handler = new CommandSyntaxHandler(registry);
        var context = CreateSyntaxContext(queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return root-level commands
        options.Select(o => o.Value.ToLowerInvariant()).Should().Contain("helprootcommand");
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test command group marker for "files".
    /// </summary>
    [Group(Name = "files")]
    [API.Description("File operations")]
    private class FilesGroup { }

    /// <summary>
    /// Test command group marker for "config".
    /// </summary>
    [Group(Name = "config")]
    [API.Description("Configuration operations")]
    private class ConfigGroup { }

    /// <summary>
    /// Test command in files group.
    /// </summary>
    [Command(Group = typeof(FilesGroup))]
    private class ListFilesCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test command in config group.
    /// </summary>
    [Command(Group = typeof(ConfigGroup))]
    private class ShowConfigCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Root-level command (no group).
    /// </summary>
    [Command]
    private class HelpRootCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Builds a registry with test command groups.
    /// </summary>
    private static ICommandRegistry BuildRegistryWithGroups()
    {
        var builder = new CommandRegistryBuilder();
        builder.RegisterCommand<ListFilesCommand>();
        builder.RegisterCommand<ShowConfigCommand>();
        return builder.Build();
    }

    /// <summary>
    /// Builds a registry with root-level commands (no group).
    /// </summary>
    private static ICommandRegistry BuildRegistryWithRootCommands()
    {
        var builder = new CommandRegistryBuilder();
        builder.RegisterCommand<HelpRootCommand>();
        return builder.Build();
    }

    /// <summary>
    /// Creates an AutoCompleteContext for command syntax testing.
    /// </summary>
    private static AutoCompleteContext CreateSyntaxContext(string queryString = "", string fullInput = null)
    {
        // Use fullInput if provided, otherwise use queryString as fullInput
        var effectiveFullInput = fullInput ?? queryString;
        
        // For syntax completion, we don't have a specific argument - use a dummy context
        // The handler parses FullInput to determine group context
        return new AutoCompleteContext
        {
            QueryString = queryString,
            FullInput = effectiveFullInput,
            CursorPosition = effectiveFullInput.Length,
            ArgumentInfo = CreateDummyArgumentInfo(),
            ProvidedValues = new Dictionary<ArgumentInfo, string>(),
            CommandInfo = CreateDummyCommandInfo()
        };
    }

    /// <summary>
    /// Creates a dummy ArgumentInfo for syntax contexts where argument isn't relevant.
    /// </summary>
    private static ArgumentInfo CreateDummyArgumentInfo()
    {
        // Use reflection to get a real ArgumentInfo from a simple command
        var commandInfo = CommandReflection.Describe<DummyCommand>();
        return commandInfo.Arguments.First();
    }

    /// <summary>
    /// Creates a dummy CommandInfo for syntax contexts.
    /// </summary>
    private static CommandInfo CreateDummyCommandInfo()
    {
        return CommandReflection.Describe<DummyCommand>();
    }

    /// <summary>
    /// Dummy command for creating context objects.
    /// </summary>
    [Command]
    private class DummyCommand : CommandBase
    {
        [Argument]
        public string Value { get; set; } = "";

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion
}
