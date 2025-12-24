using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Processing.Resolution;
using BitPantry.CommandLine.Tests.Commands.ResolveCommands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests
{

    [TestClass]
    public class ResolveInputTests
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
            registry.RegisterCommand<CommandWithGroup>();
            registry.RegisterCommand<DupNameDifferentGroup>();
            registry.RegisterCommand<ReturnsString>();
            registry.RegisterCommand<AcceptsString>();

            _resolver = new CommandResolver(registry);
        }

        [TestMethod]
        public void Resolve_Resolved()
        {
            var input = new ParsedInput("Command");
            var result = _resolver.Resolve(input);

            result.ResolvedCommands.Should().HaveCount(1);
            result.CommandErrors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ResolveCommandNotExists_ResolvedWithError()
        {
            var input = new ParsedInput("CommandNotExists");
            var result = _resolver.Resolve(input);

            result.CommandErrors.Should().HaveCount(1);
            result.IsValid.Should().BeFalse();
        }

        [TestMethod]
        public void ResolveTwoCommandsNoPipe_ResolvedAsGroupCommand()
        {
            // With space-separated group syntax, "Command myCommand" is valid (myCommand in group Command)
            // This now resolves as a single command with group path
            var input = new ParsedInput("Command myCommand");
            
            // The input is now valid with the new space-separated syntax
            input.IsValid.Should().BeTrue();
            input.ParsedCommands.Should().HaveCount(1);
        }

        [TestMethod]
        public void ResolvePipelineInput_Resolved()
        {
            var input = new ParsedInput("Command | myCommand");
            var result = _resolver.Resolve(input);

            result.ResolvedCommands.Should().HaveCount(2);

            result.ResolvedCommands[0].CommandInfo.Name.Should().Be("Command");
            result.ResolvedCommands[1].CommandInfo.Name.Should().Be("myCommand");

            result.CommandErrors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ResolvePipelineInputWithData_Resolved()
        {
            var input = new ParsedInput("returnsString | acceptsString");
            var result = _resolver.Resolve(input);

            result.ResolvedCommands.Should().HaveCount(2);

            result.ResolvedCommands[0].CommandInfo.Name.Should().Be("ReturnsString");
            result.ResolvedCommands[1].CommandInfo.Name.Should().Be("AcceptsString");

            result.CommandErrors.Should().BeEmpty();
            result.DataPipelineErrors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }


        [TestMethod]
        public void ResolvePipelineInputVoidToAccepts_Resolved()
        {
            var input = new ParsedInput("Command | acceptsString");
            var result = _resolver.Resolve(input);

            result.ResolvedCommands.Should().HaveCount(2);

            result.ResolvedCommands[0].CommandInfo.Name.Should().Be("Command");
            result.ResolvedCommands[1].CommandInfo.Name.Should().Be("AcceptsString");

            result.CommandErrors.Should().BeEmpty();
            result.DataPipelineErrors.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }

    }
}
