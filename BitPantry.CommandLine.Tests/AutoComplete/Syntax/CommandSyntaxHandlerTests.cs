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
    [InGroup<FilesGroup>]
    [Command]
    private class ListFilesCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test command in config group.
    /// </summary>
    [InGroup<ConfigGroup>]
    [Command]
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
    private static AutoCompleteContext CreateSyntaxContext(string queryString = "", string fullInput = null, int? cursorPosition = null)
    {
        // Use fullInput if provided, otherwise use queryString as fullInput
        var effectiveFullInput = fullInput ?? queryString;
        
        // For syntax completion, we don't have a specific argument - use a dummy context
        // The handler parses FullInput to determine group context
        return new AutoCompleteContext
        {
            QueryString = queryString,
            FullInput = effectiveFullInput,
            CursorPosition = cursorPosition ?? effectiveFullInput.Length,
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

    #region Pipe Support Tests

    /// <summary>
    /// When cursor is after pipe in "server connect sandbox | ",
    /// autocomplete should return root-level groups and commands,
    /// NOT commands from the "server" group in the first pipe segment.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_AfterPipe_ReturnsRootLevelOptions()
    {
        // Arrange - registry has "server" group with "connect" command, plus root "help" command
        var registry = BuildRegistryWithNestedGroups();
        // Also need a root command. Use the combined registry builder.
        var builder = new CommandRegistryBuilder();
        builder.RegisterCommand<ServerConnectCommand>();
        builder.RegisterCommand<ProfileAddCommand>();
        builder.RegisterCommand<HelpRootCommand>();
        var registryWithRoot = builder.Build();

        var handler = new CommandSyntaxHandler(registryWithRoot);

        // Simulate: "server connect sandbox | " cursor at the end (position 26)
        // The second segment is empty - user is starting a new command
        var context = CreateSyntaxContext(
            fullInput: "server connect sandbox | ",
            queryString: "",
            cursorPosition: 26);

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should get root-level options (server group + help command), NOT "connect"
        options.Should().NotBeEmpty();
        var optionValues = options.Select(o => o.Value.ToLowerInvariant()).ToList();
        optionValues.Should().Contain("server", "root group 'server' should appear after pipe");
        optionValues.Should().Contain("helprootcommand", "root command should appear after pipe");
        optionValues.Should().NotContain("connect", "commands from first segment's group should NOT appear after pipe");
    }

    /// <summary>
    /// When cursor is after pipe with partial text "ser",
    /// autocomplete should match against root-level groups, not prior segment's context.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_AfterPipeWithPartialGroup_ReturnsMatchingRootGroups()
    {
        // Arrange
        var registry = BuildRegistryWithNestedGroups();
        var handler = new CommandSyntaxHandler(registry);

        // Simulate: "server connect sandbox | ser" cursor at end
        var context = CreateSyntaxContext(
            fullInput: "server connect sandbox | ser",
            queryString: "ser",
            cursorPosition: 28);

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - "ser" should match "server" root group, not commands inside server group
        options.Should().ContainSingle();
        options.First().Value.ToLowerInvariant().Should().Be("server");
    }

    /// <summary>
    /// When cursor is after pipe with a group path like "| server ",
    /// autocomplete should resolve within the group of the SECOND segment.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_GroupInSecondSegment_ResolvesGroupFromSecondSegment()
    {
        // Arrange
        var builder = new CommandRegistryBuilder();
        builder.RegisterCommand<ServerConnectCommand>();
        builder.RegisterCommand<ProfileAddCommand>();
        builder.RegisterCommand<ProfileRemoveCommand>();
        builder.RegisterCommand<ProfileListCommand>();
        builder.RegisterCommand<HelpRootCommand>();
        var registry = builder.Build();

        var handler = new CommandSyntaxHandler(registry);

        // Simulate: "help | server " cursor at end - second segment has "server " group context
        var context = CreateSyntaxContext(
            fullInput: "help | server ",
            queryString: "",
            cursorPosition: 15);

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return commands/subgroups in "server" group (from second segment)
        var optionValues = options.Select(o => o.Value.ToLowerInvariant()).ToList();
        optionValues.Should().Contain("connect", "server group's command should appear");
        optionValues.Should().Contain("profile", "server group's subgroup should appear");
        optionValues.Should().NotContain("server", "root group should NOT appear when inside server group in second segment");
    }

    #endregion

    #region Nested Group Tests - Bug Fix Validation

    // ============================================================================
    // Issue 1: Missing Child Groups Enumeration
    // When inside a group, CommandSyntaxHandler never adds ChildGroups to options.
    // ============================================================================

    /// <summary>
    /// BUG-001a: When inside a group containing both nested subgroups and commands,
    /// autocomplete should return both subgroups and commands.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_WithinGroupWithSubgroups_ReturnsSubgroupsAndCommands()
    {
        // Arrange
        var registry = BuildRegistryWithNestedGroups();
        var handler = new CommandSyntaxHandler(registry);
        // User typed "server " - inside server group which has both ProfileGroup subgroup and ConnectCommand
        var context = CreateSyntaxContext(fullInput: "server ", queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return BOTH the "profile" subgroup AND the "connect" command
        options.Should().HaveCountGreaterOrEqualTo(2);
        var optionValues = options.Select(o => o.Value.ToLowerInvariant()).ToList();
        optionValues.Should().Contain("profile", "subgroup 'profile' should appear in autocomplete");
        optionValues.Should().Contain("connect", "command 'connect' should appear in autocomplete");
    }

    /// <summary>
    /// BUG-001b: When inside a group containing only nested subgroups (no direct commands),
    /// autocomplete should still return the subgroups.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_WithinGroupWithOnlySubgroups_ReturnsSubgroups()
    {
        // Arrange
        var registry = BuildRegistryWithSubgroupOnly();
        var handler = new CommandSyntaxHandler(registry);
        // User typed "admin " - inside admin group which has ONLY subgroups, no commands
        var context = CreateSyntaxContext(fullInput: "admin ", queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return the "users" subgroup
        options.Should().NotBeEmpty("subgroups should be returned even when no direct commands exist");
        options.Select(o => o.Value.ToLowerInvariant()).Should().Contain("users");
    }

    /// <summary>
    /// BUG-001c: When user types partial text matching a subgroup name inside a parent group,
    /// only the matching subgroup should be returned.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_WithinGroupPartialSubgroupQuery_ReturnsMatchingSubgroup()
    {
        // Arrange
        var registry = BuildRegistryWithNestedGroups();
        var handler = new CommandSyntaxHandler(registry);
        // User typed "server pro" - partial match for "profile" subgroup
        var context = CreateSyntaxContext(fullInput: "server pro", queryString: "pro");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return "profile" subgroup (matches "pro"), NOT "connect" command
        options.Should().ContainSingle();
        options.First().Value.ToLowerInvariant().Should().Be("profile");
    }

    // ============================================================================
    // Issue 2: DetermineCurrentGroup Only Examines First Element
    // For nested paths like "server profile ", only first-level group is detected.
    // ============================================================================

    /// <summary>
    /// BUG-002a: When user types a two-level group path ("server profile "),
    /// autocomplete should return commands from the innermost (child) group.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_NestedGroupPath_ReturnsCommandsInInnerGroup()
    {
        // Arrange
        var registry = BuildRegistryWithNestedGroups();
        var handler = new CommandSyntaxHandler(registry);
        // User typed "server profile " - inside the nested ProfileGroup
        var context = CreateSyntaxContext(fullInput: "server profile ", queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return commands in ProfileGroup (add, remove, list), NOT ServerGroup commands
        options.Should().NotBeEmpty("commands in nested group should be returned");
        var optionValues = options.Select(o => o.Value.ToLowerInvariant()).ToList();
        optionValues.Should().Contain("add", "ProfileGroup's 'add' command should appear");
        optionValues.Should().NotContain("connect", "ServerGroup's 'connect' should NOT appear in nested context");
    }

    /// <summary>
    /// BUG-002b: When user types a 3-level nested path,
    /// autocomplete should correctly resolve to and return children of the deepest group.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_DeeplyNestedGroupPath_ReturnsDeepestGroupChildren()
    {
        // Arrange
        var registry = BuildRegistryWithDeeplyNestedGroups();
        var handler = new CommandSyntaxHandler(registry);
        // User typed "admin users roles " - 3 levels deep
        var context = CreateSyntaxContext(fullInput: "admin users roles ", queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return commands in the deepest "roles" group
        options.Should().NotBeEmpty("commands in deeply nested group should be returned");
        var optionValues = options.Select(o => o.Value.ToLowerInvariant()).ToList();
        optionValues.Should().Contain("assign", "RolesGroup's 'assign' command should appear");
        optionValues.Should().NotContain("create", "UsersGroup's 'create' should NOT appear at roles level");
    }

    /// <summary>
    /// BUG-002c: When user types a nested path plus partial command name,
    /// autocomplete should filter to matching commands in the innermost group.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_NestedGroupWithPartialQuery_FiltersInnerGroupCommands()
    {
        // Arrange
        var registry = BuildRegistryWithNestedGroups();
        var handler = new CommandSyntaxHandler(registry);
        // User typed "server profile a" - partial "a" should match "add" in ProfileGroup
        var context = CreateSyntaxContext(fullInput: "server profile a", queryString: "a");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return only "add" command from ProfileGroup
        options.Should().ContainSingle();
        options.First().Value.ToLowerInvariant().Should().Be("add");
    }

    #endregion

    #region Nested Group Test Fixtures

    /// <summary>
    /// Server group marker - top level group with nested ProfileGroup.
    /// </summary>
    [Group(Name = "server")]
    [API.Description("Server operations")]
    private class ServerGroup
    {
        /// <summary>
        /// Nested profile group for profile management commands.
        /// </summary>
        [Group(Name = "profile")]
        [API.Description("Profile management")]
        public class ProfileGroup { }
    }

    /// <summary>
    /// Admin group marker - has only subgroups, no direct commands.
    /// </summary>
    [Group(Name = "admin")]
    [API.Description("Administration")]
    private class AdminGroup
    {
        /// <summary>
        /// Users subgroup.
        /// </summary>
        [Group(Name = "users")]
        [API.Description("User management")]
        public class UsersGroup
        {
            /// <summary>
            /// Roles subgroup - 3 levels deep.
            /// </summary>
            [Group(Name = "roles")]
            [API.Description("Role management")]
            public class RolesGroup { }
        }
    }

    /// <summary>
    /// Command in ServerGroup (not nested).
    /// </summary>
    [InGroup<ServerGroup>]
    [Command(Name = "connect")]
    private class ServerConnectCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command in nested ProfileGroup.
    /// </summary>
    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "add")]
    private class ProfileAddCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Another command in nested ProfileGroup.
    /// </summary>
    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "remove")]
    private class ProfileRemoveCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Third command in nested ProfileGroup.
    /// </summary>
    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "list")]
    private class ProfileListCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command in AdminGroup.UsersGroup.
    /// </summary>
    [InGroup<AdminGroup.UsersGroup>]
    [Command(Name = "create")]
    private class UserCreateCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command in AdminGroup.UsersGroup.RolesGroup (3 levels deep).
    /// </summary>
    [InGroup<AdminGroup.UsersGroup.RolesGroup>]
    [Command(Name = "assign")]
    private class RoleAssignCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Builds a registry with nested groups: server -> profile.
    /// </summary>
    private static ICommandRegistry BuildRegistryWithNestedGroups()
    {
        var builder = new CommandRegistryBuilder();
        builder.RegisterCommand<ServerConnectCommand>();
        builder.RegisterCommand<ProfileAddCommand>();
        builder.RegisterCommand<ProfileRemoveCommand>();
        builder.RegisterCommand<ProfileListCommand>();
        return builder.Build();
    }

    /// <summary>
    /// Builds a registry where parent group has ONLY subgroups, no direct commands.
    /// </summary>
    private static ICommandRegistry BuildRegistryWithSubgroupOnly()
    {
        var builder = new CommandRegistryBuilder();
        // AdminGroup has no direct commands, only UsersGroup subgroup with commands
        builder.RegisterCommand<UserCreateCommand>();
        return builder.Build();
    }

    /// <summary>
    /// Builds a registry with 3 levels of nesting: admin -> users -> roles.
    /// </summary>
    private static ICommandRegistry BuildRegistryWithDeeplyNestedGroups()
    {
        var builder = new CommandRegistryBuilder();
        builder.RegisterCommand<UserCreateCommand>();
        builder.RegisterCommand<RoleAssignCommand>();
        return builder.Build();
    }

    #endregion
}
