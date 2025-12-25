using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.AutoComplete.Providers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Providers;

/// <summary>
/// Tests for StaticValuesProvider - completing static [Completion("a","b")] values.
/// Covers test cases SV-001 to SV-004 from specs.
/// </summary>
[TestClass]
public class StaticValuesProviderTests
{
    private StaticValuesProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _provider = new StaticValuesProvider();
    }

    #region SV-001: Complete from static values

    [TestMethod]
    public async Task SV001_GetCompletions_StaticValues_ReturnsAllValues()
    {
        // Arrange
        var context = CreateContext("", new[] { "apple", "banana", "cherry" });

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.Should().Contain(item => item.InsertText == "apple");
        result.Items.Should().Contain(item => item.InsertText == "banana");
        result.Items.Should().Contain(item => item.InsertText == "cherry");
    }

    #endregion

    #region SV-002: Filter by partial value

    [TestMethod]
    public async Task SV002_GetCompletions_WithPartialValue_FiltersResults()
    {
        // Arrange
        var context = CreateContext("ap", new[] { "apple", "banana", "apricot" });

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(item => item.InsertText == "apple");
        result.Items.Should().Contain(item => item.InsertText == "apricot");
    }

    #endregion

    #region SV-003: Case-insensitive matching

    [TestMethod]
    public async Task SV003_GetCompletions_CaseInsensitive_Matches()
    {
        // Arrange
        var context = CreateContext("APP", new[] { "apple", "banana" });

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].InsertText.Should().Be("apple");
    }

    #endregion

    #region SV-004: No matching values

    [TestMethod]
    public async Task SV004_GetCompletions_NoMatchingValues_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext("xyz", new[] { "apple", "banana" });

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
    public void CanHandle_WithStaticValues_ReturnsTrue()
    {
        var context = CreateContext("", new[] { "value1" });
        _provider.CanHandle(context).Should().BeTrue();
    }

    [TestMethod]
    public void CanHandle_WithoutStaticValues_ReturnsFalse()
    {
        var context = CreateContext("", null);
        _provider.CanHandle(context).Should().BeFalse();
    }

    [TestMethod]
    public async Task GetCompletions_EmptyStaticValues_ReturnsEmpty()
    {
        // Arrange
        var context = CreateContext("", new string[0]);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    private CompletionContext CreateContext(string prefix, string[] staticValues)
    {
        CompletionAttribute completionAttr = null;
        if (staticValues != null)
        {
            completionAttr = new CompletionAttribute(staticValues);
        }

        return new CompletionContext
        {
            FullInput = $"mycommand --fruit {prefix}",
            CursorPosition = $"mycommand --fruit {prefix}".Length,
            ElementType = CompletionElementType.ArgumentValue,
            CurrentWord = prefix,
            CommandName = "mycommand",
            ArgumentName = "fruit",
            CompletionAttribute = completionAttr
        };
    }
}
