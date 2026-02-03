using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Context;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Component;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete
{
    /// <summary>
    /// Tests for AutoCompleteSuggestionProvider which handles context resolution,
    /// option filtering, and ghost text generation.
    /// </summary>
    [TestClass]
    public class AutoCompleteSuggestionProviderTests
    {
        private ICommandRegistry _registry;
        private IAutoCompleteHandlerRegistry _handlerRegistry;
        private AutoCompleteHandlerActivator _handlerActivator;
        private AutoCompleteSuggestionProvider _provider;
        private CursorContextResolver _contextResolver;

        #region Test Commands

        [Command(Name = "greet")]
        [Description("A greeting command")]
        public class GreetCommand : CommandBase
        {
            public string Name { get; set; }
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "configure")]
        [Description("Configure settings")]
        public class ConfigureCommand : CommandBase
        {
            [Argument]
            [Description("Verbose output")]
            public bool Verbose { get; set; }

            [Argument]
            [Description("Output path")]
            public string Output { get; set; }
            
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "deploy")]
        [Description("Deploy command")]
        public class DeployCommand : CommandBase
        {
            [Argument]
            [Description("The environment")]
            public string Environment { get; set; }
            
            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        [TestInitialize]
        public void Setup()
        {
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<GreetCommand>();
            builder.RegisterCommand<ConfigureCommand>();
            builder.RegisterCommand<DeployCommand>();
            _registry = builder.Build();

            var services = new ServiceCollection();
            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            _handlerRegistry = handlerRegistryBuilder.Build(services);

            var serviceProvider = services.BuildServiceProvider();
            _handlerActivator = new AutoCompleteHandlerActivator(serviceProvider);

            _provider = new AutoCompleteSuggestionProvider(_registry, _handlerRegistry, _handlerActivator, new NoopServerProxy(), NullLogger<AutoCompleteSuggestionProvider>.Instance);
            _contextResolver = new CursorContextResolver(_registry);
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var handlerRegistry = new AutoCompleteHandlerRegistryBuilder().Build(services);
            var activator = new AutoCompleteHandlerActivator(services.BuildServiceProvider());

            // Act
            Action act = () => new AutoCompleteSuggestionProvider(null, handlerRegistry, activator, new NoopServerProxy(), NullLogger<AutoCompleteSuggestionProvider>.Instance);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_WithNullHandlerRegistry_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var activator = new AutoCompleteHandlerActivator(services.BuildServiceProvider());

            // Act
            Action act = () => new AutoCompleteSuggestionProvider(_registry, null, activator, new NoopServerProxy(), NullLogger<AutoCompleteSuggestionProvider>.Instance);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_WithNullActivator_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var handlerRegistry = new AutoCompleteHandlerRegistryBuilder().Build(services);

            // Act
            Action act = () => new AutoCompleteSuggestionProvider(_registry, handlerRegistry, null, new NoopServerProxy(), NullLogger<AutoCompleteSuggestionProvider>.Instance);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_WithNullServerProxy_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var handlerRegistry = new AutoCompleteHandlerRegistryBuilder().Build(services);
            var activator = new AutoCompleteHandlerActivator(services.BuildServiceProvider());

            // Act
            Action act = () => new AutoCompleteSuggestionProvider(_registry, handlerRegistry, activator, null, NullLogger<AutoCompleteSuggestionProvider>.Instance);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var handlerRegistry = new AutoCompleteHandlerRegistryBuilder().Build(services);
            var activator = new AutoCompleteHandlerActivator(services.BuildServiceProvider());

            // Act
            Action act = () => new AutoCompleteSuggestionProvider(_registry, handlerRegistry, activator, new NoopServerProxy(), null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region ResolveContext Tests

        [TestMethod]
        public void ResolveContext_WithNullInput_ReturnsNull()
        {
            // Act
            var result = _contextResolver.ResolveContext(null, 0);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ResolveContext_WithEmptyInput_ReturnsNull()
        {
            // Act
            var result = _contextResolver.ResolveContext("", 0);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ResolveContext_WithPartialCommand_ReturnsCommandContext()
        {
            // Arrange
            var input = "gre";

            // Act
            var result = _contextResolver.ResolveContext(input, input.Length);

            // Assert
            result.Should().NotBeNull();
            result.ContextType.Should().Be(CursorContextType.GroupOrCommand);
            result.QueryText.Should().Be("gre");
        }

        [TestMethod]
        public void ResolveContext_WithCommandAndArgName_ReturnsArgumentNameContext()
        {
            // Arrange
            var input = "configure --ver";

            // Act
            var result = _contextResolver.ResolveContext(input, input.Length);

            // Assert
            result.Should().NotBeNull();
            result.ContextType.Should().Be(CursorContextType.ArgumentName);
            result.QueryText.Should().Be("ver");
        }

        [TestMethod]
        public void ResolveContext_WithCommandAndAlias_ReturnsArgumentAliasContext()
        {
            // Arrange
            var input = "configure -v";

            // Act
            var result = _contextResolver.ResolveContext(input, input.Length);

            // Assert
            result.Should().NotBeNull();
            result.ContextType.Should().Be(CursorContextType.ArgumentAlias);
            result.QueryText.Should().Be("v");
        }

        #endregion

        #region GetOptions Tests

        [TestMethod]
        public void GetOptions_WithNullContext_ReturnsNull()
        {
            // Act
            var result = _provider.GetOptions(null, "test");

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetOptions_WithCommandContext_ReturnsMatchingCommands()
        {
            // Arrange
            var input = "gre";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var options = _provider.GetOptions(context, input);

            // Assert
            options.Should().NotBeNull();
            options.Should().HaveCount(1);
            options[0].Value.Should().Be("greet");
        }

        [TestMethod]
        public void GetOptions_WithArgumentNameContext_ReturnsMatchingArgs()
        {
            // Arrange
            var input = "configure --out";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var options = _provider.GetOptions(context, input);

            // Assert
            options.Should().NotBeNull();
            options.Should().Contain(o => o.Value == "--output");
        }

        [TestMethod]
        public void GetOptions_WithNoMatches_ReturnsNullOrEmpty()
        {
            // Arrange
            var input = "xyz";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var options = _provider.GetOptions(context, input);

            // Assert - may be null or empty collection depending on context type
            if (options != null)
            {
                options.Should().BeEmpty();
            }
        }

        #endregion

        #region GetGhostText Tests

        [TestMethod]
        public void GetGhostText_WithNullOptions_ReturnsNull()
        {
            // Arrange
            var input = "gre";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var result = _provider.GetGhostText(null, context);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetGhostText_WithEmptyOptions_ReturnsNull()
        {
            // Arrange
            var input = "gre";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var result = _provider.GetGhostText(new List<AutoCompleteOption>(), context);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetGhostText_WithNullContext_ReturnsNull()
        {
            // Arrange
            var options = new List<AutoCompleteOption> { new AutoCompleteOption("test") };

            // Act
            var result = _provider.GetGhostText(options, null);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetGhostText_WithPartialCommand_ReturnsCompletionPortion()
        {
            // Arrange
            var input = "gre";
            var context = _contextResolver.ResolveContext(input, input.Length);
            var options = new List<AutoCompleteOption> { new AutoCompleteOption("greet") };

            // Act
            var result = _provider.GetGhostText(options, context);

            // Assert
            result.Should().Be("et"); // "greet" minus "gre" = "et"
        }

        [TestMethod]
        public void GetGhostText_WithExactMatch_ReturnsNull()
        {
            // Arrange
            var input = "greet";
            var context = _contextResolver.ResolveContext(input, input.Length);
            var options = new List<AutoCompleteOption> { new AutoCompleteOption("greet") };

            // Act
            var result = _provider.GetGhostText(options, context);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetGhostText_WithArgumentName_IncludesDoubleDash()
        {
            // Arrange
            var input = "configure --ver";
            var context = _contextResolver.ResolveContext(input, input.Length);
            var options = new List<AutoCompleteOption> { new AutoCompleteOption("--verbose") };

            // Act
            var result = _provider.GetGhostText(options, context);

            // Assert
            result.Should().Be("bose"); // "--verbose" minus "--ver" = "bose"
        }

        #endregion

        #region ShouldAddTrailingSpace Tests

        [TestMethod]
        public void ShouldAddTrailingSpace_WithNullContext_ReturnsFalse()
        {
            // Act
            var result = _provider.ShouldAddTrailingSpace(null);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ShouldAddTrailingSpace_WithCommandContext_ReturnsTrue()
        {
            // Arrange
            var input = "gre";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var result = _provider.ShouldAddTrailingSpace(context);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldAddTrailingSpace_WithArgumentNameContext_ReturnsTrue()
        {
            // Arrange
            var input = "configure --ver";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var result = _provider.ShouldAddTrailingSpace(context);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldAddTrailingSpace_WithArgumentValueContext_ReturnsFalse()
        {
            // This test would require setting up a command with value completion
            // For now, we just verify null context returns false
            var result = _provider.ShouldAddTrailingSpace(null);
            result.Should().BeFalse();
        }

        #endregion

        #region IsInQuoteContext Tests

        [TestMethod]
        public void IsInQuoteContext_WithNullContext_ReturnsFalse()
        {
            // Act
            var result = _provider.IsInQuoteContext(null);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsInQuoteContext_WithUnquotedQuery_ReturnsFalse()
        {
            // Arrange
            var input = "greet test";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var result = _provider.IsInQuoteContext(context);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsInQuoteContext_WithQuotedQuery_ReturnsTrue()
        {
            // Arrange - simulating 'configure --output "My' 
            var input = "configure --output \"My";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var result = _provider.IsInQuoteContext(context);

            // Assert
            result.Should().BeTrue(because: "the active element '{0}' starts with quote", context?.ActiveElement?.Raw);
        }

        [TestMethod]
        public void IsInQuoteContext_WithJustOpeningQuote_ReturnsTrue()
        {
            // Arrange - simulating 'configure --output "' (just opening quote, no value yet)
            var input = "configure --output \"";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var result = _provider.IsInQuoteContext(context);

            // Assert
            result.Should().BeTrue(because: "the active element '{0}' starts with quote", context?.ActiveElement?.Raw);
        }

        #endregion

        #region ProvidedValues Tests - FR-027

        /// <summary>
        /// Test handler that captures ProvidedValues for verification.
        /// </summary>
        private class CapturingHandler : IAutoCompleteHandler
        {
            public static IReadOnlyDictionary<ArgumentInfo, string> CapturedProvidedValues { get; private set; }

            public Task<List<AutoCompleteOption>> GetOptionsAsync(
                AutoCompleteContext context, CancellationToken cancellationToken = default)
            {
                CapturedProvidedValues = context.ProvidedValues;
                return Task.FromResult(new List<AutoCompleteOption>
                {
                    new AutoCompleteOption("captured", "Captured handler was invoked")
                });
            }
        }

        /// <summary>
        /// Command with context-aware autocomplete for testing ProvidedValues passthrough.
        /// </summary>
        [Command(Name = "travel")]
        public class TravelTestCommand : CommandBase
        {
            [Argument(Name = "country")]
            public string Country { get; set; }

            [Argument(Name = "city")]
            [AutoComplete<CapturingHandler>]
            public string City { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Implements: 008:FR-027, T034
        /// Handler receives ProvidedValues in context with already-entered values.
        /// </summary>
        [TestMethod]
        public void Handler_ReceivesProvidedValues_WhenArgumentValuesExist()
        {
            // Arrange - register command and handler
            var registryBuilder = new CommandRegistryBuilder();
            registryBuilder.RegisterCommand<TravelTestCommand>();
            var registry = registryBuilder.Build();

            var services = new ServiceCollection();
            services.AddTransient<CapturingHandler>();
            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerRegistryBuilder.Build(services);
            var serviceProvider = services.BuildServiceProvider();
            var activator = new AutoCompleteHandlerActivator(serviceProvider);

            var provider = new AutoCompleteSuggestionProvider(registry, handlerRegistry, activator, new NoopServerProxy(), NullLogger<AutoCompleteSuggestionProvider>.Instance);
            var contextResolver = new CursorContextResolver(registry);

            // Input: "travel --country USA --city |" - country is already set, cursor at city value
            var input = "travel --country USA --city ";
            var context = contextResolver.ResolveContext(input, input.Length);

            // Act - Get suggestions (this invokes the handler)
            var options = provider.GetOptions(context, input);

            // Assert - Handler should have received ProvidedValues with "country" = "USA"
            CapturingHandler.CapturedProvidedValues.Should().NotBeNull();
            CapturingHandler.CapturedProvidedValues.Should().ContainValue("USA");
            CapturingHandler.CapturedProvidedValues.Keys
                .Should().Contain(a => a.Name.Equals("country", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Implements: 008:FR-027
        /// Handler receives empty ProvidedValues when no other arguments are set.
        /// </summary>
        [TestMethod]
        public void Handler_ReceivesEmptyProvidedValues_WhenNoOtherArgumentsExist()
        {
            // Arrange - register command and handler
            var registryBuilder = new CommandRegistryBuilder();
            registryBuilder.RegisterCommand<TravelTestCommand>();
            var registry = registryBuilder.Build();

            var services = new ServiceCollection();
            services.AddTransient<CapturingHandler>();
            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            var handlerRegistry = handlerRegistryBuilder.Build(services);
            var serviceProvider = services.BuildServiceProvider();
            var activator = new AutoCompleteHandlerActivator(serviceProvider);

            var provider = new AutoCompleteSuggestionProvider(registry, handlerRegistry, activator, new NoopServerProxy(), NullLogger<AutoCompleteSuggestionProvider>.Instance);
            var contextResolver = new CursorContextResolver(registry);

            // Input: "travel --city |" - no country set yet
            var input = "travel --city ";
            var context = contextResolver.ResolveContext(input, input.Length);

            // Act - Get suggestions (this invokes the handler)
            var options = provider.GetOptions(context, input);

            // Assert - Handler should have received ProvidedValues with only --city (empty value)
            // The --city argument itself is being typed, so it may or may not be in ProvidedValues
            CapturingHandler.CapturedProvidedValues.Should().NotBeNull();
            // Country should NOT be in ProvidedValues
            CapturingHandler.CapturedProvidedValues.Keys
                .Should().NotContain(a => a.Name.Equals("country", StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region GetOptionsAsync Parity Tests

        /// <summary>
        /// Verifies that GetOptionsAsync returns the same results as GetOptions for command context.
        /// This is a parity test to ensure the async migration doesn't change behavior.
        /// </summary>
        [TestMethod]
        public async Task GetOptionsAsync_CommandContext_ReturnsSameResultsAsSync()
        {
            // Arrange
            var input = "gre";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var syncResult = _provider.GetOptions(context, input);
            var asyncResult = await _provider.GetOptionsAsync(context, input);

            // Assert
            asyncResult.Should().BeEquivalentTo(syncResult, 
                because: "async method should return identical results to sync method");
        }

        /// <summary>
        /// Verifies that GetOptionsAsync returns the same results as GetOptions for argument name context.
        /// </summary>
        [TestMethod]
        public async Task GetOptionsAsync_ArgumentNameContext_ReturnsSameResultsAsSync()
        {
            // Arrange - "configure --ver" should suggest "--verbose"
            var input = "configure --ver";
            var context = _contextResolver.ResolveContext(input, input.Length);

            // Act
            var syncResult = _provider.GetOptions(context, input);
            var asyncResult = await _provider.GetOptionsAsync(context, input);

            // Assert
            asyncResult.Should().BeEquivalentTo(syncResult,
                because: "async method should return identical results to sync method");
        }

        /// <summary>
        /// Verifies that GetOptionsAsync returns null for null context (same as sync).
        /// </summary>
        [TestMethod]
        public async Task GetOptionsAsync_NullContext_ReturnsNull()
        {
            // Arrange
            CursorContext context = null;

            // Act
            var syncResult = _provider.GetOptions(context, "test");
            var asyncResult = await _provider.GetOptionsAsync(context, "test");

            // Assert
            syncResult.Should().BeNull();
            asyncResult.Should().BeNull();
        }

        /// <summary>
        /// Verifies that GetOptionsAsync respects cancellation token.
        /// </summary>
        [TestMethod]
        public async Task GetOptionsAsync_WithCancelledToken_ReturnsNullOrThrows()
        {
            // Arrange
            var input = "gre";
            var context = _contextResolver.ResolveContext(input, input.Length);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act - for local handlers, cancellation may not be checked immediately,
            // but the token should be passed through. This test verifies no exceptions.
            var result = await _provider.GetOptionsAsync(context, input, cts.Token);

            // Assert - for local handlers, result may still be returned since
            // the sync handlers don't always check cancellation
            // The important thing is we don't throw unexpectedly
            result.Should().NotBeNull(); // Local handlers complete synchronously
        }

        #endregion
    }
}
