using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete.Handlers;

/// <summary>
/// Tests for CommandRegistryBuilder.RegisterAutoCompleteHandlerTypes —
/// verifies that handler types from [AutoComplete&lt;T&gt;] attributes
/// are automatically registered with DI.
/// </summary>
[TestClass]
public class AutoCompleteHandlerRegistrationTests
{
    [TestMethod]
    public void Build_CommandWithAutoCompleteAttribute_RegistersHandlerInDI()
    {
        // Arrange
        var services = new ServiceCollection();
        // Register IFileSystem, IPathEntryProvider, and Theme since FilePathAutoCompleteHandler depends on them
        services.AddSingleton<IFileSystem>(new System.IO.Abstractions.FileSystem());
        services.AddSingleton<IPathEntryProvider>(sp => new LocalPathEntryProvider(sp.GetRequiredService<IFileSystem>()));
        services.AddSingleton(new Theme());
        var builder = new CommandRegistryBuilder();
        builder.RegisterCommand<TestCommandWithFilePathArg>();

        // Act
        builder.Build(services);
        var provider = services.BuildServiceProvider();

        // Assert — FilePathAutoCompleteHandler should be auto-registered
        var handler = provider.GetService<FilePathAutoCompleteHandler>();
        handler.Should().NotBeNull(
            "Build() should auto-register handler types discovered from [FilePathAutoComplete] attribute");
    }

    [TestMethod]
    public void Build_CommandWithCustomAutoCompleteAttribute_RegistersCustomHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new CommandRegistryBuilder();
        builder.RegisterCommand<TestCommandWithCustomHandler>();

        // Act
        builder.Build(services);
        var provider = services.BuildServiceProvider();

        // Assert — StubAutoCompleteHandler should be auto-registered
        var handler = provider.GetService<StubAutoCompleteHandler>();
        handler.Should().NotBeNull(
            "Build() should auto-register custom handler types from [AutoComplete<T>]");
    }

    [TestMethod]
    public void Build_CommandWithoutAutoCompleteAttribute_DoesNotRegisterHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new CommandRegistryBuilder();
        builder.RegisterCommand<TestCommandWithoutAutoComplete>();

        // Act
        builder.Build(services);
        var provider = services.BuildServiceProvider();

        // Assert — no handler should be registered
        var handler = provider.GetService<StubAutoCompleteHandler>();
        handler.Should().BeNull(
            "Build() should not register handlers when no [AutoComplete] attributes exist");
    }

    #region Test Helpers

    /// <summary>
    /// A minimal autocomplete handler for testing DI registration.
    /// </summary>
    public class StubAutoCompleteHandler : IAutoCompleteHandler
    {
        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AutoCompleteOption>());
        }
    }

    [Command]
    private class TestCommandWithFilePathArg : CommandBase
    {
        [Argument]
        [FilePathAutoComplete]
        public string Path { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command]
    private class TestCommandWithCustomHandler : CommandBase
    {
        [Argument]
        [AutoComplete<StubAutoCompleteHandler>]
        public string CustomArg { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command]
    private class TestCommandWithoutAutoComplete : CommandBase
    {
        [Argument]
        public string PlainArg { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion
}
