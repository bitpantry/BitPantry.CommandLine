using System;
using System.Collections.Generic;
using BitPantry.CommandLine.AutoComplete;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete
{
    /// <summary>
    /// Tests for AutoCompleteMenu state management.
    /// Verifies selection, scrolling, and filtering behavior.
    /// </summary>
    [TestClass]
    public class AutoCompleteMenuTests
    {
        #region Test Helpers

        private static List<AutoCompleteOption> CreateOptions(params string[] values)
        {
            var options = new List<AutoCompleteOption>();
            foreach (var value in values)
            {
                options.Add(new AutoCompleteOption(value));
            }
            return options;
        }

        private static List<AutoCompleteOption> CreateManyOptions(int count)
        {
            var options = new List<AutoCompleteOption>();
            for (int i = 1; i <= count; i++)
            {
                options.Add(new AutoCompleteOption($"Option{i:D2}"));
            }
            return options;
        }

        #endregion

        #region Construction Tests

        [TestMethod]
        public void Constructor_WithOptions_InitializesMenuCorrectly()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");

            // Act
            var menu = new AutoCompleteMenu(options);

            // Assert - Comprehensive initialization verification
            // Options are stored correctly
            menu.Options.Should().HaveCount(3);
            menu.Options[0].Value.Should().Be("Alpha");
            menu.Options[1].Value.Should().Be("Beta");
            menu.Options[2].Value.Should().Be("Gamma");
            
            // First item is selected by default
            menu.SelectedIndex.Should().Be(0);
            menu.SelectedOption.Value.Should().Be("Alpha");
            
            // Filtered options equal all options initially
            menu.FilteredOptions.Should().HaveCount(3);
            menu.IsEmpty.Should().BeFalse();
        }

        [TestMethod]
        public void Constructor_WithEmptyOptions_ThrowsArgumentException()
        {
            // Arrange
            var options = new List<AutoCompleteOption>();

            // Act
            Action act = () => new AutoCompleteMenu(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*at least one option*");
        }

        [TestMethod]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new AutoCompleteMenu(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Selection State Tests

        [TestMethod]
        public void MoveDown_IncrementsSelectedIndex()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var menu = new AutoCompleteMenu(options);

            // Act
            menu.MoveDown();

            // Assert
            menu.SelectedIndex.Should().Be(1);
            menu.SelectedOption.Value.Should().Be("Beta");
        }

        [TestMethod]
        public void MoveUp_DecrementsSelectedIndex()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var menu = new AutoCompleteMenu(options);
            menu.MoveDown(); // Move to index 1

            // Act
            menu.MoveUp();

            // Assert
            menu.SelectedIndex.Should().Be(0);
            menu.SelectedOption.Value.Should().Be("Alpha");
        }

        [TestMethod]
        public void MoveDown_AtLastItem_WrapsToFirst()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var menu = new AutoCompleteMenu(options);
            menu.MoveDown(); // Index 1
            menu.MoveDown(); // Index 2 (last)

            // Act
            menu.MoveDown(); // Should wrap to 0

            // Assert
            menu.SelectedIndex.Should().Be(0);
            menu.SelectedOption.Value.Should().Be("Alpha");
        }

        [TestMethod]
        public void MoveUp_AtFirstItem_WrapsToLast()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var menu = new AutoCompleteMenu(options);

            // Act
            menu.MoveUp(); // Should wrap to last (index 2)

            // Assert
            menu.SelectedIndex.Should().Be(2);
            menu.SelectedOption.Value.Should().Be("Gamma");
        }

        #endregion

        #region Visible Window Tests (5 items max)

        [TestMethod]
        public void VisibleOptions_LessThan5Items_ReturnsAll()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var menu = new AutoCompleteMenu(options);

            // Act
            var visible = menu.VisibleOptions;

            // Assert
            visible.Should().HaveCount(3);
        }

        [TestMethod]
        public void VisibleOptions_Exactly5Items_ReturnsAll()
        {
            // Arrange
            var options = CreateManyOptions(5);
            var menu = new AutoCompleteMenu(options);

            // Act
            var visible = menu.VisibleOptions;

            // Assert
            visible.Should().HaveCount(5);
        }

        [TestMethod]
        public void VisibleOptions_MoreThan5Items_Returns5()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);

            // Act
            var visible = menu.VisibleOptions;

            // Assert
            visible.Should().HaveCount(5);
        }

        [TestMethod]
        public void VisibleOptions_SelectionAtTop_ShowsFromTop()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);
            // Selection is at index 0

            // Act
            var visible = menu.VisibleOptions;

            // Assert
            visible[0].Value.Should().Be("Option01");
            visible[4].Value.Should().Be("Option05");
            menu.VisibleStartIndex.Should().Be(0);
        }

        [TestMethod]
        public void VisibleOptions_SelectionNearBottom_ScrollsToKeepVisible()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);
            
            // Move to index 7
            for (int i = 0; i < 7; i++)
                menu.MoveDown();

            // Act
            var visible = menu.VisibleOptions;

            // Assert - window should scroll so index 7 is visible
            menu.SelectedIndex.Should().Be(7);
            visible.Should().Contain(o => o.Value == "Option08");
        }

        [TestMethod]
        public void VisibleOptions_SelectionAtBottom_ShowsLastItems()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);
            
            // Move to last item (index 9)
            for (int i = 0; i < 9; i++)
                menu.MoveDown();

            // Act
            var visible = menu.VisibleOptions;

            // Assert - window should show last 5 items
            menu.SelectedIndex.Should().Be(9);
            visible[4].Value.Should().Be("Option10");
            menu.VisibleStartIndex.Should().Be(5);
        }

        [TestMethod]
        public void VisibleOptions_AfterWrapToTop_ShowsFromTop()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);
            
            // Move to last then wrap
            for (int i = 0; i < 10; i++)
                menu.MoveDown();

            // Act
            var visible = menu.VisibleOptions;

            // Assert - should be back at top
            menu.SelectedIndex.Should().Be(0);
            visible[0].Value.Should().Be("Option01");
            menu.VisibleStartIndex.Should().Be(0);
        }

        [TestMethod]
        public void VisibleOptions_AfterWrapToBottom_ShowsFromBottom()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);

            // Move up from top (wrap to bottom)
            menu.MoveUp();

            // Act
            var visible = menu.VisibleOptions;

            // Assert - should be at bottom
            menu.SelectedIndex.Should().Be(9);
            visible[4].Value.Should().Be("Option10");
            menu.VisibleStartIndex.Should().Be(5);
        }

        #endregion

        #region Scroll Indicator Tests

        [TestMethod]
        public void HasMoreAbove_AtTop_ReturnsFalse()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);

            // Act & Assert
            menu.HasMoreAbove.Should().BeFalse();
        }

        [TestMethod]
        public void HasMoreAbove_Scrolled_ReturnsTrue()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);
            
            // Scroll down enough to have items above
            for (int i = 0; i < 6; i++)
                menu.MoveDown();

            // Act & Assert
            menu.HasMoreAbove.Should().BeTrue();
        }

        [TestMethod]
        public void HasMoreBelow_AtTop_ReturnsTrue()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);

            // Act & Assert
            menu.HasMoreBelow.Should().BeTrue();
        }

        [TestMethod]
        public void HasMoreBelow_AtBottom_ReturnsFalse()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);
            
            // Move to last item
            for (int i = 0; i < 9; i++)
                menu.MoveDown();

            // Act & Assert
            menu.HasMoreBelow.Should().BeFalse();
        }

        [TestMethod]
        public void MoreAboveCount_ReturnsCorrectCount()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);
            
            // Move down to trigger scroll (selection at index 6)
            for (int i = 0; i < 6; i++)
                menu.MoveDown();

            // Act & Assert - VisibleStartIndex should be 2, so 2 items above
            menu.MoreAboveCount.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void MoreBelowCount_ReturnsCorrectCount()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);

            // Act - at top, visible window shows 0-4, so 5 items below (5-9)
            menu.MoreBelowCount.Should().Be(5);
        }

        [TestMethod]
        public void MoreAboveCount_AtTop_ReturnsZero()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);

            // Act & Assert
            menu.MoreAboveCount.Should().Be(0);
        }

        [TestMethod]
        public void MoreBelowCount_AtBottom_ReturnsZero()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);
            
            // Move to last
            for (int i = 0; i < 9; i++)
                menu.MoveDown();

            // Act & Assert
            menu.MoreBelowCount.Should().Be(0);
        }

        [TestMethod]
        public void ScrollIndicators_LessThan5Items_BothFalse()
        {
            // Arrange
            var options = CreateOptions("Alpha", "Beta", "Gamma");
            var menu = new AutoCompleteMenu(options);

            // Act & Assert
            menu.HasMoreAbove.Should().BeFalse();
            menu.HasMoreBelow.Should().BeFalse();
        }

        #endregion

        #region Filtering Tests

        [TestMethod]
        public void Filter_MatchingQuery_ReducesOptions()
        {
            // Arrange
            var options = CreateOptions("Apple", "Apricot", "Banana", "Cherry");
            var menu = new AutoCompleteMenu(options);

            // Act
            var hasMatches = menu.Filter("Ap");

            // Assert
            hasMatches.Should().BeTrue();
            menu.FilteredOptions.Should().HaveCount(2);
            menu.FilteredOptions[0].Value.Should().Be("Apple");
            menu.FilteredOptions[1].Value.Should().Be("Apricot");
        }

        [TestMethod]
        public void Filter_NoMatches_ReturnsEmptyList()
        {
            // Arrange
            var options = CreateOptions("Apple", "Banana", "Cherry");
            var menu = new AutoCompleteMenu(options);

            // Act
            var hasMatches = menu.Filter("Xyz");

            // Assert
            hasMatches.Should().BeFalse();
            menu.FilteredOptions.Should().BeEmpty();
        }

        [TestMethod]
        public void Filter_ResetsSelectionToFirst()
        {
            // Arrange
            var options = CreateOptions("Apple", "Apricot", "Banana", "Cherry");
            var menu = new AutoCompleteMenu(options);
            menu.MoveDown(); // Select "Apricot"
            menu.MoveDown(); // Select "Banana"

            // Act
            menu.Filter("Ap");

            // Assert
            menu.SelectedIndex.Should().Be(0);
            menu.SelectedOption.Value.Should().Be("Apple");
        }

        [TestMethod]
        public void Filter_CaseInsensitive_Matches()
        {
            // Arrange
            var options = CreateOptions("Apple", "APRICOT", "Banana");
            var menu = new AutoCompleteMenu(options);

            // Act
            var hasMatches = menu.Filter("ap");

            // Assert
            hasMatches.Should().BeTrue();
            menu.FilteredOptions.Should().HaveCount(2);
        }

        [TestMethod]
        public void IsEmpty_AfterFilterRemovesAll_ReturnsTrue()
        {
            // Arrange
            var options = CreateOptions("Apple", "Banana", "Cherry");
            var menu = new AutoCompleteMenu(options);

            // Act
            menu.Filter("Xyz");

            // Assert
            menu.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void IsEmpty_WithMatches_ReturnsFalse()
        {
            // Arrange
            var options = CreateOptions("Apple", "Banana", "Cherry");
            var menu = new AutoCompleteMenu(options);

            // Act
            menu.Filter("App");

            // Assert
            menu.IsEmpty.Should().BeFalse();
        }

        [TestMethod]
        public void Filter_EmptyString_ShowsAllOptions()
        {
            // Arrange
            var options = CreateOptions("Apple", "Banana", "Cherry");
            var menu = new AutoCompleteMenu(options);
            menu.Filter("App"); // First filter to reduce

            // Act
            menu.Filter("");

            // Assert
            menu.FilteredOptions.Should().HaveCount(3);
        }

        [TestMethod]
        public void FilteredOptions_InitiallyEqualsAllOptions()
        {
            // Arrange
            var options = CreateOptions("Apple", "Banana", "Cherry");

            // Act
            var menu = new AutoCompleteMenu(options);

            // Assert
            menu.FilteredOptions.Should().HaveCount(3);
            menu.FilteredOptions.Should().BeEquivalentTo(options);
        }

        [TestMethod]
        public void VisibleOptions_UsesFilteredOptions()
        {
            // Arrange
            var options = CreateManyOptions(10);
            var menu = new AutoCompleteMenu(options);

            // Act
            menu.Filter("Option0"); // Matches Option01-09 (9 items)

            // Assert
            menu.FilteredOptions.Should().HaveCount(9);
            menu.VisibleOptions.Should().HaveCount(5); // Still capped at 5 visible
        }

        #endregion
    }
}
