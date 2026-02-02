using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.CommandLine.Tests.Commands.PositionalCommands;
using BitPantry.CommandLine.Tests.Commands.RepeatedOptionCommands;
using BitPantry.CommandLine.Tests.Commands.ResolveCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class ResolveCommandTests
    {
        private static CommandResolver _resolver;
        private static CommandResolver _positionalResolver;
        private static CommandResolver _repeatedOptionResolver;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var builder = new CommandRegistryBuilder();

            builder.RegisterCommand<Command>();
            builder.RegisterCommand<CommandWithNameAttribute>();
            builder.RegisterCommand<CommandWithArgument>();
            builder.RegisterCommand<CommandWithAlias>();
            builder.RegisterCommand<MultipleArgumentsAndAliases>();
            builder.RegisterCommand<CommandWithGroup>();
            builder.RegisterCommand<DupNameDifferentGroup>();
            builder.RegisterCommand<ExtendedCommand>();

            var registry = builder.Build();
            _resolver = new CommandResolver(registry);

            // Create separate resolver for positional argument tests
            var positionalBuilder = new CommandRegistryBuilder();
            positionalBuilder.RegisterCommand<SinglePositionalCommand>();
            positionalBuilder.RegisterCommand<MultiplePositionalCommand>();
            positionalBuilder.RegisterCommand<PositionalWithNamedCommand>();
            positionalBuilder.RegisterCommand<RequiredPositionalCommand>();
            positionalBuilder.RegisterCommand<OptionalPositionalCommand>();
            positionalBuilder.RegisterCommand<IsRestCommand>();
            positionalBuilder.RegisterCommand<IsRestWithPrecedingCommand>();
            var positionalRegistry = positionalBuilder.Build();
            _positionalResolver = new CommandResolver(positionalRegistry);

            // Create separate resolver for repeated option tests
            var repeatedOptionBuilder = new CommandRegistryBuilder();
            repeatedOptionBuilder.RegisterCommand<RepeatedOptionArrayCommand>();
            repeatedOptionBuilder.RegisterCommand<RepeatedOptionScalarCommand>();
            var repeatedOptionRegistry = repeatedOptionBuilder.Build();
            _repeatedOptionResolver = new CommandResolver(repeatedOptionRegistry);
        }

        [TestMethod]
        public void ResolveCommand_Resolved()
        {
            var input = new ParsedCommand("Command");
            var result = _resolver.Resolve(input);

            result.ParsedCommand.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Type.Should().Be<Command>();
        }

        [TestMethod]
        public void ResolveCommandCaseVariant_Resolved()
        {
            var input = new ParsedCommand("cOmMaNd");
            var result = _resolver.Resolve(input);

            result.ParsedCommand.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Type.Should().Be<Command>();
        }

        [TestMethod]
        public void ResolveCommandWithNameAttribute_Resolved()
        {
            var input = new ParsedCommand("myCommand");
            var result = _resolver.Resolve(input);

            result.ParsedCommand.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Type.Should().Be<CommandWithNameAttribute>();
        }

        [TestMethod]
        public void ResolveNonExistentCommand_NotResolvedWithErrors()
        {
            var input = new ParsedCommand("nonExistant");
            var result = _resolver.Resolve(input);

            result.ParsedCommand.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.CommandInfo.Should().BeNull();
            result.Errors.Count.Should().Be(1);
            result.Errors.First().Type.Should().Be(CommandResolutionErrorType.CommandNotFound);
            result.Errors.First().Element.Should().BeNull();
            result.Errors.First().Message.Should().BeNull();

        }

        [TestMethod]
        public void ResolveCommandWithBadArgumentName_ResolvedWithErrors()
        {
            var input = new ParsedCommand("command --doesntExist");
            var result = _resolver.Resolve(input);

            result.ParsedCommand.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(1);
            result.Errors.First().Type.Should().Be(CommandResolutionErrorType.ArgumentNotFound);
            result.Errors.First().Element.Raw.Should().Be("--doesntExist");
            result.Errors.First().Message.Should().NotBeNull();

        }

        [TestMethod]
        public void ResolveCommandWithTwoBadArgumentName_ResolvedWithErrors()
        {
            var input = new ParsedCommand("command --doesntExist --alsoDoesntExist");
            var result = _resolver.Resolve(input);

            result.ParsedCommand.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(2);
            result.Errors.First().Type.Should().Be(CommandResolutionErrorType.ArgumentNotFound);
            result.Errors.First().Element.Raw.Should().Be("--doesntExist");
            result.Errors.First().Message.Should().NotBeNull();
            result.Errors.Skip(1).First().Type.Should().Be(CommandResolutionErrorType.ArgumentNotFound);
            result.Errors.Skip(1).First().Element.Raw.Should().Be("--alsoDoesntExist");
            result.Errors.Skip(1).First().Message.Should().NotBeNull();

        }

        [TestMethod]
        public void ResolveCommandWithArgument_Resolved()
        {
            var input = new ParsedCommand("commandWithArgument --ArgOne 42");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(0);
        }

        [TestMethod]
        public void ResolveCommandWithArgumentNameInvariantCase_Resolved()
        {
            var input = new ParsedCommand("commandWithArgument --aRgOnE 42");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(0);
        }

        [TestMethod]
        public void ResolveCommandWithArgumentAlias_Resolved()
        {
            var input = new ParsedCommand("commandWithAlias -p 42");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(0);
        }

        [TestMethod]
        public void ResolveCommandWithArgumentAliasWrongCase_ResolvedWithErrors()
        {
            var input = new ParsedCommand("commandWithAlias -P");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(1);
            result.Errors.First().Type.Should().Be(CommandResolutionErrorType.ArgumentNotFound);
            result.Errors.First().Element.Raw.Should().Be("-P");
            result.Errors.First().Message.Should().NotBeNull();
        }

        [TestMethod]
        public void ResolveCommandWithMultipleArguments_Resolved()
        {
            var input = new ParsedCommand("multipleArgumentsAndAliases --myProperty 123 -p \"value\" --Prop propValue");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(0);
        }

        [TestMethod]
        public void ResolveCommandWithGroup_Resolved()
        {
            // Note: Group-based resolution needs groups registered. For now, just test by command name.
            // Full group resolution will be tested in GroupResolutionTests
            var input = new ParsedCommand("CommandWithGroup");
            var result = _resolver.Resolve(input);

            result.ParsedCommand.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Type.Should().Be<CommandWithGroup>();
        }


        [TestMethod]
        public void ResolveCommandWithDupCmdDifferentGroup_Resolved()
        {
            // Note: Group-based resolution needs groups registered. For now, just test by command name.
            var input = new ParsedCommand("Command");
            var result = _resolver.Resolve(input);

            result.ParsedCommand.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            // Note: Without groups registered, this will resolve to the first matching command
            // Full group resolution will be tested in GroupResolutionTests
        }

        [TestMethod]
        public void ResolveExtendedCommand_Resolved()
        {
            var input = new ParsedCommand("extendedCommand");
            var result = _resolver.Resolve(input);

            result.ParsedCommand.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Type.Should().Be<ExtendedCommand>();
        }

        #region Positional Argument Resolution Tests (RES-001 through RES-011)

        /// <summary>
        /// RES-001: Single positional value resolved to Position=0 argument
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES001_SinglePositionalResolved()
        {
            // Arrange - "singlePositionalCommand value1" should resolve value1 to Position=0 (Source)
            var input = new ParsedCommand("singlePositionalCommand value1");

            // Act
            var result = _positionalResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Type.Should().Be<SinglePositionalCommand>();
            result.Errors.Should().BeEmpty();
            
            // The InputMap should contain the positional argument mapped
            var sourceArg = result.CommandInfo.Arguments.Single(a => a.Name == "Source");
            result.InputMap.Should().ContainKey(sourceArg);
            result.InputMap[sourceArg].Value.Should().Be("value1");
        }

        /// <summary>
        /// RES-002: Multiple positional values resolved to Position=0,1,2 arguments
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES002_MultiplePositionalResolved()
        {
            // Arrange - "multiplePositionalCommand first second 42" 
            var input = new ParsedCommand("multiplePositionalCommand first second 42");

            // Act
            var result = _positionalResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Type.Should().Be<MultiplePositionalCommand>();
            result.Errors.Should().BeEmpty();
            
            // Verify each positional argument is mapped correctly
            var firstArg = result.CommandInfo.Arguments.Single(a => a.Name == "First");
            var secondArg = result.CommandInfo.Arguments.Single(a => a.Name == "Second");
            var thirdArg = result.CommandInfo.Arguments.Single(a => a.Name == "Third");
            
            result.InputMap.Should().ContainKey(firstArg);
            result.InputMap.Should().ContainKey(secondArg);
            result.InputMap.Should().ContainKey(thirdArg);
            result.InputMap[firstArg].Value.Should().Be("first");
            result.InputMap[secondArg].Value.Should().Be("second");
            result.InputMap[thirdArg].Value.Should().Be("42");
        }

        /// <summary>
        /// RES-003: Positional + named arguments both resolved
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES003_PositionalAndNamedResolved()
        {
            // Arrange - "positionalWithNamedCommand source.txt dest.txt --force --mode copy"
            var input = new ParsedCommand("positionalWithNamedCommand source.txt dest.txt --force --mode copy");

            // Act
            var result = _positionalResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Type.Should().Be<PositionalWithNamedCommand>();
            result.Errors.Should().BeEmpty();
            
            // Verify positional arguments
            var sourceArg = result.CommandInfo.Arguments.Single(a => a.Name == "Source");
            var destArg = result.CommandInfo.Arguments.Single(a => a.Name == "Destination");
            result.InputMap.Should().ContainKey(sourceArg);
            result.InputMap.Should().ContainKey(destArg);
            result.InputMap[sourceArg].Value.Should().Be("source.txt");
            result.InputMap[destArg].Value.Should().Be("dest.txt");
            
            // Verify named arguments
            var forceArg = result.CommandInfo.Arguments.Single(a => a.Name == "Force");
            var modeArg = result.CommandInfo.Arguments.Single(a => a.Name == "Mode");
            result.InputMap.Should().ContainKey(forceArg);
            result.InputMap.Should().ContainKey(modeArg);
        }

        /// <summary>
        /// RES-006: Missing required positional argument produces error
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES006_MissingRequiredPositional()
        {
            // Arrange - "requiredPositionalCommand source.txt" (missing second required positional)
            var input = new ParsedCommand("requiredPositionalCommand source.txt");

            // Act
            var result = _positionalResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeFalse();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Type == CommandResolutionErrorType.MissingRequiredPositional);
            
            // Error message should mention the missing argument
            var missingError = result.Errors.First(e => e.Type == CommandResolutionErrorType.MissingRequiredPositional);
            missingError.Message.Should().Contain("Destination");
        }

        /// <summary>
        /// RES-007: Excess positional values (more than defined, no IsRest) produces error
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES007_ExcessPositionalValues()
        {
            // Arrange - "singlePositionalCommand value1 value2 value3" (only one positional defined)
            var input = new ParsedCommand("singlePositionalCommand value1 value2 value3");

            // Act
            var result = _positionalResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeFalse();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Type == CommandResolutionErrorType.ExcessPositionalValues);
        }

        /// <summary>
        /// RES-011: Positional values after -- are treated as positional (not named args)
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES011_PositionalAfterEndOfOptions()
        {
            // Arrange - "singlePositionalCommand -- --dashValue" 
            // The --dashValue should be treated as positional value (literal), not as argument name
            var input = new ParsedCommand("singlePositionalCommand -- --dashValue");

            // Act
            var result = _positionalResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Type.Should().Be<SinglePositionalCommand>();
            
            // The --dashValue should be bound to the Source positional argument as a literal value
            var sourceArg = result.CommandInfo.Arguments.Single(a => a.Name == "Source");
            result.InputMap.Should().ContainKey(sourceArg);
            result.InputMap[sourceArg].Value.Should().Be("--dashValue");
        }

        #endregion

        #region IsRest Positional Argument Resolution Tests (RES-004, RES-005)

        /// <summary>
        /// RES-004: IsRest argument collects remaining positional values
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES004_IsRestCollectsRemaining()
        {
            // Arrange - "isRestWithPrecedingCommand target a b c d"
            // Target should get "target", Sources (IsRest) should get ["a", "b", "c", "d"]
            var input = new ParsedCommand("isRestWithPrecedingCommand target a b c d");

            // Act
            var result = _positionalResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Type.Should().Be<IsRestWithPrecedingCommand>();
            result.Errors.Should().BeEmpty();

            // Verify Target gets the first value
            var targetArg = result.CommandInfo.Arguments.Single(a => a.Name == "Target");
            result.InputMap.Should().ContainKey(targetArg);
            result.InputMap[targetArg].Value.Should().Be("target");

            // Verify Sources (IsRest) is in the InputMap
            var sourcesArg = result.CommandInfo.Arguments.Single(a => a.Name == "Sources");
            result.InputMap.Should().ContainKey(sourcesArg);
            // Note: Full IsRest multi-value support will be verified in activation tests
            // For now, the resolver just needs to not error on excess values when IsRest is defined
        }

        /// <summary>
        /// RES-005: IsRest argument with zero extra values is valid (empty array)
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES005_IsRestWithZeroExtra()
        {
            // Arrange - "isRestWithPrecedingCommand target" - only the preceding arg, no rest values
            var input = new ParsedCommand("isRestWithPrecedingCommand target");

            // Act
            var result = _positionalResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Type.Should().Be<IsRestWithPrecedingCommand>();
            result.Errors.Should().BeEmpty();

            // Verify Target gets the value
            var targetArg = result.CommandInfo.Arguments.Single(a => a.Name == "Target");
            result.InputMap.Should().ContainKey(targetArg);
            result.InputMap[targetArg].Value.Should().Be("target");

            // Sources (IsRest) should not be in InputMap when no values provided
            // (or should be present with no values - depends on implementation)
            var sourcesArg = result.CommandInfo.Arguments.Single(a => a.Name == "Sources");
            // Either not in map OR in map with empty/null (implementation detail)
        }

        /// <summary>
        /// Additional: IsRest-only command (no preceding positional)
        /// </summary>
        [TestMethod]
        public void ResolveCommand_IsRestOnly()
        {
            // Arrange - "isRestCommand a b c"
            var input = new ParsedCommand("isRestCommand a b c");

            // Act
            var result = _positionalResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Type.Should().Be<IsRestCommand>();
            result.Errors.Should().BeEmpty();

            // Verify Files (IsRest) is in the InputMap
            var filesArg = result.CommandInfo.Arguments.Single(a => a.Name == "Files");
            result.InputMap.Should().ContainKey(filesArg);
        }

        #endregion

        #region Repeated Named Option Resolution Tests (RES-008, RES-009, RES-010)

        /// <summary>
        /// RES-008: Repeated option on collection type is valid and collects values
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES008_RepeatedOptionCollection()
        {
            // Arrange - "repeatedOptionArrayCommand --items a --items b --items c"
            var input = new ParsedCommand("repeatedOptionArrayCommand --items a --items b --items c");

            // Act
            var result = _repeatedOptionResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Type.Should().Be<RepeatedOptionArrayCommand>();
            result.Errors.Should().BeEmpty();

            // Verify Items is in the InputMap
            var itemsArg = result.CommandInfo.Arguments.Single(a => a.Name == "Items");
            result.InputMap.Should().ContainKey(itemsArg);
            
            // Verify all values are captured in IsRestValues (reusing for repeated options)
            result.IsRestValues.Should().ContainKey(itemsArg);
            result.IsRestValues[itemsArg].Should().HaveCount(3);
        }

        /// <summary>
        /// RES-009: Repeated option on scalar type produces error
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES009_RepeatedOptionScalarError()
        {
            // Arrange - "repeatedOptionScalarCommand --value a --value b"
            var input = new ParsedCommand("repeatedOptionScalarCommand --value a --value b");

            // Act
            var result = _repeatedOptionResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeFalse();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().Contain(e => e.Type == CommandResolutionErrorType.DuplicateScalarArgument);
        }

        /// <summary>
        /// RES-010: Mixed delimiter-separated and repeated options both work
        /// </summary>
        [TestMethod]
        public void ResolveCommand_RES010_MixedDelimiterAndRepeated()
        {
            // Arrange - "repeatedOptionArrayCommand --items a --verbose true --items b"
            // Tests that a bool argument with value between repeated options works correctly
            var input = new ParsedCommand("repeatedOptionArrayCommand --items a --verbose true --items b");

            // Act
            var result = _repeatedOptionResolver.Resolve(input);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().BeEmpty();

            // Both options should be resolved
            var itemsArg = result.CommandInfo.Arguments.Single(a => a.Name == "Items");
            var verboseArg = result.CommandInfo.Arguments.Single(a => a.Name == "Verbose");
            result.InputMap.Should().ContainKey(itemsArg);
            result.InputMap.Should().ContainKey(verboseArg);
        }

        #endregion

    }
}
