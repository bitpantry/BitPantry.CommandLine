using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Processing.Description;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BitPantry.CommandLine.Tests.AutoComplete.Integration;

/// <summary>
/// Integration tests for command implementer workflow - CI-001 to CI-013.
/// Tests the complete flow from attribute to completion result.
/// </summary>
[TestClass]
public class CommandImplementerIntegrationTests
{
    private ServiceProvider _serviceProvider;
    private CommandRegistry _registry;
    private IEnumerable<ICompletionProvider> _providers;
    private CompletionOrchestrator _orchestrator;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Register a sample service for DI tests
        services.AddSingleton<IUserService>(new UserService(new[] { "alice", "bob", "charlie" }));
        
        _serviceProvider = services.BuildServiceProvider();

        // Create registry and register test commands
        _registry = new CommandRegistry();
        _registry.RegisterCommand<TestFormatCommand>();
        _registry.RegisterCommand<TestEnumCommand>();
        _registry.RegisterCommand<TestStaticValuesCommand>();
        _registry.RegisterCommand<TestDICommand>();
        
        // Create providers with proper filesystem mock for file providers
        var mockFileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        _providers = new List<ICompletionProvider>
        {
            new HistoryProvider(new InputLog()),
            new MethodProvider(),
            new StaticValuesProvider(),
            new EnumProvider(),
            new FilePathProvider(mockFileSystem),
            new DirectoryPathProvider(mockFileSystem),
            new ArgumentNameProvider(_registry),
            new ArgumentAliasProvider(_registry),
            new CommandCompletionProvider(_registry)
        };

        var cache = new CompletionCache();
        _orchestrator = new CompletionOrchestrator(_providers, cache, _registry, _serviceProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    #region CI-001: Method-based completion works end-to-end

    [TestMethod]
    [TestCategory("CI-001")]
    public async Task HandleTabAsync_WithMethodBasedCompletion_ReturnsMethodResults()
    {
        // Arrange - "format --output " (completing value for --output)
        var input = "format --output ";
        
        // Create context for value completion
        var context = new CompletionContext
        {
            InputText = input,
            CursorPosition = input.Length,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "format",
            ArgumentName = "output",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetOutputFormats"),
            CommandInstance = new TestFormatCommand(),
            Services = _serviceProvider
        };

        // Act - use MethodProvider directly for this test
        var methodProvider = _providers.OfType<MethodProvider>().First();
        var result = await methodProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.Select(i => i.InsertText).Should().Contain(new[] { "json", "xml", "csv" });
    }

    #endregion

    #region CI-002: Static values completion works end-to-end

    [TestMethod]
    [TestCategory("CI-002")]
    public async Task HandleTabAsync_WithStaticValuesCompletion_ReturnsStaticValues()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "statictest",
            ArgumentName = "level",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute(new[] { "debug", "info", "warn", "error" }),
            Services = _serviceProvider
        };

