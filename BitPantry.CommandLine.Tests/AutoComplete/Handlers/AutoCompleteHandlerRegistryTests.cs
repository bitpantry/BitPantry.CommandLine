using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Description;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        var services = new ServiceCollection();
        services.AddTransient<TestTypeHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = new AutoCompleteHandlerRegistry();
        registry.Register<TestTypeHandler>();
        registry.SetServiceProvider(serviceProvider);
        
        // Get an ArgumentInfo for int (which TestTypeHandler does NOT handle)
        var commandInfo = CommandReflection.Describe<TestCommandWithIntArg>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Count");

        // Act
        var handler = registry.GetHandler(argumentInfo);

        // Assert
        handler.Should().BeNull();
    }

    /// <summary>
    /// Implements: 008:TC-1.3
    /// GetHandler returns the last registered matching handler (last-registered-wins).
    /// </summary>
    [TestMethod]
    public void GetHandler_MultipleMatchingHandlers_ReturnsLastRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EnumHandlerA>();
        services.AddTransient<EnumHandlerB>();
        var serviceProvider = services.BuildServiceProvider();

        var registry = new AutoCompleteHandlerRegistry();
        registry.Register<EnumHandlerA>();
        registry.Register<EnumHandlerB>();
        registry.SetServiceProvider(serviceProvider);
        
        // Get an ArgumentInfo for LogLevel enum
        var commandInfo = CommandReflection.Describe<TestCommandWithLogLevel>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Level");

        // Act
        var handler = registry.GetHandler(argumentInfo);

        // Assert - should return HandlerB (last registered wins)
        handler.Should().NotBeNull();
        handler.Should().BeOfType<EnumHandlerB>();
    }

    /// <summary>
    /// Implements: 008:TC-1.4
    /// Attribute handler takes precedence over type handler.
    /// </summary>
    [TestMethod]
    public void GetHandler_AttributeAndTypeHandler_ReturnsAttributeHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<EnumHandlerA>();        // Type handler that would match enum
        services.AddTransient<CustomEnumHandler>();   // Attribute handler explicitly specified
        var serviceProvider = services.BuildServiceProvider();

        var registry = new AutoCompleteHandlerRegistry();
        registry.Register<EnumHandlerA>();  // Type handler registered
        registry.SetServiceProvider(serviceProvider);
        
        // Get an ArgumentInfo for enum WITH [AutoComplete<CustomEnumHandler>] attribute
        var commandInfo = CommandReflection.Describe<TestCommandWithAttributeHandler>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Level");

        // Act
        var handler = registry.GetHandler(argumentInfo);

        // Assert - should return CustomEnumHandler (attribute takes precedence over type handler)
        handler.Should().NotBeNull();
        handler.Should().BeOfType<CustomEnumHandler>();
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test log level enum for testing handler precedence.
    /// </summary>
    private enum TestLogLevel { Debug, Info, Warning, Error }

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
    /// Test command with LogLevel enum for testing handler precedence.
    /// </summary>
    [Command]
    private class TestCommandWithLogLevel : CommandBase
    {
        [Argument]
        public TestLogLevel Level { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test command with LogLevel enum AND [AutoComplete] attribute for testing attribute precedence.
    /// </summary>
    [Command]
    private class TestCommandWithAttributeHandler : CommandBase
    {
        [Argument]
        [AutoComplete<CustomEnumHandler>]
        public TestLogLevel Level { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test handler for unit tests - handles strings only.
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

    /// <summary>
    /// First enum handler for testing last-registered-wins.
    /// </summary>
    private class EnumHandlerA : ITypeAutoCompleteHandler
    {
        public bool CanHandle(System.Type argumentType) => argumentType.IsEnum;

        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            HandlerContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AutoCompleteOption>());
        }
    }

    /// <summary>
    /// Second enum handler for testing last-registered-wins.
    /// </summary>
    private class EnumHandlerB : ITypeAutoCompleteHandler
    {
        public bool CanHandle(System.Type argumentType) => argumentType.IsEnum;

        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            HandlerContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AutoCompleteOption>());
        }
    }

    /// <summary>
    /// Custom attribute handler for testing attribute precedence over type handlers.
    /// Implements IAutoCompleteHandler (not ITypeAutoCompleteHandler) because 
    /// attribute handlers don't need CanHandle - they're explicitly bound.
    /// </summary>
    private class CustomEnumHandler : IAutoCompleteHandler
    {
        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            HandlerContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AutoCompleteOption>());
        }
    }

    #endregion
}
