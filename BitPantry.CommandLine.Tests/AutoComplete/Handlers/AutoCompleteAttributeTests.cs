using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete.Handlers;

/// <summary>
/// Tests for AutoCompleteAttribute.
/// </summary>
[TestClass]
public class AutoCompleteAttributeTests
{
    #region Spec 008-autocomplete-extensions

    /// <summary>
    /// Implements: 008:TC-3.2
    /// HandlerType property returns correct type via IAutoCompleteAttribute.
    /// </summary>
    [TestMethod]
    public void HandlerType_ViaInterface_ReturnsCorrectType()
    {
        // Arrange - Get the attribute from the test property via IAutoCompleteAttribute interface
        var property = typeof(TestCommandWithHandler).GetProperty(nameof(TestCommandWithHandler.Value));
        var attribute = property!.GetCustomAttributes()
            .OfType<IAutoCompleteAttribute>()
            .FirstOrDefault();

        // Act
        var handlerType = attribute!.HandlerType;

        // Assert
        handlerType.Should().Be(typeof(TestValueHandler));
    }

    /// <summary>
    /// Implements: 008:TC-3.3
    /// Attribute works with ITypeAutoCompleteHandler types.
    /// The generic constraint allows any IAutoCompleteHandler, including ITypeAutoCompleteHandler.
    /// </summary>
    [TestMethod]
    public void HandlerType_WithTypeHandler_ReturnsCorrectType()
    {
        // Arrange - Get the attribute from a property using an ITypeAutoCompleteHandler
        var property = typeof(TestCommandWithTypeHandler).GetProperty(nameof(TestCommandWithTypeHandler.Level));
        var attribute = property!.GetCustomAttributes()
            .OfType<IAutoCompleteAttribute>()
            .FirstOrDefault();

        // Act
        var handlerType = attribute!.HandlerType;

        // Assert - Should work with ITypeAutoCompleteHandler implementations
        handlerType.Should().Be(typeof(TestTypeHandler));
    }

    /// <summary>
    /// Implements: 008:TC-3.4
    /// Custom attributes inheriting AutoCompleteAttribute<T> are discoverable via marker interface.
    /// </summary>
    [TestMethod]
    public void CustomAttribute_InheritingAutoComplete_IsDiscoverableViaMarkerInterface()
    {
        // Arrange - Get the custom attribute from a property via IAutoCompleteAttribute interface
        var property = typeof(TestCommandWithCustomAttribute).GetProperty(nameof(TestCommandWithCustomAttribute.Name));
        var attribute = property!.GetCustomAttributes()
            .OfType<IAutoCompleteAttribute>()
            .FirstOrDefault();

        // Act & Assert - Custom attribute should be found via marker interface
        attribute.Should().NotBeNull("custom attribute should be discoverable via IAutoCompleteAttribute");
        attribute!.HandlerType.Should().Be(typeof(TestValueHandler));
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Test handler for verifying HandlerType.
    /// </summary>
    private class TestValueHandler : IAutoCompleteHandler
    {
        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            BitPantry.CommandLine.AutoComplete.Handlers.AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AutoCompleteOption>());
        }
    }

    /// <summary>
    /// Test command with [AutoComplete<TestValueHandler>] attribute.
    /// </summary>
    [Command]
    private class TestCommandWithHandler : CommandBase
    {
        [AutoComplete<TestValueHandler>]
        public string Value { get; set; } = string.Empty;

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Test type handler implementing ITypeAutoCompleteHandler for TC-3.3.
    /// </summary>
    private class TestTypeHandler : ITypeAutoCompleteHandler
    {
        public bool CanHandle(Type type) => type == typeof(int);

        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            BitPantry.CommandLine.AutoComplete.Handlers.AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AutoCompleteOption>());
        }
    }

    /// <summary>
    /// Test command with [AutoComplete<TestTypeHandler>] attribute (ITypeAutoCompleteHandler).
    /// </summary>
    [Command]
    private class TestCommandWithTypeHandler : CommandBase
    {
        [AutoComplete<TestTypeHandler>]
        public int Level { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Custom attribute inheriting from AutoCompleteAttribute<T> for TC-3.4.
    /// This tests that inheritance works with the marker interface.
    /// </summary>
    private class CustomAutoCompleteAttribute : AutoCompleteAttribute<TestValueHandler>
    {
    }

    /// <summary>
    /// Test command using the custom attribute that inherits from AutoCompleteAttribute<T>.
    /// </summary>
    [Command]
    private class TestCommandWithCustomAttribute : CommandBase
    {
        [CustomAutoComplete]
        public string Name { get; set; } = string.Empty;

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion
}
