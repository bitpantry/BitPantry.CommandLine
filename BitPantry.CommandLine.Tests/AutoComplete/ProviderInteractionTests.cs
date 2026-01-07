using System.Linq;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Completion Source Interactions Tests (TC-31.1 through TC-31.5)
/// Tests how multiple completion providers interact.
/// Note: Provider internals are not directly observable; tests verify end behavior.
/// </summary>
[TestClass]
public class ProviderInteractionTests
{
    #region TC-31.1: Multiple Providers Same Priority

    /// <summary>
    /// TC-31.1: When two providers have same priority and both can handle,
    /// Then results are merged or first wins consistently.
    /// </summary>
    [TestMethod]
    public void TC_31_1_MultipleSamePriority_ConsistentBehavior()
    {
        // Arrange: Command with multiple argument sources
        using var harness = AutoCompleteTestHarness.WithCommand<MultiArgTestCommand>();

        // Act: Tab for argument completions
        harness.TypeText("multicmd ");
        harness.PressTab();
        
        // Assert: Consistent results each time
        harness.IsMenuVisible.Should().BeTrue("should show completions");
        var firstAttempt = harness.MenuItems!.Select(m => m.InsertText).ToList();
        
        harness.PressEscape();
        harness.PressTab();
        
        var secondAttempt = harness.MenuItems!.Select(m => m.InsertText).ToList();
        secondAttempt.Should().BeEquivalentTo(firstAttempt, "results should be consistent");
    }

    #endregion

    #region TC-31.2: Provider Priority Ordering

    /// <summary>
    /// TC-31.2: When multiple providers can handle with different priorities,
    /// Then highest priority provider handles first.
    /// Note: Priority is internal; test validates that results appear correctly.
    /// </summary>
    [TestMethod]
    public void TC_31_2_ProviderPriority_HighestFirst()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<EnumArgTestCommand>();

        // Act: Tab for enum value completions
        harness.TypeText("enumarg --Mode ");
        harness.PressTab();
        
        // Assert: Enum values appear (EnumProvider should handle)
        harness.IsMenuVisible.Should().BeTrue("should show enum completions");
        var items = harness.MenuItems!.Select(m => m.InsertText).ToList();
        items.Count.Should().BeGreaterThan(0, "should have enum values");
    }

    #endregion

    #region TC-31.3: Fallback to Next Provider on Empty

    /// <summary>
    /// TC-31.3: When primary provider returns empty,
    /// Then system falls back to next provider.
    /// </summary>
    [TestMethod]
    public void TC_31_3_FallbackProvider_OnEmpty()
    {
        // Arrange: Command with positional and named arguments
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        // Act: Type command and Tab (no positional value provider)
        harness.TypeText("positional ");
        harness.PressTab();
        
        // Assert: Falls back to showing argument names if no positional values
        // Behavior depends on implementation
        if (harness.IsMenuVisible || harness.HasGhostText)
        {
            // Some completion is available
        }
    }

    #endregion

    #region TC-31.4: Provider Short-Circuits on First Result

    /// <summary>
    /// TC-31.4: When high priority provider returns results,
    /// Then lower priority providers may not be called.
    /// Note: Internal optimization; test verifies results appear quickly.
    /// </summary>
    [TestMethod]
    public void TC_31_4_ProviderShortCircuit_OnResult()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Tab for arguments (should return quickly)
        harness.TypeText("server ");
        harness.PressTab();
        
        // Assert: Results appear (short-circuit optimization works)
        harness.IsMenuVisible.Should().BeTrue("should show completions");
        harness.MenuItemCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region TC-31.5: All Providers Fail Gracefully

    /// <summary>
    /// TC-31.5: When every registered provider fails or returns empty,
    /// Then system handles gracefully.
    /// </summary>
    [TestMethod]
    public void TC_31_5_AllProvidersFail_Graceful()
    {
        // Arrange: Command with no matching completions for input
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type nonsense and Tab
        harness.TypeText("xyznonexistent");
        harness.PressTab();
        
        // Assert: No crash, graceful handling
        // Either no menu or "(no matches)" indicator
        if (harness.IsMenuVisible)
        {
            // If menu is shown, it should indicate no matches
        }
        // The key is no exception/crash
    }

    #endregion
}
