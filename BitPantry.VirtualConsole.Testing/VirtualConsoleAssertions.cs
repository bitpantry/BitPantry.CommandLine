using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// FluentAssertions extensions for VirtualConsole.
/// Provides assertions for screen content and cell styles.
/// </summary>
public static class VirtualConsoleAssertionExtensions
{
    /// <summary>
    /// Returns an assertion object for the VirtualConsole.
    /// </summary>
    public static VirtualConsoleAssertions Should(this VirtualConsole console)
    {
        return new VirtualConsoleAssertions(console);
    }
}

/// <summary>
/// Provides assertions for VirtualConsole state.
/// </summary>
public class VirtualConsoleAssertions : ReferenceTypeAssertions<VirtualConsole, VirtualConsoleAssertions>
{
    /// <summary>
    /// Creates a new VirtualConsoleAssertions instance.
    /// </summary>
    public VirtualConsoleAssertions(VirtualConsole console) : base(console)
    {
    }

    /// <inheritdoc/>
    protected override string Identifier => "VirtualConsole";

    /// <summary>
    /// Asserts that the screen content contains the specified text.
    /// </summary>
    /// <param name="expected">The text expected to be found.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> ContainText(
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        var content = Subject.GetScreenContent();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(content.Contains(expected))
            .FailWith("Expected VirtualConsole to contain text {0}{reason}, but found:\n{1}",
                expected, content);

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that the screen content does not contain the specified text.
    /// </summary>
    /// <param name="unexpected">The text that should not be found.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> NotContainText(
        string unexpected,
        string because = "",
        params object[] becauseArgs)
    {
        var content = Subject.GetScreenContent();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!content.Contains(unexpected))
            .FailWith("Expected VirtualConsole not to contain text {0}{reason}, but found:\n{1}",
                unexpected, content);

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that the cell at the specified position has the specified attributes.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="column">Column (0-based).</param>
    /// <param name="expectedAttributes">The expected cell attributes.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveCellWithStyle(
        int row,
        int column,
        CellAttributes expectedAttributes,
        string because = "",
        params object[] becauseArgs)
    {
        var cell = Subject.GetCell(row, column);
        var actualAttributes = cell.Style.Attributes;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actualAttributes.HasFlag(expectedAttributes))
            .FailWith("Expected cell at ({0}, {1}) to have attributes {2}{reason}, but found {3}. Character: '{4}'",
                row, column, expectedAttributes, actualAttributes, cell.Character);

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that the cell at the specified position has dim (ghost text) style.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="column">Column (0-based).</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveDimCellAt(
        int row,
        int column,
        string because = "",
        params object[] becauseArgs)
    {
        return HaveCellWithStyle(row, column, CellAttributes.Dim, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the cell at the specified position has reverse (selection) style.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="column">Column (0-based).</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveReverseCellAt(
        int row,
        int column,
        string because = "",
        params object[] becauseArgs)
    {
        return HaveCellWithStyle(row, column, CellAttributes.Reverse, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that text at the specified position matches the expected string.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="column">Column (0-based).</param>
    /// <param name="expectedText">The expected text.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveTextAt(
        int row,
        int column,
        string expectedText,
        string because = "",
        params object[] becauseArgs)
    {
        var screenRow = Subject.GetRow(row);
        var actualText = "";
        
        for (int i = 0; i < expectedText.Length && column + i < Subject.Width; i++)
        {
            actualText += Subject.GetCell(row, column + i).Character;
        }

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actualText == expectedText)
            .FailWith("Expected text at ({0}, {1}) to be {2}{reason}, but found {3}. Full row: {4}",
                row, column, expectedText, actualText, screenRow.GetText().TrimEnd());

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that a row contains the specified text.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="expectedText">The text expected to be found in the row.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveRowContaining(
        int row,
        string expectedText,
        string because = "",
        params object[] becauseArgs)
    {
        var rowText = Subject.GetRow(row).GetText();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(rowText.Contains(expectedText))
            .FailWith("Expected row {0} to contain {1}{reason}, but found: {2}",
                row, expectedText, rowText.TrimEnd());

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that the cursor is at the specified position.
    /// </summary>
    /// <param name="row">Expected cursor row (0-based).</param>
    /// <param name="column">Expected cursor column (0-based).</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveCursorAt(
        int row,
        int column,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.CursorRow == row && Subject.CursorColumn == column)
            .FailWith("Expected cursor at ({0}, {1}){reason}, but was at ({2}, {3})",
                row, column, Subject.CursorRow, Subject.CursorColumn);

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that a range of cells have the specified attribute.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="startColumn">Start column (0-based).</param>
    /// <param name="length">Number of cells to check.</param>
    /// <param name="expectedAttributes">The expected cell attributes.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveRangeWithStyle(
        int row,
        int startColumn,
        int length,
        CellAttributes expectedAttributes,
        string because = "",
        params object[] becauseArgs)
    {
        var failedCells = new List<(int col, CellAttributes actual)>();

        for (int col = startColumn; col < startColumn + length && col < Subject.Width; col++)
        {
            var cell = Subject.GetCell(row, col);
            if (!cell.Style.Attributes.HasFlag(expectedAttributes))
            {
                failedCells.Add((col, cell.Style.Attributes));
            }
        }

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(failedCells.Count == 0)
            .FailWith("Expected cells at row {0}, columns {1}-{2} to have attributes {3}{reason}, but cells at columns {4} had different attributes",
                row, startColumn, startColumn + length - 1, expectedAttributes,
                string.Join(", ", failedCells.Select(f => $"{f.col}:{f.actual}")));

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }
}
