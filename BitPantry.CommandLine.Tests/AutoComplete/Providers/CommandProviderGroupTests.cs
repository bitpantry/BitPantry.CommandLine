using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using System.Linq;
using System.Threading.Tasks;
using CmdDescription = BitPantry.CommandLine.API.DescriptionAttribute;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Providers;

/// <summary>
/// Tests for <see cref="CommandCompletionProvider"/> - group completion and trailing space scenarios.
/// These tests verify that typing "group " (with trailing space) shows commands within the group.
/// </summary>
[TestClass]
public class CommandProviderGroupTests
{
    #region Test Commands and Groups

    [Group]
    [CmdDescription("Server management commands")]
    public class ServerGroup
    {
        [Group]
        [CmdDescription("Profile management")]
        public class ProfileGroup { }
    }

    [Command(Group = typeof(ServerGroup), Name = "connect")]
    [CmdDescription("Connect to a server")]
    public class ConnectCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Group = typeof(ServerGroup), Name = "disconnect")]
    [CmdDescription("Disconnect from server")]
    public class DisconnectCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Group = typeof(ServerGroup), Name = "status")]
    [CmdDescription("Show connection status")]
    public class StatusCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "add")]
    [CmdDescription("Add a profile")]
    public class ProfileAddCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "remove")]
    [CmdDescription("Remove a profile")]
    public class ProfileRemoveCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "list")]
    [CmdDescription("List profiles")]
    public class ProfileListCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "help")]
    [CmdDescription("Show help")]
    public class HelpCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "version")]
    [CmdDescription("Show version")]
    public class VersionCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Setup

    private CommandRegistry CreateTestRegistry()
    {
        var registry = new CommandRegistry();
        
        // Register groups
        registry.RegisterGroup<ServerGroup>();
        registry.RegisterGroup<ServerGroup.ProfileGroup>();
        
        // Register commands
        registry.RegisterCommand<ConnectCommand>();
        registry.RegisterCommand<DisconnectCommand>();
        registry.RegisterCommand<StatusCommand>();
        registry.RegisterCommand<ProfileAddCommand>();
        registry.RegisterCommand<ProfileRemoveCommand>();
        registry.RegisterCommand<ProfileListCommand>();
        registry.RegisterCommand<HelpCommand>();
        registry.RegisterCommand<VersionCommand>();
        
        return registry;
    }

    #endregion

    #region Trailing Space - Group Completion

    [TestMethod]
    [TestDescription("GRP001: 'server ' (with space) shows commands within server group")]
    public async Task GetCompletions_GroupWithTrailingSpace_ShowsGroupContents()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "server ",
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().NotBeEmpty("should show contents of server group");
        
        // Should show the profile subgroup
        result.Items.Should().Contain(i => i.InsertText == "profile" && i.Kind == CompletionItemKind.CommandGroup,
            "should show 'profile' subgroup");
        
        // Should show commands in server group
        result.Items.Should().Contain(i => i.InsertText == "connect" && i.Kind == CompletionItemKind.Command,
            "should show 'connect' command");
        result.Items.Should().Contain(i => i.InsertText == "disconnect" && i.Kind == CompletionItemKind.Command,
            "should show 'disconnect' command");
        result.Items.Should().Contain(i => i.InsertText == "status" && i.Kind == CompletionItemKind.Command,
            "should show 'status' command");
        
        // Should NOT show root-level commands
        result.Items.Should().NotContain(i => i.InsertText == "help",
            "should not show root-level 'help' command");
        result.Items.Should().NotContain(i => i.InsertText == "version",
            "should not show root-level 'version' command");
    }

    [TestMethod]
    [TestDescription("GRP002: 'server profile ' (nested group with space) shows commands within profile subgroup")]
    public async Task GetCompletions_NestedGroupWithTrailingSpace_ShowsNestedGroupContents()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "server profile ",
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().NotBeEmpty("should show contents of profile subgroup");
        
        // Should show commands in profile subgroup
        result.Items.Should().Contain(i => i.InsertText == "add" && i.Kind == CompletionItemKind.Command,
            "should show 'add' command");
        result.Items.Should().Contain(i => i.InsertText == "remove" && i.Kind == CompletionItemKind.Command,
            "should show 'remove' command");
        result.Items.Should().Contain(i => i.InsertText == "list" && i.Kind == CompletionItemKind.Command,
            "should show 'list' command");
        
        // Should NOT show parent group commands
        result.Items.Should().NotContain(i => i.InsertText == "connect",
            "should not show parent 'connect' command");
        result.Items.Should().NotContain(i => i.InsertText == "status",
            "should not show parent 'status' command");
    }

    [TestMethod]
    [TestDescription("GRP003: 'server' (no space) filters root groups by prefix")]
    public async Task GetCompletions_GroupNameNoSpace_FiltersRootByPrefix()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "server",
            PartialValue = "server"
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        // Should show the server group as a match
        result.Items.Should().Contain(i => i.InsertText == "server" && i.Kind == CompletionItemKind.CommandGroup,
            "should show 'server' group as match");
        
        // Should NOT show commands inside server group (we're filtering, not navigating)
        result.Items.Should().NotContain(i => i.InsertText == "connect",
            "should not show 'connect' - we're filtering root, not inside group");
    }

    [TestMethod]
    [TestDescription("GRP004: 'server c' (partial command within group) filters group contents")]
    public async Task GetCompletions_PartialCommandInGroup_FiltersGroupContents()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "server c",
            PartialValue = "c"
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        // Should show 'connect' as it starts with 'c'
        result.Items.Should().Contain(i => i.InsertText == "connect",
            "should show 'connect' as it starts with 'c'");
        
        // Should NOT show 'disconnect' as it doesn't start with 'c'
        result.Items.Where(i => i.InsertText == "disconnect").Should().BeEmpty(
            "should not show 'disconnect' - doesn't start with 'c'");
        
        // Should NOT show 'status' as it doesn't start with 'c'
        result.Items.Where(i => i.InsertText == "status").Should().BeEmpty(
            "should not show 'status' - doesn't start with 'c'");
    }

    [TestMethod]
    [TestDescription("GRP005: 'server profile a' (partial command in nested group) filters subgroup contents")]
    public async Task GetCompletions_PartialCommandInNestedGroup_FiltersSubgroupContents()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "server profile a",
            PartialValue = "a"
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        // Should show 'add' as it starts with 'a'
        result.Items.Should().Contain(i => i.InsertText == "add",
            "should show 'add' as it starts with 'a'");
        
        // Should NOT show 'remove' or 'list'
        result.Items.Where(i => i.InsertText == "remove").Should().BeEmpty();
        result.Items.Where(i => i.InsertText == "list").Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    [TestDescription("GRP006: Empty input shows all root commands and groups")]
    public async Task GetCompletions_EmptyInput_ShowsRootCommandsAndGroups()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Empty,
            InputText = "",
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().NotBeEmpty();
        
        // Should show root commands
        result.Items.Should().Contain(i => i.InsertText == "help");
        result.Items.Should().Contain(i => i.InsertText == "version");
        
        // Should show root groups
        result.Items.Should().Contain(i => i.InsertText == "server" && i.Kind == CompletionItemKind.CommandGroup);
    }

    [TestMethod]
    [TestDescription("GRP007: Multiple spaces are handled correctly")]
    public async Task GetCompletions_MultipleSpaces_HandleCorrectly()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "server  ",  // Two spaces
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        // Should still recognize 'server' as a group and show its contents
        result.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(i => i.InsertText == "connect");
    }

    [TestMethod]
    [TestDescription("GRP008: Unknown group with trailing space returns empty results (not root completions)")]
    public async Task GetCompletions_UnknownGroupWithSpace_ReturnsEmpty()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "nonexistent ",
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        // When "nonexistent" is not a valid group or command, we should return empty results
        // This prevents confusing UX where typing gibberish shows root completions
        result.Items.Should().BeEmpty("unrecognized group/command should not show completions");
    }

    [TestMethod]
    [TestDescription("GRP009: Groups are shown before commands in results")]
    public async Task GetCompletions_GroupsOrderedBeforeCommands()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "server ",
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        var items = result.Items.ToList();
        var profileIndex = items.FindIndex(i => i.InsertText == "profile");
        var connectIndex = items.FindIndex(i => i.InsertText == "connect");
        
        profileIndex.Should().BeLessThan(connectIndex, 
            "groups should appear before commands in completion list");
    }

    [TestMethod]
    [TestDescription("GRP010: Case-insensitive group matching")]
    public async Task GetCompletions_CaseInsensitiveGroupMatching()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "SERVER ",  // Uppercase
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().NotBeEmpty("group matching should be case-insensitive");
        result.Items.Should().Contain(i => i.InsertText == "connect");
    }

    [TestMethod]
    [TestDescription("GRP011: Partial group name filters correctly")]
    public async Task GetCompletions_PartialGroupName_FiltersCorrectly()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "ser",
            PartialValue = "ser"
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().Contain(i => i.InsertText == "server",
            "should show 'server' group matching 'ser' prefix");
        result.Items.Should().NotContain(i => i.InsertText == "help",
            "should not show 'help' - doesn't match 'ser'");
    }

    [TestMethod]
    [TestDescription("GRP012: Command completion within group includes descriptions")]
    public async Task GetCompletions_GroupContents_IncludeDescriptions()
    {
        // Arrange
        var registry = CreateTestRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            InputText = "server ",
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context);

        // Assert
        var connectItem = result.Items.FirstOrDefault(i => i.InsertText == "connect");
        connectItem.Should().NotBeNull();
        connectItem.Description.Should().NotBeNullOrEmpty("commands should have descriptions");
    }

    #endregion
}
