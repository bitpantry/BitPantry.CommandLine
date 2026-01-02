using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Providers;

/// <summary>
/// Tests for ArgumentAliasProvider - completing -a aliases after command.
/// </summary>
[TestClass]
public class ArgumentAliasProviderTests
{
    private ArgumentAliasProvider _provider;
    private CommandRegistry _registry;

    [TestInitialize]
    public void Setup()
    {
        _registry = new CommandRegistry();
        _provider = new ArgumentAliasProvider(_registry);
    }

    #region Provider Configuration

    [TestMethod]
    public void Priority_ShouldBePositive()
    {
        _provider.Priority.Should().BeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public void CanHandle_WithArgumentAlias_ReturnsTrue()
    {
        var context = CreateContext("command -", CompletionElementType.ArgumentAlias);
        _provider.CanHandle(context).Should().BeTrue();
    }

    [TestMethod]
    public void CanHandle_WithCommand_ReturnsFalse()
    {
        var context = CreateContext("com", CompletionElementType.Command);
        _provider.CanHandle(context).Should().BeFalse();
    }

    [TestMethod]
    public void CanHandle_WithArgumentName_ReturnsFalse()
    {
        var context = CreateContext("command --", CompletionElementType.ArgumentName);
        _provider.CanHandle(context).Should().BeFalse();
    }

    [TestMethod]
    public async Task GetCompletions_NoCommandName_ReturnsEmpty()
    {
        // Arrange - no command name in context
        var context = CreateContext("-", CompletionElementType.ArgumentAlias);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetCompletions_UnknownCommand_ReturnsEmpty()
    {
        // Arrange - command does not exist
        var context = CreateContext("unknowncommand -", CompletionElementType.ArgumentAlias, commandName: "unknowncommand");

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region Required Arguments First

    /// <summary>
    /// Command with mix of required and optional arguments for testing alias ordering.
    /// </summary>
    [Command(Name = "reqoptcmd")]
    private class RequiredOptionalAliasCommand : CommandBase
    {
        [Argument(IsRequired = true)]
        [Alias('k')]
        public string ApiKey { get; set; }

        [Argument]
        [Alias('h')]
        public string Host { get; set; }

        [Argument(IsRequired = true)]
        [Alias('e')]
        public string Endpoint { get; set; }

        [Argument]
        [Alias('p')]
        public int Port { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    [TestMethod]
    [TestCategory("AAREQ001")]
    public async Task GetCompletions_ReturnsRequiredAliasesFirst()
    {
        // Arrange
        _registry.RegisterCommand<RequiredOptionalAliasCommand>();
        // The prefix is empty because the user typed just "-" and wants alias suggestions
        var context = CreateContext("reqoptcmd -", CompletionElementType.ArgumentAlias, "", "reqoptcmd");

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert - required aliases (-k, -e) should come before optional (-h, -p)
        var aliases = result.Items.Select(i => i.DisplayText).ToList();
        aliases.Should().HaveCount(4);

        // Required aliases should be first (exact order may vary alphabetically within group)
        var requiredAliases = new[] { "-k", "-e" };
        var optionalAliases = new[] { "-h", "-p" };

        // All required should come before all optional
        var lastRequiredIndex = aliases.Select((a, i) => new { a, i })
            .Where(x => requiredAliases.Contains(x.a))
            .Max(x => x.i);
        var firstOptionalIndex = aliases.Select((a, i) => new { a, i })
            .Where(x => optionalAliases.Contains(x.a))
            .Min(x => x.i);

        lastRequiredIndex.Should().BeLessThan(firstOptionalIndex, 
            "all required aliases should appear before any optional aliases");
    }

    [TestMethod]
    [TestCategory("AAREQ002")]
    public async Task GetCompletions_RequiredAliasesHaveHigherSortPriority()
    {
        // Arrange
        _registry.RegisterCommand<RequiredOptionalAliasCommand>();
        var context = CreateContext("reqoptcmd -", CompletionElementType.ArgumentAlias, "", "reqoptcmd");

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert - required aliases should have higher SortPriority than optional
        var requiredItems = result.Items.Where(i => i.DisplayText == "-k" || i.DisplayText == "-e");
        var optionalItems = result.Items.Where(i => i.DisplayText == "-h" || i.DisplayText == "-p");

        foreach (var required in requiredItems)
        {
            foreach (var optional in optionalItems)
            {
                required.SortPriority.Should().BeGreaterThan(optional.SortPriority,
                    $"required alias {required.DisplayText} should have higher priority than optional {optional.DisplayText}");
            }
        }
    }

    [TestMethod]
    [TestCategory("AAREQ003")]
    public async Task GetCompletions_AliasesAreAlphabeticalWithinRequiredGroup()
    {
        // Arrange
        _registry.RegisterCommand<RequiredOptionalAliasCommand>();
        var context = CreateContext("reqoptcmd -", CompletionElementType.ArgumentAlias, "", "reqoptcmd");

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert - within required group, should be alphabetical (-e before -k)
        var aliases = result.Items.Select(i => i.DisplayText).ToList();
        var requiredAliases = aliases.Where(a => a == "-e" || a == "-k").ToList();
        
        requiredAliases.Should().BeInAscendingOrder("required aliases should be alphabetical within their group");
    }

    #endregion

    private CompletionContext CreateContext(
        string input,
        CompletionElementType elementType,
        string prefix = "",
        string commandName = null,
        HashSet<string> usedArguments = null)
    {
        return new CompletionContext
        {
            FullInput = input,
            CursorPosition = input.Length,
            ElementType = elementType,
            CurrentWord = prefix,
            CommandName = commandName,
            UsedArguments = usedArguments ?? new HashSet<string>()
        };
    }
}
