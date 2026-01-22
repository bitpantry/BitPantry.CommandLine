using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.AutoComplete.Syntax;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete.Syntax;

/// <summary>
/// Tests for ArgumentNameHandler.
/// </summary>
[TestClass]
public class ArgumentNameHandlerTests
{
    #region Spec 008-autocomplete-extensions

    /// <summary>
    /// Implements: 008:SYN-005
    /// When user types "--" at an argument position, handler suggests
    /// all available argument names prefixed with "--".
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_DoubleDashQuery_ReturnsArgumentNamesPrefixedWithDoubleDash()
    {
        // Arrange
        var commandInfo = CommandReflection.Describe<TestCommandWithArguments>();
        var handler = new ArgumentNameHandler();
        var context = CreateContext(commandInfo, queryString: "--");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return all argument names prefixed with "--"
        options.Should().HaveCount(3);
        options.Select(o => o.Value).Should().Contain(new[] { "--name", "--count", "--verbose" });
    }

    /// <summary>
    /// Implements: 008:SYN-005 (partial match variant)
    /// When user types "--na", only matching argument names are suggested.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_PartialArgumentName_FiltersToMatchingNames()
    {
        // Arrange
        var commandInfo = CommandReflection.Describe<TestCommandWithArguments>();
        var handler = new ArgumentNameHandler();
        var context = CreateContext(commandInfo, queryString: "--na");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should only match "--name"
        options.Should().ContainSingle();
        options.First().Value.Should().Be("--name");
    }

    /// <summary>
    /// Implements: 008:SYN-005 (case insensitive)
    /// Argument name filtering should be case-insensitive.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_CaseInsensitive_MatchesRegardlessOfCase()
    {
        // Arrange
        var commandInfo = CommandReflection.Describe<TestCommandWithArguments>();
        var handler = new ArgumentNameHandler();
        var context = CreateContext(commandInfo, queryString: "--NA");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should match "--name" case-insensitively
        options.Should().ContainSingle();
        options.First().Value.Should().Be("--name");
    }

    /// <summary>
    /// Implements: 008:SYN-007
    /// When some arguments have already been provided in the input,
    /// those arguments should be filtered out from suggestions.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_UsedArgumentsFiltered_ExcludesProvidedArguments()
    {
        // Arrange
        var commandInfo = CommandReflection.Describe<TestCommandWithArguments>();
        var handler = new ArgumentNameHandler();
        
        // Simulate that "--name" has already been provided by including it in FullInput
        // The handler parses FullInput to determine used arguments
        var context = CreateContextWithUsedInFullInput(commandInfo, queryString: "--", usedArgsInInput: "--name value");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should NOT include "--name" since it's already used
        options.Should().HaveCount(2); // Only count and verbose
        options.Select(o => o.Value).Should().NotContain("--name");
        options.Select(o => o.Value).Should().Contain(new[] { "--count", "--verbose" });
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test command with named arguments.
    /// </summary>
    [Command]
    private class TestCommandWithArguments : CommandBase
    {
        [Argument]
        public string Name { get; set; } = "";

        [Argument]
        public int Count { get; set; }

        [Argument]
        public bool Verbose { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Creates an AutoCompleteContext for argument name testing.
    /// </summary>
    private static AutoCompleteContext CreateContext(CommandInfo commandInfo, string queryString)
    {
        return new AutoCompleteContext
        {
            QueryString = queryString,
            FullInput = $"command {queryString}",
            CursorPosition = $"command {queryString}".Length,
            ArgumentInfo = commandInfo.Arguments.First(),
            ProvidedValues = new Dictionary<ArgumentInfo, string>(),
            CommandInfo = commandInfo
        };
    }

    /// <summary>
    /// Creates an AutoCompleteContext with used arguments in the FullInput for filtering tests.
    /// The handler parses FullInput to determine which arguments are already used.
    /// </summary>
    private static AutoCompleteContext CreateContextWithUsedInFullInput(
        CommandInfo commandInfo, 
        string queryString, 
        string usedArgsInInput)
    {
        var fullInput = $"command {usedArgsInInput} {queryString}";
        return new AutoCompleteContext
        {
            QueryString = queryString,
            FullInput = fullInput,
            CursorPosition = fullInput.Length,
            ArgumentInfo = commandInfo.Arguments.First(),
            ProvidedValues = new Dictionary<ArgumentInfo, string>(),
            CommandInfo = commandInfo
        };
    }

    #endregion
}
