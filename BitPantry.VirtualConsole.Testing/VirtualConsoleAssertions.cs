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

    /// <summary>
    /// Asserts that the cell at the specified position has the specified foreground color.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="column">Column (0-based).</param>
    /// <param name="expectedColor">The expected foreground color.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveCellWithForegroundColor(
        int row,
        int column,
        ConsoleColor? expectedColor,
        string because = "",
        params object[] becauseArgs)
    {
        var cell = Subject.GetCell(row, column);
        var actualColor = cell.Style.ForegroundColor;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actualColor == expectedColor)
            .FailWith("Expected cell at ({0}, {1}) to have foreground color {2}{reason}, but found {3}. Character: '{4}'",
                row, column, expectedColor?.ToString() ?? "default", actualColor?.ToString() ?? "default", cell.Character);

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that a range of cells have the specified foreground color.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="startColumn">Start column (0-based).</param>
    /// <param name="length">Number of cells to check.</param>
    /// <param name="expectedColor">The expected foreground color.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveRangeWithForegroundColor(
        int row,
        int startColumn,
        int length,
        ConsoleColor? expectedColor,
        string because = "",
        params object[] becauseArgs)
    {
        // Validate bounds
        var endColumn = startColumn + length - 1;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(row >= 0 && row < Subject.Height)
            .FailWith("Range row {0} is out of bounds (height: {1})", row, Subject.Height);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(startColumn >= 0 && endColumn < Subject.Width)
            .FailWith("Range columns {0}-{1} are out of bounds (width: {2})", startColumn, endColumn, Subject.Width);

        var failedCells = new List<(int col, ConsoleColor? actual)>();

        for (int col = startColumn; col < startColumn + length && col < Subject.Width; col++)
        {
            var cell = Subject.GetCell(row, col);
            if (cell.Style.ForegroundColor != expectedColor)
            {
                failedCells.Add((col, cell.Style.ForegroundColor));
            }
        }

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(failedCells.Count == 0)
            .FailWith("Expected cells at row {0}, columns {1}-{2} to have foreground color {3}{reason}, but cells at columns {4} had different colors",
                row, startColumn, startColumn + length - 1, expectedColor?.ToString() ?? "default",
                string.Join(", ", failedCells.Select(f => $"{f.col}:{f.actual?.ToString() ?? "default"}")));

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that the cell at the specified position has the specified 256-color palette foreground.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="column">Column (0-based).</param>
    /// <param name="expectedColor">The expected 256-color palette index (0-255).</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveCellWithForeground256(
        int row,
        int column,
        byte expectedColor,
        string because = "",
        params object[] becauseArgs)
    {
        var cell = Subject.GetCell(row, column);
        var actualColor = cell.Style.Foreground256;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actualColor == expectedColor)
            .FailWith("Expected cell at ({0}, {1}) to have 256-color foreground {2}{reason}, but found {3}. Character: '{4}'",
                row, column, expectedColor, actualColor?.ToString() ?? "none", cell.Character);

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that a range of cells have the specified 256-color palette foreground.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="startColumn">Start column (0-based).</param>
    /// <param name="length">Number of cells to check.</param>
    /// <param name="expectedColor">The expected 256-color palette index (0-255).</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveRangeWithForeground256(
        int row,
        int startColumn,
        int length,
        byte expectedColor,
        string because = "",
        params object[] becauseArgs)
    {
        var failedCells = new List<(int col, byte? actual)>();

        for (int col = startColumn; col < startColumn + length && col < Subject.Width; col++)
        {
            var cell = Subject.GetCell(row, col);
            if (cell.Style.Foreground256 != expectedColor)
            {
                failedCells.Add((col, cell.Style.Foreground256));
            }
        }

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(failedCells.Count == 0)
            .FailWith("Expected cells at row {0}, columns {1}-{2} to have 256-color foreground {3}{reason}, but cells at columns {4} had different colors",
                row, startColumn, startColumn + length - 1, expectedColor,
                string.Join(", ", failedCells.Select(f => $"{f.col}:{f.actual?.ToString() ?? "none"}")));

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Asserts that the cell at the specified position has the specified complete style.
    /// </summary>
    /// <param name="row">Row (0-based).</param>
    /// <param name="column">Column (0-based).</param>
    /// <param name="expectedStyle">The expected complete cell style.</param>
    /// <param name="because">A reason for the expectation.</param>
    /// <param name="becauseArgs">Arguments for the because message.</param>
    public AndConstraint<VirtualConsoleAssertions> HaveCellWithFullStyle(
        int row,
        int column,
        CellStyle expectedStyle,
        string because = "",
        params object[] becauseArgs)
    {
        var cell = Subject.GetCell(row, column);
        var actualStyle = cell.Style;

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(actualStyle.Equals(expectedStyle))
            .FailWith("Expected cell at ({0}, {1}) to have style {2}{reason}, but found {3}. Character: '{4}'",
                row, column, FormatStyle(expectedStyle), FormatStyle(actualStyle), cell.Character);

        return new AndConstraint<VirtualConsoleAssertions>(this);
    }

    /// <summary>
    /// Formats a CellStyle for display in assertion messages.
    /// </summary>
    private static string FormatStyle(CellStyle style)
    {
        var parts = new List<string>();

        if (style.ForegroundColor.HasValue)
            parts.Add($"FG:{style.ForegroundColor}");
        if (style.BackgroundColor.HasValue)
            parts.Add($"BG:{style.BackgroundColor}");
        if (style.Foreground256.HasValue)
            parts.Add($"FG256:{style.Foreground256}");
        if (style.Background256.HasValue)
            parts.Add($"BG256:{style.Background256}");
        if (style.ForegroundRgb.HasValue)
            parts.Add($"FGRGB:({style.ForegroundRgb.Value.R},{style.ForegroundRgb.Value.G},{style.ForegroundRgb.Value.B})");
        if (style.BackgroundRgb.HasValue)
            parts.Add($"BGRGB:({style.BackgroundRgb.Value.R},{style.BackgroundRgb.Value.G},{style.BackgroundRgb.Value.B})");
        if (style.Attributes != CellAttributes.None)
            parts.Add($"Attr:{style.Attributes}");

        return parts.Count > 0 ? $"[{string.Join(", ", parts)}]" : "[default]";
    }
}
