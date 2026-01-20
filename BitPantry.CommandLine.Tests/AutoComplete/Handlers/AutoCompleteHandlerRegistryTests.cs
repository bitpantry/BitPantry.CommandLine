using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Description;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HandlerContext = BitPantry.CommandLine.AutoComplete.Handlers.AutoCompleteContext;

namespace BitPantry.CommandLine.Tests.AutoComplete.Handlers;

/// <summary>
/// Tests for AutoCompleteHandlerRegistry.
/// </summary>
[TestClass]
public class AutoCompleteHandlerRegistryTests
{

    #region Spec 008-autocomplete-extensions

    /// <summary>
    /// Implements: 008:TC-1.1
    /// Register adds handler to the list.
    /// </summary>
    [TestMethod]
    public void Register_WithValidHandler_AddsToRegistry()
    {
        // Arrange
        var registry = new AutoCompleteHandlerRegistry();

        // Act
        registry.Register<TestTypeHandler>();

        // Assert
        registry.TypeHandlerCount.Should().Be(1);
    }

    /// <summary>
    /// Implements: 008:TC-1.2
    /// GetHandler returns null when no handler matches the argument type.
    /// </summary>
    [TestMethod]
    public void GetHandler_NoMatchingHandler_ReturnsNull()
    {
        // Arrange
        var registry = new AutoCompleteHandlerRegistry();
        // Register a handler that only handles strings (not int)
        registry.Register<TestTypeHandler>();
        
        // Get an ArgumentInfo for int (which TestTypeHandler does NOT handle)
        var commandInfo = CommandReflection.Describe<TestCommandWithIntArg>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Count");

        // Act
        var handler = registry.GetHandler(argumentInfo);

        // Assert
        handler.Should().BeNull();
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test command with int property for testing unhandled types.
    /// </summary>
    [Command]
    private class TestCommandWithIntArg : CommandBase
    {
        [Argument]
        public int Count { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test handler for unit tests.
    /// </summary>
    private class TestTypeHandler : ITypeAutoCompleteHandler
    {
        public bool CanHandle(System.Type argumentType) => argumentType == typeof(string);

        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            HandlerContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AutoCompleteOption>());
        }
    }

    #endregion
}
