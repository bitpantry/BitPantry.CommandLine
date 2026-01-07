using System.Linq;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Configuration & Settings Tests (TC-33.1 through TC-33.5)
/// Tests configuration options for autocomplete behavior.
/// Note: Many configuration options require builder-level setup.
/// </summary>
[TestClass]
public class ConfigurationTests
{
    #region TC-33.1: Disable Ghost Text via Configuration

    /// <summary>
    /// TC-33.1: When ghost text is disabled in settings,
    /// Then no ghost text appears.
    /// Note: Requires configuration option in builder.
    /// </summary>
    [TestMethod]
    [Ignore("Requires ghost text configuration option in builder")]
    public void TC_33_1_DisableGhostText_ViaConfiguration()
    {
        // This test requires:
        // using var harness = new AutoCompleteTestHarness(
        //     configure: builder => builder
        //         .RegisterCommand<ServerCommand>()
        //         .ConfigureAutoComplete(o => o.GhostTextEnabled = false));
        //
        // harness.TypeText("serv");
        // harness.HasGhostText.Should().BeFalse();
    }

    #endregion

    #region TC-33.2: Configure Menu Size

    /// <summary>
    /// TC-33.2: When menu page size is configured,
    /// Then menu shows that many items.
    /// Note: Requires menu size configuration option.
    /// </summary>
    [TestMethod]
    [Ignore("Requires menu page size configuration option")]
    public void TC_33_2_ConfigureMenuSize()
    {
        // This test requires configuration option for menu page size
    }

    #endregion

    #region TC-33.3: Configure Debounce Delay

    /// <summary>
    /// TC-33.3: When debounce delay is configured,
    /// Then fetches use that delay.
    /// Note: Requires timing control.
    /// </summary>
    [TestMethod]
    [Ignore("Requires timing control for debounce testing")]
    public void TC_33_3_ConfigureDebounceDelay()
    {
        // This test requires timing control
    }

    #endregion

    #region TC-33.4: Case Sensitivity Configuration

    /// <summary>
    /// TC-33.4: When case sensitivity is enabled,
    /// Then matching is case-sensitive.
    /// </summary>
    [TestMethod]
    public void TC_33_4_CaseSensitivity_Default()
    {
        // Arrange: Default configuration (case-insensitive)
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand));

        // Act: Type lowercase to match commands
        harness.TypeText("S");
        harness.PressTab();
        
        // Assert: Should match (case-insensitive by default)
        harness.IsMenuVisible.Should().BeTrue("should find matches case-insensitively");
        var items = harness.MenuItems!.Select(m => m.InsertText).ToList();
        items.Count.Should().BeGreaterThan(0, "should have case-insensitive matches");
    }

    #endregion

    #region TC-33.5: History Limit Configuration

    /// <summary>
    /// TC-33.5: When history limit is set,
    /// Then only that many entries are kept.
    /// Note: Requires history limit configuration.
    /// </summary>
    [TestMethod]
    [Ignore("Requires history limit configuration and cross-session testing")]
    public void TC_33_5_HistoryLimitConfiguration()
    {
        // This test requires configuration option for history limit
        // and ability to execute many commands
    }

    #endregion

    #region Default Configuration Works

    /// <summary>
    /// Verify that default configuration produces working autocomplete.
    /// </summary>
    [TestMethod]
    public void DefaultConfiguration_Works()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Basic autocomplete
        harness.TypeText("serv");
        harness.PressTab();
        
        // Assert: Autocomplete works with defaults
        var hasCompletion = harness.IsMenuVisible || harness.HasGhostText;
        hasCompletion.Should().BeTrue("default configuration should provide completions");
    }

    #endregion
}