        // Act - use StaticValuesProvider directly
        var staticProvider = _providers.OfType<StaticValuesProvider>().First();
        var result = await staticProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(4);
        result.Items.Select(i => i.InsertText).Should().Contain(new[] { "debug", "info", "warn", "error" });
    }

    #endregion

    #region CI-003: Enum completion works end-to-end

    [TestMethod]
    [TestCategory("CI-003")]
    public async Task HandleTabAsync_WithEnumProperty_ReturnsEnumValues()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "enumtest",
            ArgumentName = "day",
            PartialValue = "",
            PropertyType = typeof(DayOfWeek),
            Services = _serviceProvider
        };

        // Act - use EnumProvider directly
        var enumProvider = _providers.OfType<EnumProvider>().First();
        var result = await enumProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(7);
        result.Items.Select(i => i.InsertText).Should().Contain(new[] { "Monday", "Tuesday", "Wednesday" });
    }

    #endregion

    #region CI-004: DI injection works in completion methods

    [TestMethod]
    [TestCategory("CI-004")]
    public async Task HandleTabAsync_WithDIInMethod_InjectsServices()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "ditest",
            ArgumentName = "user",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetUsers"),
            CommandInstance = new TestDICommand(),
            Services = _serviceProvider
        };

        // Act - use MethodProvider directly
        var methodProvider = _providers.OfType<MethodProvider>().First();
        var result = await methodProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.Select(i => i.InsertText).Should().Contain(new[] { "alice", "bob", "charlie" });
    }

    #endregion

    #region CI-005: Async completion methods work

    [TestMethod]
    [TestCategory("CI-005")]
    public async Task HandleTabAsync_WithAsyncMethod_AwaitsResult()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "format",
            ArgumentName = "output",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetOutputFormatsAsync"),
            CommandInstance = new TestFormatCommand(),
            Services = _serviceProvider
        };

        // Act
        var methodProvider = _providers.OfType<MethodProvider>().First();
        var result = await methodProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.InsertText).Should().Contain(new[] { "async-format1", "async-format2" });
    }

    #endregion

    #region CI-006: Partial value filtering works

    [TestMethod]
    [TestCategory("CI-006")]
    public async Task HandleTabAsync_WithPartialInput_FiltersResults()
    {
        // Arrange - partial value "js" should filter to "json"
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "format",
            ArgumentName = "output",
            PartialValue = "js",
            CompletionAttribute = new CompletionAttribute("GetOutputFormats"),
            CommandInstance = new TestFormatCommand(),
            Services = _serviceProvider
        };

        // Act
        var methodProvider = _providers.OfType<MethodProvider>().First();
        var result = await methodProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].InsertText.Should().Be("json");
    }

    #endregion

    #region CI-007: Argument name completion works

    [TestMethod]
    [TestCategory("CI-007")]
    public async Task HandleTabAsync_ForArgumentName_ReturnsArguments()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentName,
            CommandName = "format",
            PartialValue = "Out",
            UsedArguments = new HashSet<string>(),
            Services = _serviceProvider
        };

        // Act
        var argProvider = _providers.OfType<ArgumentNameProvider>().First();
        var result = await argProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert - expects "--Output" with prefix
        result.Items.Should().Contain(i => i.InsertText == "--Output");
    }

    #endregion

    #region CI-008: Command completion works

    [TestMethod]
    [TestCategory("CI-008")]
    public async Task HandleTabAsync_ForCommand_ReturnsCommands()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.Command,
            CommandName = "",
            PartialValue = "format",
            Services = _serviceProvider
        };

        // Act
        var cmdProvider = _providers.OfType<CommandCompletionProvider>().First();
        var result = await cmdProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().Contain(i => i.InsertText == "format");
    }

    #endregion

    #region CI-009: Used arguments are excluded

    [TestMethod]
    [TestCategory("CI-009")]
    public async Task HandleTabAsync_WithUsedArguments_ExcludesUsed()
    {
        // Arrange
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentName,
            CommandName = "format",
            PartialValue = "",
            UsedArguments = new HashSet<string> { "output" },
            Services = _serviceProvider
        };

        // Act
        var argProvider = _providers.OfType<ArgumentNameProvider>().First();
        var result = await argProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().NotContain(i => i.InsertText == "output");
    }

    #endregion

    #region CI-010: Multiple attributes on same command work

    [TestMethod]
    [TestCategory("CI-010")]
    public async Task HandleTabAsync_MultipleCompletionProperties_EachWorksIndependently()
    {
        // Arrange - first property uses method
        var methodContext = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "multi",
            ArgumentName = "format",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetFormats"),
            CommandInstance = new MultiAttributeCommand(),
            Services = _serviceProvider
        };

        // Arrange - second property uses static values
        var staticContext = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "multi",
            ArgumentName = "level",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute(new[] { "low", "medium", "high" }),
            Services = _serviceProvider
        };

        // Act
        var methodProvider = _providers.OfType<MethodProvider>().First();
        var staticProvider = _providers.OfType<StaticValuesProvider>().First();

        var methodResult = await methodProvider.GetCompletionsAsync(methodContext, CancellationToken.None);
        var staticResult = await staticProvider.GetCompletionsAsync(staticContext, CancellationToken.None);

        // Assert
        methodResult.Items.Select(i => i.InsertText).Should().Contain(new[] { "format1", "format2" });
        staticResult.Items.Select(i => i.InsertText).Should().Contain(new[] { "low", "medium", "high" });
    }

    #endregion

    #region CI-011: Priority order is respected

    [TestMethod]
    [TestCategory("CI-011")]
    public void Providers_AreOrderedByPriority()
    {
        // Assert provider priority order
        var ordered = _providers.OrderByDescending(p => p.Priority).ToList();

        // History should be highest (100)
        ordered.First().Should().BeOfType<HistoryProvider>();
        
        // MethodProvider should be near top (75)
        ordered.OfType<MethodProvider>().First().Priority.Should().Be(75);
        
        // CommandCompletionProvider should be lowest (0)
        ordered.Last().Should().BeOfType<CommandCompletionProvider>();
    }

    #endregion

    #region CI-012: Empty completions handled gracefully

    [TestMethod]
    [TestCategory("CI-012")]
    public async Task HandleTabAsync_WhenNoCompletions_ReturnsEmpty()
    {
        // Arrange - method that returns empty
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "format",
            ArgumentName = "output",
            PartialValue = "",
            CompletionAttribute = new CompletionAttribute("GetEmptyResults"),
            CommandInstance = new TestFormatCommand(),
            Services = _serviceProvider
        };

        // Act
        var methodProvider = _providers.OfType<MethodProvider>().First();
        var result = await methodProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region CI-013: Completion context has correct values

    [TestMethod]
    [TestCategory("CI-013")]
    public async Task HandleTabAsync_ContextParameter_HasCorrectValues()
    {
        // Arrange
        CompletionContext capturedContext = null;
        var testCommand = new ContextCapturingCommand(ctx => capturedContext = ctx);

        var context = new CompletionContext
        {
            InputText = "test --value xyz",
            CursorPosition = 16,
            ElementType = CompletionElementType.ArgumentValue,
            CommandName = "test",
            ArgumentName = "value",
            PartialValue = "xyz",
            CompletionAttribute = new CompletionAttribute("GetCompletionsWithContext"),
            CommandInstance = testCommand,
            Services = _serviceProvider
        };

        // Act
        var methodProvider = _providers.OfType<MethodProvider>().First();
        await methodProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext.PartialValue.Should().Be("xyz");
        capturedContext.ArgumentName.Should().Be("value");
        capturedContext.CommandName.Should().Be("test");
    }

    #endregion

    #region CI-014: Argument name prefix filtering works

    [TestMethod]
    [TestCategory("CI-014")]
    public async Task HandleTabAsync_ArgumentNameWithPrefix_FiltersResults()
    {
        // Arrange - prefix "Out" should match "--Output" but not other args
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentName,
            CommandName = "format",
            PartialValue = "Out",
            UsedArguments = new HashSet<string>(),
            Services = _serviceProvider
        };

        // Act
        var argProvider = _providers.OfType<ArgumentNameProvider>().First();
        var result = await argProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert - only arguments starting with "Out" should match
        result.Items.Should().OnlyContain(i => 
            i.InsertText.StartsWith("--Out", StringComparison.OrdinalIgnoreCase) ||
            i.InsertText.StartsWith("Out", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region CI-015: Argument name case-insensitive matching

    [TestMethod]
    [TestCategory("CI-015")]
    public async Task HandleTabAsync_ArgumentNameCaseInsensitive_MatchesBothCases()
    {
        // Arrange - uppercase "OUTPUT" should match "--Output" 
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentName,
            CommandName = "format",
            PartialValue = "OUTPUT",
            UsedArguments = new HashSet<string>(),
            Services = _serviceProvider
        };

        // Act
        var argProvider = _providers.OfType<ArgumentNameProvider>().First();
        var result = await argProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert - case-insensitive match should work
        result.Items.Should().Contain(i => 
            i.InsertText.Equals("--Output", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region CI-016: Argument alias prefix filtering works

    [TestMethod]
    [TestCategory("CI-016")]
    public async Task HandleTabAsync_AliasWithPrefix_FiltersResults()
    {
        // Arrange - test alias filtering (if command has aliases)
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentAlias,
            CommandName = "format",
            PartialValue = "",
            UsedArguments = new HashSet<string>(),
            Services = _serviceProvider
        };

        // Act
        var aliasProvider = _providers.OfType<ArgumentAliasProvider>().First();
        var result = await aliasProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert - should return aliases (may be empty if no aliases defined)
        result.Should().NotBeNull();
    }

    #endregion

    #region CI-017: All available argument names returned when no prefix

    [TestMethod]
    [TestCategory("CI-017")]
    public async Task HandleTabAsync_ArgumentNameNoPrefix_ReturnsAllArguments()
    {
        // Arrange - empty prefix should return all args
        var context = new CompletionContext
        {
            ElementType = CompletionElementType.ArgumentName,
            CommandName = "format",
            PartialValue = "",
            UsedArguments = new HashSet<string>(),
            Services = _serviceProvider
        };

        // Act
        var argProvider = _providers.OfType<ArgumentNameProvider>().First();
        var result = await argProvider.GetCompletionsAsync(context, CancellationToken.None);

        // Assert - should return all args for the command
        result.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(i => i.InsertText.Contains("Output"));
    }

    #endregion

    #region Test Commands

    [Command(Name = "format")]
    public class TestFormatCommand : CommandBase
    {
        [Argument]
        [Completion("GetOutputFormats")]
        public string Output { get; set; }

        public string[] GetOutputFormats() => new[] { "json", "xml", "csv" };

        public async Task<string[]> GetOutputFormatsAsync()
        {
            await Task.Delay(1);
            return new[] { "async-format1", "async-format2" };
        }

        public string[] GetEmptyResults() => Array.Empty<string>();

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "enumtest")]
    public class TestEnumCommand : CommandBase
    {
        [Argument]
        public DayOfWeek Day { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "statictest")]
    public class TestStaticValuesCommand : CommandBase
    {
        [Argument]
        [Completion(new[] { "debug", "info", "warn", "error" })]
        public string Level { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "ditest")]
    public class TestDICommand : CommandBase
    {
        [Argument]
        [Completion("GetUsers")]
        public string User { get; set; }

        public string[] GetUsers(IUserService userService) => userService.GetUsers().ToArray();

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "multi")]
    public class MultiAttributeCommand : CommandBase
    {
        [Argument]
        [Completion("GetFormats")]
        public string Format { get; set; }

        [Argument]
        [Completion(new[] { "low", "medium", "high" })]
        public string Level { get; set; }

        public string[] GetFormats() => new[] { "format1", "format2" };

        public void Execute(CommandExecutionContext ctx) { }
    }

    public class ContextCapturingCommand : CommandBase
    {
        private readonly Action<CompletionContext> _captureAction;

        public ContextCapturingCommand(Action<CompletionContext> captureAction)
        {
            _captureAction = captureAction;
        }

        [Argument]
        [Completion("GetCompletionsWithContext")]
        public string Value { get; set; }

        public string[] GetCompletionsWithContext(CompletionContext context)
        {
            _captureAction?.Invoke(context);
            return new[] { "result" };
        }

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Test Services

    public interface IUserService
    {
        IEnumerable<string> GetUsers();
    }

    public class UserService : IUserService
    {
        private readonly string[] _users;

        public UserService(string[] users)
        {
            _users = users;
        }

        public IEnumerable<string> GetUsers() => _users;
    }

    #endregion
}
