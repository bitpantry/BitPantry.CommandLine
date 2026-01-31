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

    #region Gap 3: Positional-Capable Arguments in Named Suggestions

    /// <summary>
    /// 008:UX-035 - Unsatisfied positional-capable arguments should appear in -- suggestions.
    /// All arguments are named arguments, including those with Position set.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_UnsatisfiedPositionalCapable_IncludedInSuggestions()
    {
        // Arrange
        var commandInfo = CommandReflection.Describe<TestCommandWithPositionalArgs>();
        var handler = new ArgumentNameHandler();
        var context = CreateContext(commandInfo, queryString: "--");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - should include ALL arguments including positional-capable ones
        options.Select(o => o.Value).Should().Contain("--level",
            because: "positional-capable arguments are also named arguments");
        options.Select(o => o.Value).Should().Contain("--verbose");
    }

    /// <summary>
    /// When a positional-capable argument is satisfied (by position), 
    /// it should be excluded from -- suggestions.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_SatisfiedPositionalCapable_ExcludedFromSuggestions()
    {
        // Arrange - Level (Position=0) is satisfied by positional value "Debug"
        var commandInfo = CommandReflection.Describe<TestCommandWithPositionalArgs>();
        var handler = new ArgumentNameHandler();
        var context = CreateContextWithUsedInFullInput(commandInfo, queryString: "--", usedArgsInInput: "Debug");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - --level should NOT appear (satisfied positionally)
        options.Select(o => o.Value).Should().NotContain("--level",
            because: "Level was satisfied by positional value Debug");
        options.Select(o => o.Value).Should().Contain("--verbose",
            because: "Verbose is still unsatisfied");
    }

    /// <summary>
    /// When a positional-capable argument is satisfied by name,
    /// it should be excluded from -- suggestions.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_PositionalSatisfiedByName_ExcludedFromSuggestions()
    {
        // Arrange - Level is satisfied by --level Debug
        var commandInfo = CommandReflection.Describe<TestCommandWithPositionalArgs>();
        var handler = new ArgumentNameHandler();
        var context = CreateContextWithUsedInFullInput(commandInfo, queryString: "--", usedArgsInInput: "--level Debug");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - --level should NOT appear (already used)
        options.Select(o => o.Value).Should().NotContain("--level");
        options.Select(o => o.Value).Should().Contain("--verbose");
    }

    /// <summary>
    /// Mixed positional and named-only arguments should all appear when unsatisfied.
    /// </summary>
    [TestMethod]
    public async Task GetOptionsAsync_MixedArgs_AllUnsatisfiedIncluded()
    {
        // Arrange - command with Position=0 "Source", Position=1 "Dest", named-only "Compress"
        var commandInfo = CommandReflection.Describe<TestUploadCommand>();
        var handler = new ArgumentNameHandler();
        var context = CreateContext(commandInfo, queryString: "--");

        // Act
        var options = await handler.GetOptionsAsync(context);

        // Assert - all three should be included
        options.Select(o => o.Value).Should().Contain("--source");
        options.Select(o => o.Value).Should().Contain("--destination");
        options.Select(o => o.Value).Should().Contain("--compress");
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
    /// Test command with a positional-capable argument.
    /// </summary>
    public enum LogLevel { Debug, Info, Warning, Error }

    [Command]
    private class TestCommandWithPositionalArgs : CommandBase
    {
        [Argument(Position = 0)]
        [Alias('l')]
        public LogLevel Level { get; set; }

        [Argument]
        [Alias('v')]
        public bool Verbose { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test command with multiple positional-capable args and one named-only.
    /// </summary>
    [Command]
    private class TestUploadCommand : CommandBase
    {
        [Argument(Position = 0)]
        [Alias('s')]
        public string Source { get; set; } = "";

        [Argument(Position = 1)]
        [Alias('d')]
        public string Destination { get; set; } = "";

        [Argument]
        [Alias('c')]
        public bool Compress { get; set; }

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
