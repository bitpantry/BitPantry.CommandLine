using System;
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
/// Tests for AutoCompleteHandlerRegistry and AutoCompleteHandlerRegistryBuilder.
/// </summary>
[TestClass]
public class AutoCompleteHandlerRegistryTests
{

    #region Spec 008-autocomplete-extensions

    /// <summary>
    /// Implements: 008:TC-1.1
    /// Register adds handler to the list, verified after build.
    /// Note: Builder starts with 2 built-in handlers (EnumAutoCompleteHandler, BooleanAutoCompleteHandler).
    /// </summary>
    [TestMethod]
    public void Register_WithValidHandler_AddsToRegistry()
    {
        // Arrange
        var builder = new AutoCompleteHandlerRegistryBuilder();
        // Builder includes 2 built-in handlers by default

        // Act
        builder.Register<TestTypeHandler>();
        var registry = builder.Build();

        // Assert - 2 built-in + 1 registered = 3
        registry.TypeHandlerCount.Should().Be(3);
    }

    /// <summary>
    /// Implements: 008:TC-1.2
    /// FindHandler returns null when no handler matches the argument type.
    /// </summary>
    [TestMethod]
    public void FindHandler_NoMatchingHandler_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new AutoCompleteHandlerRegistryBuilder();
        builder.Register<TestTypeHandler>();
        var registry = builder.Build(services);
        var serviceProvider = services.BuildServiceProvider();
        var activator = new AutoCompleteHandlerActivator(serviceProvider);
        
        // Get an ArgumentInfo for int (which TestTypeHandler does NOT handle)
        var commandInfo = CommandReflection.Describe<TestCommandWithIntArg>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Count");

        // Act
        var handlerType = registry.FindHandler(argumentInfo, activator);

        // Assert
        handlerType.Should().BeNull();
    }

    /// <summary>
    /// Implements: 008:TC-1.3
    /// FindHandler returns the last registered matching handler type (last-registered-wins).
    /// </summary>
    [TestMethod]
    public void FindHandler_MultipleMatchingHandlers_ReturnsLastRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new AutoCompleteHandlerRegistryBuilder();
        builder.Register<EnumHandlerA>();
        builder.Register<EnumHandlerB>();
        var registry = builder.Build(services);
        var serviceProvider = services.BuildServiceProvider();
        var activator = new AutoCompleteHandlerActivator(serviceProvider);
        
        // Get an ArgumentInfo for LogLevel enum
        var commandInfo = CommandReflection.Describe<TestCommandWithLogLevel>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Level");

        // Act
        var handlerType = registry.FindHandler(argumentInfo, activator);

        // Assert - should return HandlerB type (last registered wins)
        handlerType.Should().NotBeNull();
        handlerType.Should().Be(typeof(EnumHandlerB));
        
        // Verify activation works
        using var activation = activator.Activate(handlerType!);
        activation.Handler.Should().BeOfType<EnumHandlerB>();
    }

    /// <summary>
    /// Implements: 008:TC-1.4
    /// Attribute handler takes precedence over type handler.
    /// </summary>
    [TestMethod]
    public void FindHandler_AttributeAndTypeHandler_ReturnsAttributeHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<CustomEnumHandler>();   // Attribute handler explicitly specified (not registered via builder)
        var builder = new AutoCompleteHandlerRegistryBuilder();
        builder.Register<EnumHandlerA>();  // Type handler registered
        var registry = builder.Build(services);
        var serviceProvider = services.BuildServiceProvider();
        var activator = new AutoCompleteHandlerActivator(serviceProvider);
        
        // Get an ArgumentInfo for enum WITH [AutoComplete<CustomEnumHandler>] attribute
        var commandInfo = CommandReflection.Describe<TestCommandWithAttributeHandler>();
        var argumentInfo = commandInfo.Arguments.First(a => a.Name == "Level");

        // Act
        var handlerType = registry.FindHandler(argumentInfo, activator);

        // Assert - should return CustomEnumHandler type (attribute takes precedence over type handler)
        handlerType.Should().NotBeNull();
        handlerType.Should().Be(typeof(CustomEnumHandler));
        
        // Verify activation works
        using var activation = activator.Activate(handlerType!);
        activation.Handler.Should().BeOfType<CustomEnumHandler>();
    }

    /// <summary>
    /// Builder should throw if used after Build() has been called.
    /// </summary>
    [TestMethod]
    public void Builder_AfterBuild_ThrowsOnRegister()
    {
        // Arrange
        var builder = new AutoCompleteHandlerRegistryBuilder();
        builder.Build();

        // Act & Assert
        var act = () => builder.Register<TestTypeHandler>();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been built*");
    }

    /// <summary>
    /// Builder should throw if Build() is called twice.
    /// </summary>
    [TestMethod]
    public void Builder_BuildCalledTwice_Throws()
    {
        // Arrange
        var builder = new AutoCompleteHandlerRegistryBuilder();
        builder.Build();

        // Act & Assert
        var act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been built*");
    }

    /// <summary>
    /// Build(services) should register handler types with DI container.
    /// </summary>
    [TestMethod]
    public void Build_WithServices_RegistersHandlersWithDI()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new AutoCompleteHandlerRegistryBuilder();
        builder.Register<TestTypeHandler>();
        builder.Register<EnumHandlerA>();

        // Act
        builder.Build(services);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - handlers should be resolvable from DI
        var handler1 = serviceProvider.GetService<TestTypeHandler>();
        var handler2 = serviceProvider.GetService<EnumHandlerA>();
        handler1.Should().NotBeNull();
        handler2.Should().NotBeNull();
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
