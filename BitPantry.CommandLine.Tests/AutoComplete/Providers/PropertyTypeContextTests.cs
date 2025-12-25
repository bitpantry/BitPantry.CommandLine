using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.AutoComplete.Providers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete.Providers;

/// <summary>
/// Tests for PropertyType context in completion - PT-001 to PT-003.
/// Verifies that property types are correctly passed to providers.
/// </summary>
[TestClass]
public class PropertyTypeContextTests
{
    #region PT-001: Enum type is correctly identified

    [TestMethod]
    [TestCategory("PT-001")]
    public async Task EnumProvider_WithEnumPropertyType_ReturnsEnumValues()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(ConsoleColor),
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCountGreaterThan(10); // ConsoleColor has many values
        result.Items.Should().Contain(i => i.InsertText == "Red");
        result.Items.Should().Contain(i => i.InsertText == "Blue");
        result.Items.Should().Contain(i => i.InsertText == "Green");
    }

    [TestMethod]
    [TestCategory("PT-001")]
    public async Task EnumProvider_WithNullableEnumType_ReturnsEnumValues()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(ConsoleColor?),
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCountGreaterThan(10);
        result.Items.Should().Contain(i => i.InsertText == "Black");
    }

    #endregion

    #region PT-002: Non-enum types don't trigger enum provider

    [TestMethod]
    [TestCategory("PT-002")]
    public void EnumProvider_WithStringType_CannotHandle()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(string),
            PartialValue = ""
        };

        // Act
        var canHandle = provider.CanHandle(context);

        // Assert
        canHandle.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("PT-002")]
    public void EnumProvider_WithIntType_CannotHandle()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(int),
            PartialValue = ""
        };

        // Act
        var canHandle = provider.CanHandle(context);

        // Assert
        canHandle.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("PT-002")]
    public void EnumProvider_WithNullPropertyType_CannotHandle()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = null,
            PartialValue = ""
        };

        // Act
        var canHandle = provider.CanHandle(context);

        // Assert
        canHandle.Should().BeFalse();
    }

    #endregion

    #region PT-003: Custom enum values work correctly

    [TestMethod]
    [TestCategory("PT-003")]
    public async Task EnumProvider_WithCustomEnum_ReturnsAllValues()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(TestPriority),
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(4);
        result.Items.Select(i => i.InsertText).Should().Contain(new[] { "Low", "Medium", "High", "Critical" });
    }

    [TestMethod]
    [TestCategory("PT-003")]
    public async Task EnumProvider_WithFlagsEnum_ReturnsAllValues()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(TestFlags),
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(4);
        result.Items.Select(i => i.InsertText).Should().Contain(new[] { "None", "Read", "Write", "Execute" });
    }

    [TestMethod]
    [TestCategory("PT-003")]
    public async Task EnumProvider_WithPartialValue_FiltersResults()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(DayOfWeek),
            PartialValue = "Mon"
        };

        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].InsertText.Should().Be("Monday");
    }

    [TestMethod]
    [TestCategory("PT-003")]
    public async Task EnumProvider_CaseInsensitiveFiltering()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(DayOfWeek),
            PartialValue = "tue"
        };

        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].InsertText.Should().Be("Tuesday");
    }

    #endregion

    #region Enum type detection edge cases

    [TestMethod]
    public async Task EnumProvider_WithEnumArray_ReturnsEnumValues()
    {
        // Arrange
        var provider = new EnumProvider();
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            PropertyType = typeof(DayOfWeek[]),
            PartialValue = ""
        };

        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert - should handle array of enum
        result.Items.Should().HaveCount(7);
    }

    #endregion

    #region Test Enums

    public enum TestPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    [Flags]
    public enum TestFlags
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }

    #endregion
}
