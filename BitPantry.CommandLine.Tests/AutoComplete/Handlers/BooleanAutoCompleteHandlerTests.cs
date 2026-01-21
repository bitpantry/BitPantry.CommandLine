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
/// Tests for BooleanAutoCompleteHandler.
/// </summary>
[TestClass]
public class BooleanAutoCompleteHandlerTests
{
    #region Spec 008-autocomplete-extensions

    /// <summary>
    /// Implements: 008:TC-2.8
    /// CanHandle returns true for bool types.
    /// </summary>
    [TestMethod]
    public void CanHandle_BoolType_ReturnsTrue()
    {
        // Arrange
        var handler = new BooleanAutoCompleteHandler();

        // Act
        var result = handler.CanHandle(typeof(bool));

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Implements: 008:TC-2.9
    /// CanHandle returns false for non-bool types.
    /// </summary>
    [TestMethod]
    public void CanHandle_NonBoolType_ReturnsFalse()
    {
        // Arrange
        var handler = new BooleanAutoCompleteHandler();

        // Act
        var result = handler.CanHandle(typeof(string));

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Implements: 008:TC-2.10
    /// GetOptionsAsync returns ["false", "true"] when query is empty.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_EmptyQuery_ReturnsFalseAndTrue()
    {
        // Arrange
        var handler = new BooleanAutoCompleteHandler();
        var context = CreateContext<TestCommandWithBool>("Enabled", queryString: "");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return false and true in alphabetical order
        options.Should().HaveCount(2);
        options.Select(o => o.Value).Should().ContainInOrder("false", "true");
    }

    /// <summary>
    /// Implements: 008:TC-2.11
    /// GetOptionsAsync filters by prefix.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_QueryWithPrefix_FiltersResults()
    {
        // Arrange
        var handler = new BooleanAutoCompleteHandler();
        var context = CreateContext<TestCommandWithBool>("Enabled", queryString: "t");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should only return "true" when query is "t"
        options.Should().HaveCount(1);
        options.First().Value.Should().Be("true");
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test command with bool property.
    /// </summary>
    [Command]
    private class TestCommandWithBool : CommandBase
    {
        [Argument]
        public bool Enabled { get; set; }

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
