using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Providers;

/// <summary>
/// Tests for ArgumentNameProvider - completing --argName after command.
/// Covers test cases AC-001 to AC-006 from specs.
/// </summary>
[TestClass]
public class ArgumentNameProviderTests
{
    private ArgumentNameProvider _provider;
    private CommandRegistry _registry;

    [TestInitialize]
    public void Setup()
    {
        _registry = new CommandRegistry();
        _provider = new ArgumentNameProvider(_registry);
    }

    #region AC-006: Handle commands with no arguments

    [TestMethod]
    public async Task AC006_GetCompletions_UnknownCommand_ReturnsEmpty()
    {
        // Arrange - command does not exist
        var context = CreateContext("unknowncommand --", CompletionElementType.ArgumentName, commandName: "unknowncommand");

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Should().NotBeNull();
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
    public void CanHandle_WithArgumentName_ReturnsTrue()
    {
        var context = CreateContext("command --", CompletionElementType.ArgumentName);
        _provider.CanHandle(context).Should().BeTrue();
    }

    [TestMethod]
    public void CanHandle_WithCommand_ReturnsFalse()
    {
        var context = CreateContext("com", CompletionElementType.Command);
        _provider.CanHandle(context).Should().BeFalse();
    }

    [TestMethod]
    public async Task GetCompletions_NoCommandName_ReturnsEmpty()
    {
        // Arrange - no command name in context
        var context = CreateContext("--", CompletionElementType.ArgumentName);

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region Required Arguments First Tests

    /// <summary>
    /// AC-REQ-001: Required arguments should appear before optional arguments in completion results
    /// </summary>
    [TestMethod]
    public async Task ACREQ001_GetCompletions_RequiredArgumentsFirst()
    {
        // Arrange - register command with mixed required/optional args
        _registry.RegisterCommand<RequiredOptionalCommand>();
        var context = CreateContext("reqopt --", CompletionElementType.ArgumentName, commandName: "reqopt");

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        result.Items.Should().NotBeEmpty();
        var items = result.Items.ToList();
        
        // Find indices of required and optional args
        var apiKeyIndex = items.FindIndex(i => i.InsertText == "--ApiKey");
        var endpointIndex = items.FindIndex(i => i.InsertText == "--Endpoint");
        var hostIndex = items.FindIndex(i => i.InsertText == "--Host");
        var portIndex = items.FindIndex(i => i.InsertText == "--Port");
        
        // Required args (ApiKey, Endpoint) should appear before optional args (Host, Port)
        apiKeyIndex.Should().BeLessThan(hostIndex, "Required --ApiKey should appear before optional --Host");
        apiKeyIndex.Should().BeLessThan(portIndex, "Required --ApiKey should appear before optional --Port");
        endpointIndex.Should().BeLessThan(hostIndex, "Required --Endpoint should appear before optional --Host");
        endpointIndex.Should().BeLessThan(portIndex, "Required --Endpoint should appear before optional --Port");
    }

    /// <summary>
    /// AC-REQ-002: Required arguments should have higher SortPriority
    /// </summary>
    [TestMethod]
    public async Task ACREQ002_GetCompletions_RequiredArgsHaveHigherSortPriority()
    {
        // Arrange
        _registry.RegisterCommand<RequiredOptionalCommand>();
        var context = CreateContext("reqopt --", CompletionElementType.ArgumentName, commandName: "reqopt");

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        var requiredItems = result.Items.Where(i => i.InsertText == "--ApiKey" || i.InsertText == "--Endpoint");
        var optionalItems = result.Items.Where(i => i.InsertText == "--Host" || i.InsertText == "--Port");
        
        foreach (var reqItem in requiredItems)
        {
            foreach (var optItem in optionalItems)
            {
                reqItem.SortPriority.Should().BeGreaterThan(optItem.SortPriority,
                    $"Required {reqItem.InsertText} should have higher SortPriority than optional {optItem.InsertText}");
            }
        }
    }

    /// <summary>
    /// AC-REQ-003: Within required group, maintain alphabetical order
    /// </summary>
    [TestMethod]
    public async Task ACREQ003_GetCompletions_RequiredArgsMaintainAlphabeticalOrder()
    {
        // Arrange
        _registry.RegisterCommand<RequiredOptionalCommand>();
        var context = CreateContext("reqopt --", CompletionElementType.ArgumentName, commandName: "reqopt");

        // Act
        var result = await _provider.GetCompletionsAsync(context);

        // Assert
        var items = result.Items.ToList();
        var apiKeyIndex = items.FindIndex(i => i.InsertText == "--ApiKey");
        var endpointIndex = items.FindIndex(i => i.InsertText == "--Endpoint");
        
        // Both are required, ApiKey should come before Endpoint alphabetically
        apiKeyIndex.Should().BeLessThan(endpointIndex, "ApiKey should appear before Endpoint (alphabetically)");
    }

    #endregion

    #region Test Commands

    [Command(Name = "reqopt")]
    private class RequiredOptionalCommand : CommandBase
    {
        [Argument(IsRequired = true)]
        public string ApiKey { get; set; }

        [Argument]
        public string Host { get; set; }

        [Argument(IsRequired = true)]
        public string Endpoint { get; set; }

        [Argument]
        public int Port { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
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
