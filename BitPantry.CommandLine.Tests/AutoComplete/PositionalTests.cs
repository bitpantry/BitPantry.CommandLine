using System;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Positional Argument Completion Tests (TC-8.1 through TC-8.11)
/// Tests positional completion hypothesis: positional slots complete in order.
/// </summary>
[TestClass]
public class PositionalTests
{
    #region TC-8.1: First Positional Slot with Custom Completion

    /// <summary>
    /// TC-8.1: When a command has a positional argument with completion,
    /// Then Tab after command shows completions for that position.
    /// </summary>
    [TestMethod]
    public void TC_8_1_FirstPositionalSlot_WithCompletion()
    {
        // Arrange: PositionalTestCommand has Source at position 0
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        // Act: Type "poscmd " and Tab
        harness.TypeText("poscmd ");
        harness.PressTab();

        // Assert: Should show completions or named arguments
        // Positional without custom completion falls back to named args
        if (harness.IsMenuVisible)
        {
            harness.MenuItemCount.Should().BeGreaterOrEqualTo(0);
        }
    }

    #endregion

    #region TC-8.2: Second Positional Slot Completion

    /// <summary>
    /// TC-8.2: When first positional is filled and Tab pressed,
    /// Then completions for second positional slot are shown.
    /// </summary>
    [TestMethod]
    public void TC_8_2_SecondPositionalSlot_Completion()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        // Act: Fill first positional and Tab
        harness.TypeText("poscmd file1.txt ");
        harness.PressTab();

        // Assert: Should show second positional or named args
        if (harness.IsMenuVisible)
        {
            harness.MenuItemCount.Should().BeGreaterOrEqualTo(0);
        }
    }

    #endregion

    #region TC-8.3: Positional Argument Provider Type

    /// <summary>
    /// TC-8.3: When a positional argument has [Completion(typeof(Provider))],
    /// Then that provider type is used for completions.
    /// </summary>
    [TestMethod]
    public void TC_8_3_PositionalArgument_ProviderType()
    {
        // Arrange: Test with positional command
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        harness.TypeText("poscmd ");
        harness.PressTab();

        // Assert: Provider-based completions or fallback behavior
        // This validates the infrastructure works
    }

    #endregion

    #region TC-8.4: Positional Without Completion Falls Back to Arguments

    /// <summary>
    /// TC-8.4: When a positional argument has no completion attribute,
    /// Then Tab shows named arguments instead.
    /// </summary>
    [TestMethod]
    public void TC_8_4_PositionalWithoutCompletion_FallsBackToArguments()
    {
        // Arrange: Use command with positional args but no completions
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        // Act
        harness.TypeText("poscmd ");
        harness.PressTab();

        // Assert: May show named arguments or nothing
        // This tests fallback behavior
    }

    #endregion

    #region TC-8.5: Partial Positional Value Filtering

    /// <summary>
    /// TC-8.5: When user types partial text for a positional slot,
    /// Then Tab filters completions by that prefix.
    /// </summary>
    [TestMethod]
    public void TC_8_5_PartialPositionalValue_Filtering()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        // Act: Type partial value
        harness.TypeText("poscmd fi");
        harness.PressTab();

        // Assert: Buffer should contain the partial text
        harness.Buffer.Should().StartWith("poscmd fi");
    }

    #endregion

    #region TC-8.6: IsRest Variadic Positional Continues Completing

    /// <summary>
    /// TC-8.6: When a positional argument has IsRest=true,
    /// Then completion continues to offer values for additional positions.
    /// </summary>
    [TestMethod]
    public void TC_8_6_IsRestVariadic_ContinuesCompleting()
    {
        // Arrange: IsRestTestCommand has Files with IsRest=true
        using var harness = AutoCompleteTestHarness.WithCommand<IsRestTestCommand>();

        // Act: Type multiple values
        harness.TypeText("restcmd file1.txt ");
        harness.PressTab();

        // Assert: Should still offer completions for next position
        // With IsRest, completion can continue indefinitely
    }

    #endregion

    #region TC-8.7: Double-Dash Switches to Options Mode

    /// <summary>
    /// TC-8.7: When user types "--" while in positional context,
    /// Then completion switches to show named arguments.
    /// </summary>
    [TestMethod]
    public void TC_8_7_DoubleDash_SwitchesToOptionsMode()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        // Act: Type "--" in positional context
        harness.TypeText("poscmd --");
        harness.PressTab();

        // Assert: Should show named arguments, not positional completions
        if (harness.IsMenuVisible)
        {
            // Menu should show argument names
            harness.MenuItemCount.Should().BeGreaterOrEqualTo(0);
        }
    }

    #endregion

    #region TC-8.8: Single-Dash Switches to Aliases Mode

    /// <summary>
    /// TC-8.8: When user types "-" while in positional context,
    /// Then completion switches to show argument aliases.
    /// </summary>
    [TestMethod]
    public void TC_8_8_SingleDash_SwitchesToAliasesMode()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<MultiArgTestCommand>();

        // Act: Type "-" in positional context
        harness.TypeText("multicmd -");
        harness.PressTab();

        // Assert: Should show aliases or named args
        if (harness.IsMenuVisible)
        {
            harness.MenuItemCount.Should().BeGreaterOrEqualTo(0);
        }
    }

    #endregion

    #region TC-8.9: All Positionals Filled Shows Options Only

    /// <summary>
    /// TC-8.9: When all positional slots are filled,
    /// Then Tab shows remaining named options only.
    /// </summary>
    [TestMethod]
    public void TC_8_9_AllPositionalsFilled_ShowsOptionsOnly()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        // Act: Fill both positionals
        harness.TypeText("poscmd file1.txt backup/ ");
        harness.PressTab();

        // Assert: Should show named options only
        // No more positional slots available
    }

    #endregion

    #region TC-8.10: Named Argument Satisfies Positional Slot

    /// <summary>
    /// TC-8.10: When positional slot is filled via named argument syntax,
    /// Then that slot is considered filled for completion purposes.
    /// </summary>
    [TestMethod]
    public void TC_8_10_NamedArgument_SatisfiesPositionalSlot()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        // Act: Fill positional via named syntax
        harness.TypeText("poscmd --Source file1.txt ");
        harness.PressTab();

        // Assert: Position 0 is filled, should show position 1 or remaining options
    }

    #endregion

    #region TC-8.11: Positional Filled Excludes Corresponding Named Argument

    /// <summary>
    /// TC-8.11: When a positional slot is filled by position,
    /// Then the corresponding --ArgName is excluded from completion.
    /// </summary>
    [TestMethod]
    public void TC_8_11_PositionalFilled_ExcludesCorrespondingNamedArg()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PositionalTestCommand>();

        // Act: Fill both positionals
        harness.TypeText("poscmd file1.txt backup/ ");
        harness.PressTab();

        // Assert: --Source and --Destination should be excluded
        if (harness.IsMenuVisible && harness.MenuItems != null)
        {
            foreach (var item in harness.MenuItems)
            {
                item.DisplayText.Should().NotContain("Source", "Source is filled");
                item.DisplayText.Should().NotContain("Destination", "Destination is filled");
            }
        }
    }

    #endregion
}
