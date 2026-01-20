using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Description;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete.Handlers;

/// <summary>
/// Tests for EnumAutoCompleteHandler.
/// </summary>
[TestClass]
public class EnumAutoCompleteHandlerTests
{
    #region Spec 008-autocomplete-extensions

    /// <summary>
    /// Implements: 008:TC-2.1
    /// CanHandle returns true for enum types.
    /// </summary>
    [TestMethod]
    public void CanHandle_EnumType_ReturnsTrue()
    {
        // Arrange
        var handler = new EnumAutoCompleteHandler();

        // Act
        var result = handler.CanHandle(typeof(TestLogLevel));

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Implements: 008:TC-2.2
    /// CanHandle returns false for non-enum types.
    /// </summary>
    [TestMethod]
    public void CanHandle_NonEnumType_ReturnsFalse()
    {
        // Arrange
        var handler = new EnumAutoCompleteHandler();

        // Act
        var result = handler.CanHandle(typeof(string));

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Implements: 008:TC-2.3
    /// CanHandle returns false for Enum base type (System.Enum itself is not an enum).
    /// </summary>
    [TestMethod]
    public void CanHandle_EnumBaseType_ReturnsFalse()
    {
        // Arrange
        var handler = new EnumAutoCompleteHandler();

        // Act
        var result = handler.CanHandle(typeof(Enum));

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Implements: 008:TC-2.4
    /// GetOptionsAsync returns all enum values when query is empty.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_EmptyQuery_ReturnsAllEnumValues()
    {
        // Arrange
        var handler = new EnumAutoCompleteHandler();
        var context = CreateContext<TestCommandWithLogLevel>("Level", queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert
        options.Should().HaveCount(4);
        options.Select(o => o.Value).Should().Contain(new[] { "Debug", "Info", "Warning", "Error" });
    }

    /// <summary>
    /// Implements: 008:TC-2.5
    /// GetOptionsAsync filters by prefix case-insensitively.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_WithPrefix_FiltersCaseInsensitively()
    {
        // Arrange
        var handler = new EnumAutoCompleteHandler();
        var context = CreateContext<TestCommandWithLogLevel>("Level", queryString: "war");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should match "Warning" case-insensitively
        options.Should().HaveCount(1);
        options.Select(o => o.Value).Should().Contain("Warning");
    }

    /// <summary>
    /// Implements: 008:TC-2.6
    /// GetOptionsAsync returns alphabetically sorted results.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_ReturnsAlphabeticallySorted()
    {
        // Arrange - enum declared as Zebra, Apple, Mango (NOT alphabetical)
        var handler = new EnumAutoCompleteHandler();
        var context = CreateContext<TestCommandWithFruit>("Fruit", queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should be alphabetically sorted
        var values = options.Select(o => o.Value).ToList();
        values.Should().BeInAscendingOrder();
        values.Should().ContainInOrder("Apple", "Mango", "Zebra");
    }

    /// <summary>
    /// Implements: 008:TC-2.7
    /// GetOptionsAsync unwraps nullable enum types.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_NullableEnum_ReturnsEnumValues()
    {
        // Arrange
        var handler = new EnumAutoCompleteHandler();
        var context = CreateContext<TestCommandWithNullableLogLevel>("Level", queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should unwrap LogLevel? to LogLevel and return all values
        options.Should().HaveCount(4);
        options.Select(o => o.Value).Should().Contain(new[] { "Debug", "Error", "Info", "Warning" });
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test enum for handler tests.
    /// </summary>
    private enum TestLogLevel { Debug, Info, Warning, Error }

    /// <summary>
    /// Test enum with non-alphabetical declaration order.
    /// </summary>
    private enum TestFruit { Zebra, Apple, Mango }

    /// <summary>
    /// Test command with LogLevel enum property.
    /// </summary>
    [Command]
    private class TestCommandWithLogLevel : CommandBase
    {
        [Argument]
        public TestLogLevel Level { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test command with nullable LogLevel enum property.
    /// </summary>
    [Command]
    private class TestCommandWithNullableLogLevel : CommandBase
    {
        [Argument]
        public TestLogLevel? Level { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test command with Fruit enum property for sorting tests.
    /// </summary>
    [Command]
    private class TestCommandWithFruit : CommandBase
    {
        [Argument]
        public TestFruit Fruit { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Creates an AutoCompleteContext for testing.
    /// </summary>
    private static AutoCompleteContext CreateContext<TCommand>(string argumentName, string queryString = "")
        where TCommand : class
    {
        var commandInfo = CommandReflection.Describe<TCommand>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == argumentName);

        return new AutoCompleteContext
        {
            QueryString = queryString,
            FullInput = $"cmd --{argumentName} {queryString}",
            CursorPosition = $"cmd --{argumentName} {queryString}".Length,
            ArgumentInfo = argumentInfo,
            ProvidedValues = new Dictionary<BitPantry.CommandLine.Component.ArgumentInfo, string>(),
            CommandInfo = commandInfo
        };
    }

    #endregion
}
