using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Providers;

/// <summary>
/// Tests for EnumProvider - completing enum values.
/// Covers test cases EP-001 to EP-007 from specs.
/// </summary>
[TestClass]
public class EnumProviderTests
{
    private EnumProvider _provider;

    // Test enum for testing
    public enum TestLogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    [Flags]
    public enum TestPermissions
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }

    [TestInitialize]
    public void Setup()
    {
        _provider = new EnumProvider();
    }

    #region EP-001: Complete all enum values

    [TestMethod]
    public async Task EP001_GetCompletions_EnumType_ReturnsAllValues()
    {
        // Arrange
        var context = CreateContext("", typeof(TestLogLevel));

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(5);
        result.Items.Should().Contain(item => item.InsertText == "Debug");
        result.Items.Should().Contain(item => item.InsertText == "Info");
        result.Items.Should().Contain(item => item.InsertText == "Warning");
        result.Items.Should().Contain(item => item.InsertText == "Error");
        result.Items.Should().Contain(item => item.InsertText == "Critical");
    }

    #endregion

    #region EP-002: Filter by partial value

    [TestMethod]
    public async Task EP002_GetCompletions_WithPartialValue_FiltersResults()
    {
        // Arrange
        var context = CreateContext("Err", typeof(TestLogLevel));

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].InsertText.Should().Be("Error");
    }

    #endregion

    #region EP-003: Case-insensitive matching

    [TestMethod]
    public async Task EP003_GetCompletions_CaseInsensitive_Matches()
    {
        // Arrange
        var context = CreateContext("debug", typeof(TestLogLevel));

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].InsertText.Should().Be("Debug");
    }

    #endregion

    #region EP-004: Sort alphabetically

    [TestMethod]
    public async Task EP004_GetCompletions_SortedAlphabetically()
    {
        // Arrange
        var context = CreateContext("", typeof(TestLogLevel));

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        var insertTexts = result.Items.Select(i => i.InsertText).ToList();
        insertTexts.Should().BeInAscendingOrder();
    }

    #endregion

    #region EP-005: Handle Flags enum

    [TestMethod]
    public async Task EP005_GetCompletions_FlagsEnum_ReturnsAllValues()
    {
        // Arrange
        var context = CreateContext("", typeof(TestPermissions));

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(4);
        result.Items.Should().Contain(item => item.InsertText == "None");
        result.Items.Should().Contain(item => item.InsertText == "Read");
        result.Items.Should().Contain(item => item.InsertText == "Write");
        result.Items.Should().Contain(item => item.InsertText == "Execute");
    }

    #endregion

    #region EP-006: Handle non-enum type

    [TestMethod]
    public async Task EP006_GetCompletions_NonEnumType_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext("", typeof(string));

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region EP-007: No matching prefix

    [TestMethod]
    public async Task EP007_GetCompletions_NoMatchingPrefix_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext("xyz", typeof(TestLogLevel));

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region Provider Configuration

    [TestMethod]
    public void Priority_ShouldBePositive()
    {
        _provider.Priority.Should().BeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public void CanHandle_WithEnumPropertyType_ReturnsTrue()
    {
        var context = CreateContext("", typeof(TestLogLevel));
        _provider.CanHandle(context).Should().BeTrue();
    }

    [TestMethod]
    public void CanHandle_WithNonEnumPropertyType_ReturnsFalse()
    {
        var context = CreateContext("", typeof(string));
        _provider.CanHandle(context).Should().BeFalse();
    }

    #endregion

    private CompletionContext CreateContext(string prefix, Type propertyType)
    {
        return new CompletionContext
        {
            FullInput = $"mycommand --level {prefix}",
            CursorPosition = $"mycommand --level {prefix}".Length,
            ElementType = CompletionElementType.ArgumentValue,
            CurrentWord = prefix,
            CommandName = "mycommand",
            ArgumentName = "level",
            PropertyType = propertyType
        };
    }
}
