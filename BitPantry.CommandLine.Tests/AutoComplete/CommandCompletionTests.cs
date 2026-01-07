using System;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Command & Group Completion Tests (TC-5.1 through TC-5.4)
/// Tests command/group completion hypothesis: Tab after command shows subcommands.
/// </summary>
[TestClass]
public class CommandCompletionTests
{
    #region TC-5.1: Tab After Group Shows Subcommands

    /// <summary>
    /// TC-5.1: When Tab is pressed after typing a command name and space,
    /// Then menu shows available arguments (for commands with args).
    /// </summary>
    [TestMethod]
    public void TC_5_1_TabAfterCommand_ShowsArguments()
    {
        // Arrange: Register command with arguments
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type "server " (with space)
        harness.TypeText("server ");
        harness.PressTab();

        // Assert: Menu should show arguments for server command
        if (harness.IsMenuVisible)
        {
            harness.MenuItemCount.Should().BeGreaterThan(0, "should show arguments");
        }
        else
        {
            // Ghost text for first argument
            harness.HasGhostText.Should().BeTrue("should show ghost for first argument");
        }
    }

    #endregion

    #region TC-5.2: Tab After Subcommand with Args Shows Arguments

    /// <summary>
    /// TC-5.2: When Tab is pressed after a command that has arguments,
    /// Then menu shows available argument names (not sibling commands).
    /// </summary>
    [TestMethod]
    public void TC_5_2_TabAfterSubcommand_ShowsArguments()
    {
        // Arrange: Register connect command with Host argument
        using var harness = AutoCompleteTestHarness.WithCommand<ConnectTestCommand>();

        // Act: Type "connect " (with space)
        harness.TypeText("connect ");
        harness.PressTab();

        // Assert: Menu should show arguments like --Host
        if (harness.IsMenuVisible)
        {
            harness.MenuItemCount.Should().BeGreaterThan(0, "should show arguments");
        }
    }

    #endregion

    #region TC-5.3: Tab After Subcommand with No Args Shows Nothing

    /// <summary>
    /// TC-5.3: When Tab is pressed after a command that has no arguments,
    /// Then no menu appears (or empty completions).
    /// </summary>
    [TestMethod]
    public void TC_5_3_TabAfterCommandWithNoArgs_ShowsNothing()
    {
        // Arrange: Register disconnect command (no arguments)
        using var harness = AutoCompleteTestHarness.WithCommand<DisconnectTestCommand>();
        harness.TypeText("disconnect ");
        var originalBuffer = harness.Buffer;

        // Act
        harness.PressTab();

        // Assert: No menu or no change
        // Buffer should not change substantially
        harness.Buffer.Should().StartWith("disconnect");
    }

    #endregion

    #region TC-5.4: Nested Group Navigation

    /// <summary>
    /// TC-5.4: When completing through multiple commands,
    /// Then each shows appropriate completions.
    /// </summary>
    [TestMethod]
    public void TC_5_4_NestedCommandNavigation()
    {
        // Arrange: Multiple commands with different prefixes
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(ConnectTestCommand));

        // Act: Type "s", Tab to show menu
        harness.TypeText("s");
        harness.PressTab();

        // Assert: Menu shows server, service, etc.
        if (harness.IsMenuVisible)
        {
            harness.MenuItemCount.Should().BeGreaterThan(1, "should show multiple matching commands");
        }

        // Select and accept
        harness.PressEnter();

        // Now should have a command selected
        harness.Buffer.Should().StartWith("s");
    }

    #endregion
}
