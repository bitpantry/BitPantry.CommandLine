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
/// Tests for ArgumentAliasHandler.
/// </summary>
[TestClass]
public class ArgumentAliasHandlerTests
{
    #region Spec 008-autocomplete-extensions

    /// <summary>
    /// Implements: 008:SYN-006
    /// When user types "-" at an argument position, handler suggests
    /// all available argument aliases prefixed with "-".
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_SingleDashQuery_ReturnsArgumentAliasesPrefixedWithDash()
    {
        // Arrange
        var commandInfo = CommandReflection.Describe<TestCommandWithAliases>();
        var handler = new ArgumentAliasHandler();
        var context = CreateContext(commandInfo, queryString: "-");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should return all argument aliases prefixed with "-"
        options.Should().HaveCount(3);
        options.Select(o => o.Value).Should().Contain(new[] { "-n", "-c", "-v" });
    }

    /// <summary>
    /// Implements: 008:SYN-006 (partial match variant)
    /// When user types "-n", only matching aliases are suggested.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_PartialAlias_FiltersToMatchingAliases()
    {
        // Arrange
        var commandInfo = CommandReflection.Describe<TestCommandWithAliases>();
        var handler = new ArgumentAliasHandler();
        var context = CreateContext(commandInfo, queryString: "-n");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should only match "-n"
        options.Should().ContainSingle();
        options.First().Value.Should().Be("-n");
    }

    /// <summary>
    /// Implements: 008:SYN-006 (case insensitive)
    /// Alias filtering should be case-insensitive.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_CaseInsensitive_MatchesRegardlessOfCase()
    {
        // Arrange
        var commandInfo = CommandReflection.Describe<TestCommandWithAliases>();
        var handler = new ArgumentAliasHandler();
        var context = CreateContext(commandInfo, queryString: "-N");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should match "-n" case-insensitively
        options.Should().ContainSingle();
        options.First().Value.Should().Be("-n");
    }

    /// <summary>
    /// Implements: 008:SYN-006 (no alias defined)
    /// When arguments don't have aliases, they should not appear in alias suggestions.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_ArgumentWithoutAlias_NotIncluded()
    {
        // Arrange
        var commandInfo = CommandReflection.Describe<TestCommandWithMixedAliases>();
        var handler = new ArgumentAliasHandler();
        var context = CreateContext(commandInfo, queryString: "-");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should only include argument WITH alias, not the one without
        options.Should().ContainSingle();
        options.First().Value.Should().Be("-a");
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test command with arguments that have aliases.
    /// </summary>
    [Command]
    private class TestCommandWithAliases : CommandBase
    {
        [Argument]
        [Alias('n')]
        public string Name { get; set; } = "";

        [Argument]
        [Alias('c')]
        public int Count { get; set; }

        [Argument]
        [Alias('v')]
        public bool Verbose { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test command with mixed aliases (some have, some don't).
    /// </summary>
    [Command]
    private class TestCommandWithMixedAliases : CommandBase
    {
        [Argument]
        [Alias('a')]
        public string WithAlias { get; set; } = "";

        [Argument]
        public string NoAlias { get; set; } = "";

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Creates an AutoCompleteContext for argument alias testing.
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

    #endregion
}
