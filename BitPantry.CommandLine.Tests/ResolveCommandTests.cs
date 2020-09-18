using BitPantry.CommandLine.Processing;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.CommandLine.Tests.Commands.ResolveCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    [TestClass]
    public class ResolveCommandTests
    {
        private static CommandResolver _resolver;

        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var registry = new CommandRegistry();

            registry.RegisterCommand<Command>();
            registry.RegisterCommand<CommandWithNameAttribute>();
            registry.RegisterCommand<CommandWithArgument>();
            registry.RegisterCommand<CommandWithAlias>();
            registry.RegisterCommand<MultipleArgumentsAndAliases>();

            _resolver = new CommandResolver(registry);
        }

        [TestMethod]
        public void ResolveCommand_Resolved()
        {
            var input = new ParsedInput("Command");
            var result = _resolver.Resolve(input);

            result.Input.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Type.Should().Be<Command>();
        }

        [TestMethod]
        public void ResolveCommandCaseInvariant_Resolved()
        {
            var input = new ParsedInput("cOmMaNd");
            var result = _resolver.Resolve(input);

            result.Input.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Type.Should().Be<Command>();
        }

        [TestMethod]
        public void ResolveCommandWithNameAttribute_Resolved()
        {
            var input = new ParsedInput("myCommand");
            var result = _resolver.Resolve(input);

            result.Input.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            result.Errors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
            result.CommandInfo.Type.Should().Be<CommandWithNameAttribute>();
        }

        [TestMethod]
        public void ResolveNonExistentCommand_NotResolvedWithErrors()
        {
            var input = new ParsedInput("nonExistant");
            var result = _resolver.Resolve(input);

            result.Input.Should().NotBeNull();
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
            var input = new ParsedInput("command --doesntExist");
            var result = _resolver.Resolve(input);

            result.Input.Should().NotBeNull();
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
            var input = new ParsedInput("command --doesntExist --alsoDoesntExist");
            var result = _resolver.Resolve(input);

            result.Input.Should().NotBeNull();
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
            var input = new ParsedInput("commandWithArgument --ArgOne");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(0);
        }

        [TestMethod]
        public void ResolveCommandWithArgumentNameInvariantCase_Resolved()
        {
            var input = new ParsedInput("commandWithArgument --aRgOnE");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(0);
        }

        [TestMethod]
        public void ResolveCommandWithArgumentAlias_Resolved()
        {
            var input = new ParsedInput("commandWithAlias -p");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(0);
        }

        [TestMethod]
        public void ResolveCommandWithArgumentAliasWrongCase_ResolvedWithErrors()
        {
            var input = new ParsedInput("commandWithAlias -P");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(1);
            result.Errors.First().Type.Should().Be(CommandResolutionErrorType.ArgumentNotFound);
            result.Errors.First().Element.Raw.Should().Be("-P");
            result.Errors.First().Message.Should().NotBeNull();
        }

        [TestMethod]
        [ExpectedException(typeof(CommandResolutionException))]
        public void ResolveCommandWithLongArgumentAlias_Resolved()
        {
            var input = new ParsedInput("commandWithArgument -alias");
            var result = _resolver.Resolve(input);
        }

        [TestMethod]
        public void ResolveCommandWithMultipleArguments_Resolved()
        {
            var input = new ParsedInput("multipleArgumentsAndAliases --myProperty value1 -p \"value\" --Prop");
            var result = _resolver.Resolve(input);

            result.CommandInfo.Should().NotBeNull();
            result.Errors.Count.Should().Be(0);
        }


    }
}
