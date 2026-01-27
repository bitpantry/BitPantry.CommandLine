using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Context;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete
{
    #region Custom Attribute Handlers for Testing

    /// <summary>
    /// A custom handler that provides color suggestions.
    /// </summary>
    public class ColorAutoCompleteHandler : IAutoCompleteHandler
    {
        private static readonly string[] Colors = { "Blue", "Green", "Red", "Yellow" };

        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            var options = new List<AutoCompleteOption>();
            foreach (var color in Colors)
            {
                if (string.IsNullOrEmpty(context.QueryString) || 
                    color.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
                {
                    options.Add(new AutoCompleteOption(color));
                }
            }
            return Task.FromResult(options);
        }
    }

    /// <summary>
    /// A custom handler that provides environment suggestions.
    /// </summary>
    public class EnvironmentAutoCompleteHandler : IAutoCompleteHandler
    {
        private static readonly string[] Environments = { "Development", "Production", "Staging", "Test" };

        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            var options = new List<AutoCompleteOption>();
            foreach (var env in Environments)
            {
                if (string.IsNullOrEmpty(context.QueryString) || 
                    env.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
                {
                    options.Add(new AutoCompleteOption(env));
                }
            }
            return Task.FromResult(options);
        }
    }

    /// <summary>
    /// Syntactic sugar attribute for color autocomplete.
    /// </summary>
    public class ColorAttribute : AutoCompleteAttribute<ColorAutoCompleteHandler> { }

    /// <summary>
    /// Syntactic sugar attribute for environment autocomplete.
    /// </summary>
    public class EnvironmentAttribute : AutoCompleteAttribute<EnvironmentAutoCompleteHandler> { }

    #endregion

    /// <summary>
    /// Unit tests for AutoCompleteController.
    /// The controller orchestrates CursorContextResolver and GhostTextController
    /// to provide intelligent autocomplete suggestions.
    /// </summary>
    [TestClass]
    public class AutoCompleteControllerTests
    {
        private ICommandRegistry _registry;
        private BitPantry.VirtualConsole.VirtualConsole _virtualConsole;
        private VirtualConsoleAnsiAdapter _ansiAdapter;
        private ConsoleLineMirror _line;

        #region Test Enums and Commands

        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        public enum ConnectionMode
        {
            Tcp,
            Udp,
            WebSocket
        }

        [Group]
        [Description("Server operations")]
        private class ServerGroup { }

        [Command(Group = typeof(ServerGroup), Name = "connect")]
        [Description("Connect to server")]
        private class ConnectCommand : CommandBase
        {
            [Argument]
            [Alias('t')]
            [Description("The host to connect to")]
            public string Host { get; set; }

            [Argument]
            [Alias('n')]
            [Description("The port number")]
            public int Port { get; set; }

            [Argument]
            [Alias('m')]
            [Description("Connection mode")]
            public ConnectionMode Mode { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(ServerGroup), Name = "disconnect")]
        [Description("Disconnect from server")]
        private class DisconnectCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "help")]
        [Description("Display help")]
        private class HelpCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "history")]
        [Description("Show command history")]
        private class HistoryCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "exit")]
        [Description("Exit the application")]
        private class ExitCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "log")]
        [Description("Configure logging")]
        private class LogCommand : CommandBase
        {
            [Argument]
            [Alias('l')]
            [Description("The log level")]
            public LogLevel Level { get; set; }

            [Argument]
            [Description("Nullable log level")]
            public LogLevel? OptionalLevel { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with attribute-based autocomplete handlers.
        /// </summary>
        [Command(Name = "theme")]
        [Description("Configure theme settings")]
        private class ThemeCommand : CommandBase
        {
            [Argument]
            [Color]
            [Description("Primary color")]
            public string PrimaryColor { get; set; }

            [Argument]
            [Color]
            [Description("Secondary color")]
            public string SecondaryColor { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with explicit AutoComplete attribute.
        /// </summary>
        [Command(Name = "deploy")]
        [Description("Deploy the application")]
        private class DeployCommand : CommandBase
        {
            [Argument]
            [AutoComplete<EnvironmentAutoCompleteHandler>]
            [Description("Target environment")]
            public string Target { get; set; }

            [Argument]
            [Environment]
            [Description("Source environment")]
            public string Source { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with positional arguments for testing positional value suggestions.
        /// </summary>
        [Command(Name = "copy")]
        [Description("Copy files")]
        private class CopyCommand : CommandBase
        {
            [Argument(Position = 0)]
            [Environment]
            [Description("Source environment")]
            public string SourceEnv { get; set; }

            [Argument(Position = 1)]
            [Color]
            [Description("Color scheme")]
            public string ColorScheme { get; set; }

            [Argument(Position = 2)]
            [Description("File path (no handler)")]
            public string FilePath { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with positional enum argument.
        /// </summary>
        [Command(Name = "setlevel")]
        [Description("Set log level")]
        private class SetLevelCommand : CommandBase
        {
            [Argument(Position = 0)]
            [Description("Log level")]
            public LogLevel Level { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        [TestInitialize]
        public void Setup()
        {
            var builder = new CommandRegistryBuilder();
            builder.RegisterGroup<ServerGroup>();
            builder.RegisterCommand<ConnectCommand>();
            builder.RegisterCommand<DisconnectCommand>();
            builder.RegisterCommand<HelpCommand>();
            builder.RegisterCommand<HistoryCommand>();
            builder.RegisterCommand<ExitCommand>();
            builder.RegisterCommand<LogCommand>();
            builder.RegisterCommand<ThemeCommand>();
            builder.RegisterCommand<DeployCommand>();
            builder.RegisterCommand<CopyCommand>();
            builder.RegisterCommand<SetLevelCommand>();

            _registry = builder.Build();

            _virtualConsole = new BitPantry.VirtualConsole.VirtualConsole(80, 24);
            _ansiAdapter = new VirtualConsoleAnsiAdapter(_virtualConsole);
            _line = new ConsoleLineMirror(_ansiAdapter);
        }

        private IAutoCompleteHandlerRegistry _handlerRegistry;
        private AutoCompleteHandlerActivator _handlerActivator;

        private AutoCompleteController CreateController()
        {
            // Build handler registry - EnumAutoCompleteHandler and BooleanAutoCompleteHandler are registered by default
            // Register custom attribute handlers for testing
            var services = new ServiceCollection();
            services.AddTransient<ColorAutoCompleteHandler>();
            services.AddTransient<EnvironmentAutoCompleteHandler>();
            
            var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
            _handlerRegistry = handlerRegistryBuilder.Build(services);
            
            var serviceProvider = services.BuildServiceProvider();
            _handlerActivator = new AutoCompleteHandlerActivator(serviceProvider);

            return new AutoCompleteController(_registry, _ansiAdapter, _handlerRegistry, _handlerActivator);
        }

        #region Construction Tests

        [TestMethod]
        public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
        {
            // Arrange - need valid handler registry/activator to test registry null check
            var services = new ServiceCollection();
            var handlerRegistry = new AutoCompleteHandlerRegistryBuilder().Build(services);
            var activator = new AutoCompleteHandlerActivator(services.BuildServiceProvider());

            // Act
            Action act = () => new AutoCompleteController(null, _ansiAdapter, handlerRegistry, activator);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("registry");
        }

        [TestMethod]
        public void Constructor_WithNullConsole_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var handlerRegistry = new AutoCompleteHandlerRegistryBuilder().Build(services);
            var activator = new AutoCompleteHandlerActivator(services.BuildServiceProvider());

            // Act
            Action act = () => new AutoCompleteController(_registry, null, handlerRegistry, activator);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("console");
        }

        [TestMethod]
        public void Constructor_WithNullHandlerRegistry_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var activator = new AutoCompleteHandlerActivator(services.BuildServiceProvider());

            // Act
            Action act = () => new AutoCompleteController(_registry, _ansiAdapter, null, activator);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("handlerRegistry");
        }

        [TestMethod]
        public void Constructor_WithNullHandlerActivator_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var handlerRegistry = new AutoCompleteHandlerRegistryBuilder().Build(services);

            // Act
            Action act = () => new AutoCompleteController(_registry, _ansiAdapter, handlerRegistry, null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("handlerActivator");
        }

        #endregion

        #region Initial State Tests

        [TestMethod]
        public void InitialState_IsNotActive()
        {
            // Arrange
            var controller = CreateController();

            // Assert
            controller.IsActive.Should().BeFalse();
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void InitialState_GhostTextIsNull()
        {
            // Arrange
            var controller = CreateController();

            // Assert
            controller.GhostText.Should().BeNull();
        }

        #endregion

        #region Update - Empty Input Tests

        [TestMethod]
        public void Update_EmptyInput_NoGhostText()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("");

            // Act
            controller.Update(_line);

            // Assert - empty input should not show ghost text
            controller.IsActive.Should().BeFalse(
                because: "empty input should not trigger ghost text suggestions");
        }

        [TestMethod]
        public void Update_EmptyInputAfterSuppress_StaysSuppressed()
        {
            // Arrange - type something, suppress, then clear
            var controller = CreateController();
            _line.Write("he");
            controller.Update(_line);
            controller.Suppress(_line);
            
            // Simulate backspacing to empty
            _line.Backspace();
            _line.Backspace();

            // Act
            controller.Update(_line);

            // Assert - should be inactive because input is empty (suppression irrelevant)
            controller.IsActive.Should().BeFalse(
                because: "empty input should not show ghost text regardless of suppression");
        }

        [TestMethod]
        public void Update_EmptyInputAfterDismiss_NoGhostText()
        {
            // Arrange - type something, dismiss, then clear
            var controller = CreateController();
            _line.Write("he");
            controller.Update(_line);
            controller.Dismiss(_line);
            
            // Simulate backspacing to empty
            _line.Backspace();
            _line.Backspace();

            // Act
            controller.Update(_line);

            // Assert - empty input should not show ghost text
            controller.IsActive.Should().BeFalse(
                because: "empty input should not trigger ghost text suggestions");
        }

        #endregion

        #region Update - Partial Command Tests

        [TestMethod]
        public void Update_PartialCommand_SuggestsCompletion()
        {
            // Arrange - "hel" should suggest "help"
            var controller = CreateController();
            _line.Write("hel");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("p"); // Completes "hel" → "help"
        }

        [TestMethod]
        public void Update_PartialCommandWithMultipleMatches_SuggestsFirst()
        {
            // Arrange - "h" matches "help" and "history"
            var controller = CreateController();
            _line.Write("h");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // Handlers return options sorted alphabetically - "help" comes before "history"
            controller.GhostText.Should().Be("elp");
        }

        [TestMethod]
        public void Update_NoMatch_IsNotActive()
        {
            // Arrange - "xyz" matches nothing
            var controller = CreateController();
            _line.Write("xyz");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void Update_ExactMatch_IsNotActive()
        {
            // Arrange - "help" is exact match, nothing more to suggest
            var controller = CreateController();
            _line.Write("help");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
        }

        #endregion

        #region Update - Group Tests

        [TestMethod]
        public void Update_PartialGroup_SuggestsGroupCompletion()
        {
            // Arrange - "ser" should suggest "ver" to complete "server"
            var controller = CreateController();
            _line.Write("ser");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("ver");
        }

        [TestMethod]
        public void Update_AfterGroup_SuggestsCommandInGroup()
        {
            // Arrange - "server " should suggest first command in group
            var controller = CreateController();
            _line.Write("server ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // Handlers return options sorted alphabetically - "connect" comes before "disconnect"
            controller.GhostText.Should().Be("connect");
        }

        [TestMethod]
        public void Update_PartialCommandInGroup_SuggestsCompletion()
        {
            // Arrange - "server con" should suggest "nect"
            var controller = CreateController();
            _line.Write("server con");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("nect");
        }

        #endregion

        #region Update - Argument Name Tests

        [TestMethod]
        public void Update_DoubleDashPrefix_SuggestsArgumentName()
        {
            // Arrange - "server connect --" should suggest first argument
            var controller = CreateController();
            _line.Write("server connect --");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // ConnectCommand has host, mode, port - alphabetically first is "host"
            controller.GhostText.Should().Be("host");
        }

        [TestMethod]
        public void Update_PartialArgumentName_SuggestsCompletion()
        {
            // Arrange - "server connect --ho" should suggest "st"
            var controller = CreateController();
            _line.Write("server connect --ho");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("st");
        }

        [TestMethod]
        public void Update_UsedArgument_ExcludesFromSuggestion()
        {
            // Arrange - "--host" already used, "--" should suggest from remaining args
            var controller = CreateController();
            _line.Write("server connect --host localhost --");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // ConnectCommand has Host, Port, Mode - with Host used, remaining are mode, port
            // Alphabetically first is "mode"
            controller.GhostText.Should().Be("mode");
        }

        #endregion

        #region Update - Argument Alias Tests

        [TestMethod]
        public void Update_SingleDashPrefix_SuggestsAlias()
        {
            // Arrange - "server connect -" should suggest first alias
            var controller = CreateController();
            _line.Write("server connect -");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // ConnectCommand has -m (Mode), -n (Port), -t (Host) - alphabetically first is "m"
            controller.GhostText.Should().Be("m");
        }

        [TestMethod]
        public void Update_PartialAlias_NoMoreToComplete()
        {
            // Arrange - "server connect -t" should have no more to complete
            var controller = CreateController();
            _line.Write("server connect -t");

            // Act
            controller.Update(_line);

            // Assert
            // -t is complete alias, might suggest nothing or space
            controller.IsActive.Should().BeFalse();
        }

        #endregion

        #region Update - Argument Value Tests

        [TestMethod]
        public void Update_AfterArgumentName_NoSuggestion()
        {
            // Arrange - "server connect --host " - value position, no suggestions
            // (Value suggestions would require domain-specific providers)
            var controller = CreateController();
            _line.Write("server connect --host ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
        }

        #endregion

        #region Accept Tests

        [TestMethod]
        public void Accept_WithSuggestion_CommitsToBuffer()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("hel");
            controller.Update(_line);

            // Act
            controller.Accept(_line);

            // Assert - completing "help" adds trailing space since it's a command
            _line.Buffer.Should().Be("help ");
        }

        [TestMethod]
        public void Accept_WithSuggestion_ReturnsToIdle()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("hel");
            controller.Update(_line);

            // Act
            controller.Accept(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
            controller.Mode.Should().Be(AutoCompleteMode.Idle);
        }

        [TestMethod]
        public void Accept_WithoutSuggestion_DoesNothing()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("xyz"); // No match
            controller.Update(_line);

            // Act
            controller.Accept(_line);

            // Assert
            _line.Buffer.Should().Be("xyz");
        }

        [TestMethod]
        public void Accept_GroupSuggestion_AddsSpaceAfter()
        {
            // Arrange - accepting "server" group should add trailing space
            var controller = CreateController();
            _line.Write("ser");
            controller.Update(_line);

            // Act
            controller.Accept(_line);

            // Assert
            _line.Buffer.Should().Be("server ");
        }

        [TestMethod]
        public void Accept_CommandInGroup_AddsSpaceAfter()
        {
            // Arrange - accepting "connect" command should add trailing space
            var controller = CreateController();
            _line.Write("server con");
            controller.Update(_line);

            // Act
            controller.Accept(_line);

            // Assert
            _line.Buffer.Should().Be("server connect ");
        }

        [TestMethod]
        public void Accept_ArgumentName_AddsSpaceAfter()
        {
            // Arrange - accepting "--host" should add trailing space for value
            var controller = CreateController();
            _line.Write("server connect --ho");
            controller.Update(_line);

            // Act
            controller.Accept(_line);

            // Assert
            _line.Buffer.Should().Be("server connect --host ");
        }

        #endregion

        #region Dismiss Tests

        [TestMethod]
        public void Dismiss_WithSuggestion_RemovesSuggestion()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("hel");
            controller.Update(_line);

            // Act
            controller.Dismiss(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
            controller.GhostText.Should().BeNull();
        }

        [TestMethod]
        public void Dismiss_WithoutSuggestion_DoesNothing()
        {
            // Arrange
            var controller = CreateController();

            // Act
            controller.Dismiss(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void Dismiss_AfterUpdate_RemovesGhostTextFromDisplay()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("hel");
            controller.Update(_line);

            // Act
            controller.Dismiss(_line);

            // Assert
            // Ghost text should be cleared from display
            // Buffer should still contain just "hel"
            _line.Buffer.Should().Be("hel");
        }

        #endregion

        #region Update - Renders Ghost Text Tests

        [TestMethod]
        public void Update_WithSuggestion_DisplaysGhostText()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("hel");

            // Act
            controller.Update(_line);

            // Assert - "hel" + ghost "p" should show "help"
            _virtualConsole.GetRow(0).GetText().TrimEnd().Should().Be("help");
        }

        [TestMethod]
        public void Update_WithoutSuggestion_DoesNotModifyDisplay()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("xyz");
            var beforeUpdate = _virtualConsole.GetRow(0).GetText();

            // Act
            controller.Update(_line);

            // Assert
            var afterUpdate = _virtualConsole.GetRow(0).GetText();
            afterUpdate.Should().Be(beforeUpdate);
        }

        #endregion

        #region Case Sensitivity Tests

        [TestMethod]
        public void Update_CaseInsensitiveMatch_SuggestsCompletion()
        {
            // Arrange - "HEL" should match "help"
            var controller = CreateController();
            _line.Write("HEL");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("p");
        }

        [TestMethod]
        public void Update_MixedCase_SuggestsCompletion()
        {
            // Arrange - "SeR" should match "server"
            var controller = CreateController();
            _line.Write("SeR");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("ver");
        }

        #endregion

        #region Multiple Spaces Tests

        [TestMethod]
        public void Update_MultipleSpacesAfterGroup_StillSuggests()
        {
            // Arrange - "server    " should still suggest commands
            var controller = CreateController();
            _line.Write("server    ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // Handlers return options sorted alphabetically - "connect" comes before "disconnect"
            controller.GhostText.Should().Be("connect");
        }

        #endregion

        #region Command Without Arguments Tests

        [TestMethod]
        public void Update_AfterCommandWithNoArgs_NoSuggestion()
        {
            // Arrange - "help " has no arguments
            var controller = CreateController();
            _line.Write("help ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
        }

        #endregion

        #region Argument Value Suggestion Tests

        [TestMethod]
        public void Update_ArgumentValue_WithoutHandler_NoSuggestion()
        {
            // Arrange - "server connect --host " has a string argument with no handler
            // String type has no type handler and no attribute handler
            var controller = CreateController();
            _line.Write("server connect --host ");

            // Act
            controller.Update(_line);

            // Assert - no suggestion since string has no handler
            controller.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void Update_EnumArgumentValue_EmptyQuery_SuggestsFirstEnumValue()
        {
            // Arrange - "log --level " should suggest first enum value alphabetically
            var controller = CreateController();
            _line.Write("log --level ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // LogLevel values: Debug, Error, Info, Warning - alphabetically first is Debug
            controller.GhostText.Should().Be("Debug");
        }

        [TestMethod]
        public void Update_EnumArgumentValue_PartialMatch_SuggestsCompletion()
        {
            // Arrange - "log --level De" should suggest "bug" to complete "Debug"
            var controller = CreateController();
            _line.Write("log --level De");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("bug");
        }

        [TestMethod]
        public void Update_EnumArgumentValue_CaseInsensitive_SuggestsCompletion()
        {
            // Arrange - "log --level wa" should suggest "rning" (case-insensitive match)
            var controller = CreateController();
            _line.Write("log --level wa");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("rning");
        }

        [TestMethod]
        public void Update_EnumArgumentValue_ExactMatch_NoSuggestion()
        {
            // Arrange - "log --level Debug" is exact match
            var controller = CreateController();
            _line.Write("log --level Debug");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void Update_EnumArgumentValue_NoMatch_NoSuggestion()
        {
            // Arrange - "log --level xyz" matches nothing
            var controller = CreateController();
            _line.Write("log --level xyz");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void Update_EnumArgumentValue_ViaAlias_SuggestsEnumValue()
        {
            // Arrange - "log -l " should also suggest enum values
            var controller = CreateController();
            _line.Write("log -l ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("Debug");
        }

        [TestMethod]
        public void Update_NullableEnumArgumentValue_SuggestsEnumValue()
        {
            // Arrange - nullable enum should still suggest values
            var controller = CreateController();
            _line.Write("log --optionallevel ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("Debug");
        }

        [TestMethod]
        public void Accept_EnumArgumentValue_CommitsToBuffer()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("log --level De");
            controller.Update(_line);

            // Act
            controller.Accept(_line);

            // Assert
            _line.Buffer.Should().Be("log --level Debug");
        }

        [TestMethod]
        public void Update_EnumArgumentValue_DifferentCommand_SuggestsCorrectEnum()
        {
            // Arrange - "server connect --mode " should suggest ConnectionMode values
            var controller = CreateController();
            _line.Write("server connect --mode ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // ConnectionMode: Tcp, Udp, WebSocket - alphabetically first is Tcp
            controller.GhostText.Should().Be("Tcp");
        }

        [TestMethod]
        public void Update_EnumArgumentValue_PartialConnectionMode_SuggestsCompletion()
        {
            // Arrange - "server connect --mode We" should suggest "bSocket"
            var controller = CreateController();
            _line.Write("server connect --mode We");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("bSocket");
        }

        #endregion

        #region Attribute Handler Tests - Syntactic Sugar Attribute

        [TestMethod]
        public void Update_AttributeHandler_SyntacticSugar_EmptyQuery_SuggestsFirstValue()
        {
            // Arrange - "theme --primarycolor " should suggest first color
            var controller = CreateController();
            _line.Write("theme --primarycolor ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // Colors: Blue, Green, Red, Yellow - alphabetically first is Blue
            controller.GhostText.Should().Be("Blue");
        }

        [TestMethod]
        public void Update_AttributeHandler_SyntacticSugar_PartialMatch_SuggestsCompletion()
        {
            // Arrange - "theme --primarycolor Gr" should suggest "een"
            var controller = CreateController();
            _line.Write("theme --primarycolor Gr");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("een");
        }

        [TestMethod]
        public void Update_AttributeHandler_SyntacticSugar_CaseInsensitive_SuggestsCompletion()
        {
            // Arrange - "theme --primarycolor ye" should suggest "llow"
            var controller = CreateController();
            _line.Write("theme --primarycolor ye");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("llow");
        }

        [TestMethod]
        public void Update_AttributeHandler_SyntacticSugar_DifferentArgument_UsesHandler()
        {
            // Arrange - both primarycolor and secondarycolor use [Color] attribute
            var controller = CreateController();
            _line.Write("theme --secondarycolor Re");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("d");
        }

        [TestMethod]
        public void Accept_AttributeHandler_SyntacticSugar_CommitsToBuffer()
        {
            // Arrange
            var controller = CreateController();
            _line.Write("theme --primarycolor Bl");
            controller.Update(_line);

            // Act
            controller.Accept(_line);

            // Assert
            _line.Buffer.Should().Be("theme --primarycolor Blue");
        }

        #endregion

        #region Attribute Handler Tests - Explicit Generic Attribute

        [TestMethod]
        public void Update_AttributeHandler_ExplicitGeneric_EmptyQuery_SuggestsFirstValue()
        {
            // Arrange - "deploy --target " should suggest first environment
            var controller = CreateController();
            _line.Write("deploy --target ");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            // Environments: Development, Production, Staging, Test - first is Development
            controller.GhostText.Should().Be("Development");
        }

        [TestMethod]
        public void Update_AttributeHandler_ExplicitGeneric_PartialMatch_SuggestsCompletion()
        {
            // Arrange - "deploy --target Pro" should suggest "duction"
            var controller = CreateController();
            _line.Write("deploy --target Pro");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("duction");
        }

        [TestMethod]
        public void Update_AttributeHandler_InheritedVsExplicit_BothWork()
        {
            // Arrange - "deploy --source " uses [Environment] (inherited), should work
            var controller = CreateController();
            _line.Write("deploy --source Sta");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("ging");
        }

        [TestMethod]
        public void Update_AttributeHandler_NoMatch_NoSuggestion()
        {
            // Arrange - "theme --primarycolor xyz" matches no color
            var controller = CreateController();
            _line.Write("theme --primarycolor xyz");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void Update_AttributeHandler_ExactMatch_NoSuggestion()
        {
            // Arrange - "theme --primarycolor Blue" is an exact match
            var controller = CreateController();
            _line.Write("theme --primarycolor Blue");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeFalse();
        }

        #endregion

        #region Attribute Handler Takes Precedence Over Type Handler

        [TestMethod]
        public void Update_AttributeHandler_TakesPrecedenceOverTypeHandler()
        {
            // String type has no type handler, but [Color] attribute provides one
            // This tests that attribute handlers work on types that wouldn't
            // otherwise have autocomplete support
            var controller = CreateController();
            _line.Write("theme --primarycolor ");

            // Act
            controller.Update(_line);

            // Assert - should get color suggestions, not nothing
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("Blue");
        }

        #endregion

        #region Positional Value Suggestion Tests

        [TestMethod]
        public void Update_PositionalValue_FirstPosition_WithAttributeHandler_SuggestsValue()
        {
            // Arrange - "copy " should suggest first positional arg value (Environment)
            // SourceEnv is Position=0 with [Environment] attribute
            var controller = CreateController();
            _line.Write("copy ");

            // Act
            controller.Update(_line);

            // Assert - should suggest first Environment value alphabetically
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("Development");
        }

        [TestMethod]
        public void Update_PositionalValue_FirstPosition_PartialMatch_SuggestsCompletion()
        {
            // Arrange - "copy Pro" should suggest "duction" to complete "Production"
            var controller = CreateController();
            _line.Write("copy Pro");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("duction");
        }

        [TestMethod]
        public void Update_PositionalValue_SecondPosition_WithAttributeHandler_SuggestsValue()
        {
            // Arrange - "copy Production " should suggest second positional arg value (Color)
            // ColorScheme is Position=1 with [Color] attribute
            var controller = CreateController();
            _line.Write("copy Production ");

            // Act
            controller.Update(_line);

            // Assert - should suggest first Color value alphabetically
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("Blue");
        }

        [TestMethod]
        public void Update_PositionalValue_SecondPosition_PartialMatch_SuggestsCompletion()
        {
            // Arrange - "copy Production Gr" should suggest "een" to complete "Green"
            var controller = CreateController();
            _line.Write("copy Production Gr");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("een");
        }

        [TestMethod]
        public void Update_PositionalValue_ThirdPosition_WithoutHandler_NoSuggestion()
        {
            // Arrange - "copy Production Blue " is at third positional (FilePath - no handler)
            var controller = CreateController();
            _line.Write("copy Production Blue ");

            // Act
            controller.Update(_line);

            // Assert - no suggestion since FilePath has no handler
            controller.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void Update_PositionalValue_WithEnumType_SuggestsEnumValue()
        {
            // Arrange - "setlevel " should suggest LogLevel enum values
            var controller = CreateController();
            _line.Write("setlevel ");

            // Act
            controller.Update(_line);

            // Assert - LogLevel: Debug, Error, Info, Warning - alphabetically first is Debug
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("Debug");
        }

        [TestMethod]
        public void Update_PositionalValue_WithEnumType_PartialMatch_SuggestsCompletion()
        {
            // Arrange - "setlevel Wa" should suggest "rning"
            var controller = CreateController();
            _line.Write("setlevel Wa");

            // Act
            controller.Update(_line);

            // Assert
            controller.IsActive.Should().BeTrue();
            controller.GhostText.Should().Be("rning");
        }

        [TestMethod]
        public void Update_PositionalValue_ExactMatch_NoSuggestion()
        {
            // Arrange - "setlevel Debug" is exact match
            var controller = CreateController();
            _line.Write("setlevel Debug");

            // Act
            controller.Update(_line);

            // Assert - exact match, no suggestion
            controller.IsActive.Should().BeFalse();
        }

        [TestMethod]
        public void Accept_PositionalValue_NoTrailingSpace()
        {
            // Arrange - Positional values don't get trailing space (same as argument values)
            var controller = CreateController();
            _line.Write("setlevel ");
            controller.Update(_line);

            // Act
            controller.Accept(_line);

            // Assert - "Debug" is accepted without trailing space
            _line.Buffer.Should().Be("setlevel Debug");
        }

        #endregion
    }
}
