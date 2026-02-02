using System;
using System.Linq;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Description;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Commands;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests
{
    /// <summary>
    /// Tests for the [Flag] attribute which marks bool properties as presence-only flags.
    /// These tests follow TDD - written before implementation to define expected behavior.
    /// </summary>
    [TestClass]
    public class FlagAttributeTests
    {
        #region Test Commands

        /// <summary>
        /// Valid command with [Flag] on a bool property.
        /// </summary>
        [Command(Name = "validFlag")]
        private class ValidFlagCommand : CommandBase
        {
            [Argument]
            [Flag]
            public bool Verbose { get; set; }

            [Argument]
            [Alias('f')]
            [Flag]
            public bool Force { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Invalid: [Flag] on a string property - should throw at registration.
        /// </summary>
        [Command(Name = "invalidFlagOnString")]
        private class InvalidFlagOnStringCommand : CommandBase
        {
            [Argument]
            [Flag]
            public string BadFlag { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Invalid: [Flag] on an int property - should throw at registration.
        /// </summary>
        [Command(Name = "invalidFlagOnInt")]
        private class InvalidFlagOnIntCommand : CommandBase
        {
            [Argument]
            [Flag]
            public int BadFlag { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with both flag and non-flag bool properties.
        /// Demonstrates that non-flag bools still require values.
        /// </summary>
        [Command(Name = "mixedBool")]
        private class MixedBoolCommand : CommandBase
        {
            [Argument]
            [Flag]
            public bool IsFlag { get; set; }

            [Argument]
            public bool RequiresValue { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        #region VAL: Validation Tests (Command Registration)

        /// <summary>
        /// VAL-001: [Flag] attribute on non-bool property throws CommandDescriptionException at registration.
        /// </summary>
        [TestMethod]
        public void Describe_FlagOnStringProperty_ThrowsCommandDescriptionException()
        {
            // Act
            Action act = () => CommandReflection.Describe<InvalidFlagOnStringCommand>();

            // Assert
            act.Should().Throw<CommandDescriptionException>()
                .WithMessage("*[Flag]*bool*");
        }

        /// <summary>
        /// VAL-001b: [Flag] attribute on int property throws CommandDescriptionException.
        /// </summary>
        [TestMethod]
        public void Describe_FlagOnIntProperty_ThrowsCommandDescriptionException()
        {
            // Act
            Action act = () => CommandReflection.Describe<InvalidFlagOnIntCommand>();

            // Assert
            act.Should().Throw<CommandDescriptionException>()
                .WithMessage("*[Flag]*bool*");
        }

        /// <summary>
        /// VAL-002: [Flag] on bool property is valid - no exception thrown.
        /// </summary>
        [TestMethod]
        public void Describe_FlagOnBoolProperty_Succeeds()
        {
            // Act
            var info = CommandReflection.Describe<ValidFlagCommand>();

            // Assert
            info.Should().NotBeNull();
            info.Arguments.Should().HaveCount(2);
        }

        /// <summary>
        /// VAL-002b: ArgumentInfo.IsFlag is true for [Flag] properties.
        /// </summary>
        [TestMethod]
        public void Describe_FlagProperty_HasIsFlagTrue()
        {
            // Act
            var info = CommandReflection.Describe<ValidFlagCommand>();

            // Assert
            var verboseArg = info.Arguments.Single(a => a.Name == "Verbose");
            verboseArg.IsFlag.Should().BeTrue();
        }

        /// <summary>
        /// VAL-002c: ArgumentInfo.IsFlag is false for non-[Flag] bool properties.
        /// </summary>
        [TestMethod]
        public void Describe_NonFlagBoolProperty_HasIsFlagFalse()
        {
            // Act
            var info = CommandReflection.Describe<MixedBoolCommand>();

            // Assert
            var requiresValueArg = info.Arguments.Single(a => a.Name == "RequiresValue");
            requiresValueArg.IsFlag.Should().BeFalse();
        }

        #endregion

        #region RES: Resolution Tests

        private CommandResolver _resolver;
        private IServiceCollection _services;

        private void SetupResolver<T>() where T : CommandBase
        {
            _services = new ServiceCollection();
            _services.AddTransient<T>();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<T>();
            var registry = builder.Build(_services);
            _resolver = new CommandResolver(registry);
        }

        /// <summary>
        /// RES-001: Flag argument with value produces UnexpectedValue error.
        /// </summary>
        [TestMethod]
        public void Resolve_FlagWithValue_ReturnsUnexpectedValueError()
        {
            // Arrange
            SetupResolver<ValidFlagCommand>();
            var input = new ParsedCommand("validFlag --verbose true");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Type == CommandResolutionErrorType.UnexpectedValue);
        }

        /// <summary>
        /// RES-001b: Flag argument (alias) with value produces UnexpectedValue error.
        /// </summary>
        [TestMethod]
        public void Resolve_FlagAliasWithValue_ReturnsUnexpectedValueError()
        {
            // Arrange
            SetupResolver<ValidFlagCommand>();
            var input = new ParsedCommand("validFlag -f yes");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Type == CommandResolutionErrorType.UnexpectedValue);
        }

        /// <summary>
        /// RES-002: Non-flag bool without value produces MissingArgumentValue error.
        /// </summary>
        [TestMethod]
        public void Resolve_NonFlagBoolWithoutValue_ReturnsMissingArgumentValueError()
        {
            // Arrange
            SetupResolver<MixedBoolCommand>();
            var input = new ParsedCommand("mixedBool --requiresValue");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Type == CommandResolutionErrorType.MissingArgumentValue);
        }

        /// <summary>
        /// RES-003: Flag without value is valid.
        /// </summary>
        [TestMethod]
        public void Resolve_FlagWithoutValue_IsValid()
        {
            // Arrange
            SetupResolver<ValidFlagCommand>();
            var input = new ParsedCommand("validFlag --verbose");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region ACT: Activation Tests

        private CommandActivator _activator;

        private void SetupActivator<T>() where T : CommandBase
        {
            _services = new ServiceCollection();
            _services.AddTransient<T>();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<T>();
            var registry = builder.Build(_services);
            _resolver = new CommandResolver(registry);
            _activator = new CommandActivator(_services.BuildServiceProvider());
        }

        /// <summary>
        /// ACT-001: Flag present sets bool property to true.
        /// </summary>
        [TestMethod]
        public void Activate_FlagPresent_SetsBoolTrue()
        {
            // Arrange
            SetupActivator<ValidFlagCommand>();
            var input = new ParsedCommand("validFlag --verbose");
            var resolved = _resolver.Resolve(input);

            // Act
            var result = _activator.Activate(resolved);

            // Assert
            result.Command.Should().NotBeNull();
            var cmd = result.Command as ValidFlagCommand;
            cmd.Verbose.Should().BeTrue();
        }

        /// <summary>
        /// ACT-002: Flag absent sets bool property to false.
        /// </summary>
        [TestMethod]
        public void Activate_FlagAbsent_SetsBoolFalse()
        {
            // Arrange
            SetupActivator<ValidFlagCommand>();
            var input = new ParsedCommand("validFlag");
            var resolved = _resolver.Resolve(input);

            // Act
            var result = _activator.Activate(resolved);

            // Assert
            result.Command.Should().NotBeNull();
            var cmd = result.Command as ValidFlagCommand;
            cmd.Verbose.Should().BeFalse();
            cmd.Force.Should().BeFalse();
        }

        /// <summary>
        /// ACT-003: Flag alias present sets bool property to true.
        /// </summary>
        [TestMethod]
        public void Activate_FlagAliasPresent_SetsBoolTrue()
        {
            // Arrange
            SetupActivator<ValidFlagCommand>();
            var input = new ParsedCommand("validFlag -f");
            var resolved = _resolver.Resolve(input);

            // Act
            var result = _activator.Activate(resolved);

            // Assert
            result.Command.Should().NotBeNull();
            var cmd = result.Command as ValidFlagCommand;
            cmd.Force.Should().BeTrue();
        }

        /// <summary>
        /// ACT-004: Multiple flags can be set independently.
        /// </summary>
        [TestMethod]
        public void Activate_MultipleFlags_SetsEachCorrectly()
        {
            // Arrange
            SetupActivator<ValidFlagCommand>();
            var input = new ParsedCommand("validFlag --verbose -f");
            var resolved = _resolver.Resolve(input);

            // Act
            var result = _activator.Activate(resolved);

            // Assert
            result.Command.Should().NotBeNull();
            var cmd = result.Command as ValidFlagCommand;
            cmd.Verbose.Should().BeTrue();
            cmd.Force.Should().BeTrue();
        }

        #endregion

        #region Autocomplete Tests (AC)

        /// <summary>
        /// AC-001: [Flag] arguments should NOT get value suggestions in autocomplete.
        /// When cursor is after "--verbose " on a flag, no argument info should be returned
        /// for value completion (since flags don't take values).
        /// </summary>
        [TestMethod]
        public void AutoComplete_FlagArgument_DoesNotSuggestValues()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<ValidFlagCommand>();
            var registry = builder.Build(services);
            var serviceProvider = services.BuildServiceProvider();

            var resolver = new BitPantry.CommandLine.AutoComplete.Context.CursorContextResolver(registry);

            // Act - cursor is after "--verbose " (flag with space, expecting value position)
            // Cursor position is 1-based, so position = length + 1 for end of string
            var input = "validFlag --verbose ";
            var context = resolver.Resolve(input, input.Length + 1);

            // Assert - Flag arguments should not return argument info for value completion
            // The context should NOT have an argument for value completion
            context.TargetArgument.Should().BeNull("because [Flag] arguments don't accept values");
        }

        /// <summary>
        /// AC-002: Non-flag bool arguments SHOULD get value suggestions (true/false).
        /// </summary>
        [TestMethod]
        public void AutoComplete_NonFlagBoolArgument_DoesSuggestValues()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<MixedBoolCommand>();
            var registry = builder.Build(services);
            var serviceProvider = services.BuildServiceProvider();

            var resolver = new BitPantry.CommandLine.AutoComplete.Context.CursorContextResolver(registry);

            // Act - cursor is after "--RequiresValue " (non-flag bool with space)
            // String: "mixedBool --RequiresValue " (length 26)
            // Cursor position is 1-based, so position 27 = end of string
            var input = "mixedBool --RequiresValue ";
            var context = resolver.Resolve(input, input.Length + 1);

            // Assert - Verify the command was resolved
            context.ResolvedCommand.Should().NotBeNull("because the command should be found");
            context.ResolvedCommand.Name.Should().Be("mixedBool");
            
            // Context should be ArgumentValue type when expecting a value
            context.ContextType.Should().Be(BitPantry.CommandLine.AutoComplete.Context.CursorContextType.ArgumentValue,
                "because cursor is after an argument name expecting a value");

            // Non-flag bool arguments SHOULD return argument info for value completion
            context.TargetArgument.Should().NotBeNull("because non-flag bool arguments accept true/false values");
            context.TargetArgument.Name.Should().Be("RequiresValue");
        }

        /// <summary>
        /// AC-003: [Flag] alias should also not get value suggestions.
        /// </summary>
        [TestMethod]
        public void AutoComplete_FlagAlias_DoesNotSuggestValues()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<ValidFlagCommand>();
            var registry = builder.Build(services);
            var serviceProvider = services.BuildServiceProvider();

            var resolver = new BitPantry.CommandLine.AutoComplete.Context.CursorContextResolver(registry);

            // Act - cursor is after "-f " (flag alias with space)
            // Cursor position is 1-based, so position = length + 1 for end of string
            var input = "validFlag -f ";
            var context = resolver.Resolve(input, input.Length + 1);

            // Assert - Flag arguments via alias should not return argument info for value completion
            context.TargetArgument.Should().BeNull("because [Flag] arguments don't accept values even when accessed via alias");
        }

        #endregion

        #region Help Tests (HELP)

        /// <summary>
        /// HELP-001: [Flag] arguments should NOT show <value> in help output.
        /// Flags are presence-only, so they should display as "--verbose" not "--verbose <value>".
        /// The HelpFormatter uses ArgumentInfo.IsFlag to determine this.
        /// </summary>
        [TestMethod]
        public void Help_FlagArgument_DoesNotShowValuePlaceholder()
        {
            // Arrange
            var commandInfo = CommandReflection.Describe<ValidFlagCommand>();

            // Act - check if the flag argument is recognized as a flag
            var verboseArg = commandInfo.Arguments.First(a => a.Name == "Verbose");
            var forceArg = commandInfo.Arguments.First(a => a.Name == "Force");

            // Assert - Flag arguments should be recognized as flags (no value placeholder)
            // The HelpFormatter's IsFlag method checks ArgumentInfo.IsFlag
            verboseArg.IsFlag.Should().BeTrue("because Verbose has [Flag] attribute");
            forceArg.IsFlag.Should().BeTrue("because Force has [Flag] attribute");
        }

        /// <summary>
        /// HELP-002: Non-flag bool arguments SHOULD show <value> in help output.
        /// Non-flag bools require explicit true/false values.
        /// </summary>
        [TestMethod]
        public void Help_NonFlagBoolArgument_ShowsValuePlaceholder()
        {
            // Arrange
            var commandInfo = CommandReflection.Describe<MixedBoolCommand>();

            // Act - check both flag and non-flag bool arguments
            var flagArg = commandInfo.Arguments.First(a => a.Name == "IsFlag");
            var nonFlagArg = commandInfo.Arguments.First(a => a.Name == "RequiresValue");

            // Assert
            flagArg.IsFlag.Should().BeTrue("because IsFlag has [Flag] attribute");
            nonFlagArg.IsFlag.Should().BeFalse("because RequiresValue does NOT have [Flag] attribute");
        }

        #endregion
    }
}
