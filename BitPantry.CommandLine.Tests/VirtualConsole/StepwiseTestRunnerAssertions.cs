using FluentAssertions;
using FluentAssertions.Primitives;
using FluentAssertions.Execution;

namespace BitPantry.CommandLine.Tests.VirtualConsole
{
    /// <summary>
    /// FluentAssertions extensions for StepwiseTestRunner visual state validation.
    /// </summary>
    public static class StepwiseTestRunnerAssertions
    {
        /// <summary>
        /// Assert on the runner state.
        /// </summary>
        public static StepwiseTestRunnerAssertionsWrapper Should(this StepwiseTestRunner runner)
        {
            return new StepwiseTestRunnerAssertionsWrapper(runner);
        }
    }

    /// <summary>
    /// Fluent assertions wrapper for StepwiseTestRunner.
    /// </summary>
    public class StepwiseTestRunnerAssertionsWrapper
    {
        private readonly StepwiseTestRunner _runner;

        public StepwiseTestRunnerAssertionsWrapper(StepwiseTestRunner runner)
        {
            _runner = runner;
        }

        #region Buffer Assertions

        /// <summary>
        /// Asserts that the input buffer contains the expected text.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveBuffer(string expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.Buffer == expected)
                .FailWith("Expected buffer to be {0}{reason}, but found {1}.", expected, _runner.Buffer);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        /// <summary>
        /// Asserts that the buffer position (cursor within buffer) is at the expected position.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveBufferPosition(int expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.BufferPosition == expected)
                .FailWith("Expected buffer position to be {0}{reason}, but found {1}.", expected, _runner.BufferPosition);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        #endregion

        #region Cursor Assertions

        /// <summary>
        /// Asserts that the console cursor column is at the expected position.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveCursorColumn(int expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.CursorColumn == expected)
                .FailWith("Expected cursor column to be {0}{reason}, but found {1}.", expected, _runner.CursorColumn);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        /// <summary>
        /// Asserts that the console cursor line is at the expected position.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveCursorLine(int expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.CursorLine == expected)
                .FailWith("Expected cursor line to be {0}{reason}, but found {1}.", expected, _runner.CursorLine);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        /// <summary>
        /// Asserts that the cursor is at the expected column, accounting for prompt.
        /// The expected position is relative to the input (0 = right after prompt).
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveInputCursorAt(int expectedInputPosition, string because = "", params object[] becauseArgs)
        {
            var expectedColumn = _runner.PromptLength + expectedInputPosition;
            var actualInputPosition = _runner.CursorColumn - _runner.PromptLength;

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.CursorColumn == expectedColumn)
                .FailWith("Expected cursor to be at input position {0} (column {1}){reason}, but found at input position {2} (column {3}).",
                    expectedInputPosition, expectedColumn, actualInputPosition, _runner.CursorColumn);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        #endregion

        #region Display Assertions

        /// <summary>
        /// Asserts that the displayed line (line 0 of console) matches exactly.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveDisplayedLine(string expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.DisplayedLine == expected)
                .FailWith("Expected displayed line to be {0}{reason}, but found {1}.", expected, _runner.DisplayedLine);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        /// <summary>
        /// Asserts that the displayed input (after prompt) matches expected.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveDisplayedInput(string expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.DisplayedInput == expected)
                .FailWith("Expected displayed input to be {0}{reason}, but found {1}.", expected, _runner.DisplayedInput);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        #endregion

        #region Menu Assertions

        /// <summary>
        /// Asserts that the menu is visible.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveMenuVisible(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.IsMenuVisible)
                .FailWith("Expected menu to be visible{reason}, but it was not.");

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        /// <summary>
        /// Asserts that the menu is NOT visible.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> NotHaveMenuVisible(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(!_runner.IsMenuVisible)
                .FailWith("Expected menu to NOT be visible{reason}, but it was.");

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        /// <summary>
        /// Asserts that the menu is NOT visible (alias for NotHaveMenuVisible).
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveMenuHidden(string because = "", params object[] becauseArgs)
        {
            return NotHaveMenuVisible(because, becauseArgs);
        }

        /// <summary>
        /// Asserts that the menu selection index is at the expected position.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveMenuSelectedIndex(int expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.MenuSelectedIndex == expected)
                .FailWith("Expected menu selected index to be {0}{reason}, but found {1}.", expected, _runner.MenuSelectedIndex);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        /// <summary>
        /// Asserts that the selected menu item text matches expected.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveSelectedMenuItem(string expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.SelectedMenuItem == expected)
                .FailWith("Expected selected menu item to be {0}{reason}, but found {1}.", expected, _runner.SelectedMenuItem);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        /// <summary>
        /// Asserts that the menu has the expected number of items.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveMenuItemCount(int expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.MenuItemCount == expected)
                .FailWith("Expected menu to have {0} items{reason}, but found {1}.", expected, _runner.MenuItemCount);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        #endregion

        #region Composite Assertions

        /// <summary>
        /// Comprehensive state assertion for buffer AND cursor position.
        /// This is the most common assertion: check what's in the buffer and where the cursor is.
        /// </summary>
        public AndConstraint<StepwiseTestRunnerAssertionsWrapper> HaveState(
            string expectedBuffer,
            int expectedInputCursorPosition,
            string because = "",
            params object[] becauseArgs)
        {
            var expectedColumn = _runner.PromptLength + expectedInputCursorPosition;
            var actualInputPosition = _runner.CursorColumn - _runner.PromptLength;

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(_runner.Buffer == expectedBuffer && _runner.CursorColumn == expectedColumn)
                .FailWith(
                    "Expected state [buffer={0}, cursor at input position {1}]{reason}, " +
                    "but found [buffer={2}, cursor at input position {3}].",
                    expectedBuffer, expectedInputCursorPosition,
                    _runner.Buffer, actualInputPosition);

            return new AndConstraint<StepwiseTestRunnerAssertionsWrapper>(this);
        }

        #endregion
    }
}
