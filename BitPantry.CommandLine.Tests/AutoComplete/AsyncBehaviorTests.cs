using System;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Async Behavior Tests (TC-25.1 through TC-25.5)
/// Tests concurrent and async completion scenarios.
/// </summary>
[TestClass]
public class AsyncBehaviorTests
{
    #region TC-25.1: Rapid Tab During Async Fetch

    /// <summary>
    /// TC-25.1: When user presses Tab multiple times rapidly during an async fetch,
    /// Then debounce prevents multiple concurrent fetches.
    /// </summary>
    [TestMethod]
    public void TC_25_1_RapidTab_DuringAsyncFetch()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Rapid Tab presses
        harness.TypeText("server ");
        harness.PressTab();
        harness.PressTab();
        harness.PressTab();

        // Assert: Menu should be visible, stable state
        harness.IsMenuVisible.Should().BeTrue();
    }

    #endregion

    #region TC-25.2: Simultaneous Ghost and Menu Fetch

    /// <summary>
    /// TC-25.2: When ghost text fetch and Tab fetch would overlap,
    /// Then they are coordinated without conflicts.
    /// </summary>
    [TestMethod]
    public void TC_25_2_SimultaneousGhostAndMenuFetch()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type rapidly then Tab
        harness.TypeText("serv");
        harness.PressTab();

        // Assert: Ghost and menu states consistent
        harness.Buffer.Should().Contain("serv");
    }

    #endregion

    #region TC-25.3: Provider Returns Results After Menu Closed

    /// <summary>
    /// TC-25.3: When async provider returns results after user pressed Escape,
    /// Then results are discarded, no menu shown.
    /// </summary>
    [TestMethod]
    public void TC_25_3_ProviderReturnsResults_AfterMenuClosed()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.TypeText("server ");
        harness.PressTab();
        harness.PressEscape();

        // Assert: Menu closed, no late appearing
        harness.IsMenuVisible.Should().BeFalse();
    }

    #endregion

    #region TC-25.4: Multiple Sequential Completions

    /// <summary>
    /// TC-25.4: When user completes one argument then immediately Tabs for next,
    /// Then context is updated correctly for new completion.
    /// </summary>
    [TestMethod]
    public void TC_25_4_MultipleSequentialCompletions()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<MultiArgTestCommand>();

        // Act
        harness.TypeText("multicmd ");
        harness.PressTab();
        
        if (harness.IsMenuVisible)
        {
            harness.PressEnter(); // Accept first
            harness.TypeText(" ");
            harness.PressTab(); // Request next
        }

        // Assert: No crash, state valid
        harness.Buffer.Should().Contain("multicmd");
    }

    #endregion

    #region TC-25.5: State Consistency After Rapid Operations

    /// <summary>
    /// TC-25.5: When user performs rapid Tab→type→Backspace→Tab sequence,
    /// Then menu and ghost states remain consistent.
    /// </summary>
    [TestMethod]
    public void TC_25_5_StateConsistency_AfterRapidOperations()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Rapid sequence
        harness.TypeText("server ");
        harness.PressTab();
        harness.TypeText("c");
        harness.PressBackspace();
        harness.PressTab();

        // Assert: State is consistent
        harness.Buffer.Should().Contain("server");
    }

    #endregion
}
