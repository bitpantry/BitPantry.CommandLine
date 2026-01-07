using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// FluentAssertions extensions for AutoCompleteTestHarness.
/// Provides assertions for menu state, ghost text, and buffer content.
/// </summary>
public static class HarnessAssertionExtensions
{
    /// <summary>
    /// Returns an assertion object for the AutoCompleteTestHarness.
    /// </summary>
    public static HarnessAssertions Should(this AutoCompleteTestHarness harness)
    {
        return new HarnessAssertions(harness);
    }
}

/// <summary>
/// Provides assertions for AutoCompleteTestHarness state.
/// </summary>
public class HarnessAssertions : ReferenceTypeAssertions<AutoCompleteTestHarness, HarnessAssertions>
{
    /// <summary>
    /// Creates a new HarnessAssertions instance.
    /// </summary>
    public HarnessAssertions(AutoCompleteTestHarness harness) : base(harness)
    {
    }

    /// <inheritdoc/>
    protected override string Identifier => "AutoCompleteTestHarness";

    /// <summary>
    /// Asserts that the autocomplete menu is currently visible.
    /// </summary>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveMenuVisible(
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsMenuVisible)
            .FailWith("Expected menu to be visible{reason}, but it was not.\n{0}",
                Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the autocomplete menu is currently hidden.
    /// </summary>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveMenuHidden(
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!Subject.IsMenuVisible)
            .FailWith("Expected menu to be hidden{reason}, but it was visible.\n{0}",
                Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the currently selected menu item matches the expected text.
    /// </summary>
    /// <param name="expected">The expected selected item text.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveSelectedItem(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.SelectedItem == expected)
            .FailWith("Expected selected item to be {0}{reason}, but was {1}.\n{2}",
                expected, Subject.SelectedItem ?? "(none)", Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the menu has the specified number of items.
    /// </summary>
    /// <param name="expectedCount">The expected number of menu items.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveMenuItemCount(
        int expectedCount,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.MenuItemCount == expectedCount)
            .FailWith("Expected menu to have {0} items{reason}, but had {1}.\n{2}",
                expectedCount, Subject.MenuItemCount, Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the selected index matches the expected value.
    /// </summary>
    /// <param name="expectedIndex">The expected selected index.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveSelectedIndex(
        int expectedIndex,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.SelectedIndex == expectedIndex)
            .FailWith("Expected selected index to be {0}{reason}, but was {1}.\n{2}",
                expectedIndex, Subject.SelectedIndex, Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that ghost text is visible with the specified content.
    /// </summary>
    /// <param name="expected">The expected ghost text.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveGhostText(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.HasGhostText && Subject.GhostText == expected)
            .FailWith("Expected ghost text {0}{reason}, but found {1}.\n{2}",
                expected, Subject.GhostText ?? "(none)", Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that ghost text is visible (any content).
    /// </summary>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveGhostTextVisible(
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.HasGhostText)
            .FailWith("Expected ghost text to be visible{reason}, but it was not.\n{0}",
                Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that ghost text is hidden.
    /// </summary>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveNoGhostText(
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!Subject.HasGhostText)
            .FailWith("Expected no ghost text{reason}, but found {0}.\n{1}",
                Subject.GhostText ?? "(none)", Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the input buffer has the specified content.
    /// </summary>
    /// <param name="expected">The expected buffer content.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveBuffer(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Buffer == expected)
            .FailWith("Expected buffer to be {0}{reason}, but was {1}.\n{2}",
                expected, Subject.Buffer, Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the buffer starts with the specified text.
    /// </summary>
    /// <param name="expected">The expected prefix.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveBufferStartingWith(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Buffer.StartsWith(expected))
            .FailWith("Expected buffer to start with {0}{reason}, but was {1}.\n{2}",
                expected, Subject.Buffer, Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the buffer position (cursor) is at the specified position.
    /// </summary>
    /// <param name="expectedPosition">The expected buffer position.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveBufferPosition(
        int expectedPosition,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.BufferPosition == expectedPosition)
            .FailWith("Expected buffer position to be {0}{reason}, but was {1}.\n{2}",
                expectedPosition, Subject.BufferPosition, Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the buffer cursor is at the end of the buffer.
    /// </summary>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveCursorAtEnd(
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.BufferPosition == Subject.Buffer.Length)
            .FailWith("Expected cursor at end of buffer (position {0}){reason}, but was at position {1}.\n{2}",
                Subject.Buffer.Length, Subject.BufferPosition, Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the menu contains an item with the specified text.
    /// </summary>
    /// <param name="itemText">The text to find in menu items.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveMenuItemContaining(
        string itemText,
        string because = "",
        params object[] becauseArgs)
    {
        var menuItems = Subject.MenuItems;
        var hasItem = menuItems?.Any(i => i.InsertText.Contains(itemText) || i.DisplayText.Contains(itemText)) ?? false;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(hasItem)
            .FailWith("Expected menu to contain item with text {0}{reason}, but items were: {1}.\n{2}",
                itemText,
                menuItems != null ? string.Join(", ", menuItems.Select(i => i.DisplayText)) : "(none)",
                Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the menu does not contain an item with the specified text.
    /// </summary>
    /// <param name="itemText">The text that should not be in menu items.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> NotHaveMenuItemContaining(
        string itemText,
        string because = "",
        params object[] becauseArgs)
    {
        var menuItems = Subject.MenuItems;
        var hasItem = menuItems?.Any(i => i.InsertText.Contains(itemText) || i.DisplayText.Contains(itemText)) ?? false;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!hasItem)
            .FailWith("Expected menu not to contain item with text {0}{reason}, but it did.\n{1}",
                itemText, Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }

    /// <summary>
    /// Asserts that the screen content contains the specified text.
    /// </summary>
    /// <param name="expectedText">The text expected to be on screen.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<HarnessAssertions> HaveScreenContaining(
        string expectedText,
        string because = "",
        params object[] becauseArgs)
    {
        var content = Subject.GetScreenContent();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(content.Contains(expectedText))
            .FailWith("Expected screen to contain {0}{reason}, but it did not.\n{1}",
                expectedText, Subject.GetDiagnostics());

        return new AndConstraint<HarnessAssertions>(this);
    }
}
