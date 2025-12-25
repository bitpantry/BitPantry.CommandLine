using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
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
