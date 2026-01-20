using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Processing.Resolution;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Integration tests for resolving group+command to CommandInfo.
    /// T022: Test resolving `group command` to CommandInfo
    /// T029: Verify dot-notation (`math.add`) is NOT recognized
    /// </summary>
    [TestClass]
    public class GroupResolutionTests
    {
        private ICommandRegistry _registry;
        private CommandResolver _resolver;

        [TestInitialize]
        public void TestInitialize()
        {
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterGroup(typeof(MathGroup));
            builder.RegisterCommand<AddCommand>();
            builder.RegisterCommand<SubtractCommand>();
            builder.RegisterCommand<VersionCommand>();

            _registry = builder.Build();
            _resolver = new CommandResolver(_registry);
        }

        [TestMethod]
        public void Resolve_GroupAndCommand_ResolvesToCommand()
        {
            // Arrange - space-separated syntax
            var input = new ParsedCommand("math add");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.Should().NotBeNull();
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Name.Should().Be("add");
            result.CommandInfo.Type.Should().Be<AddCommand>();
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Resolve_GroupAndCommand_CaseInsensitive()
        {
            // Arrange
            var input = new ParsedCommand("MATH ADD");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Name.Should().Be("add");
        }

        [TestMethod]
        public void Resolve_RootLevelCommand_ResolvesToCommand()
        {
            // Arrange - command without group
            var input = new ParsedCommand("version");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Name.Should().Be("version");
            result.CommandInfo.Group.Should().BeNull();
        }

        [TestMethod]
        public void Resolve_DotNotation_NotRecognized()
        {
            // Arrange - old dot notation should NOT work
            var input = new ParsedCommand("math.add");

            // Act
            var result = _resolver.Resolve(input);

            // Assert - should not find the command
            result.CommandInfo.Should().BeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Type == CommandResolutionErrorType.CommandNotFound);
        }

        [TestMethod]
        public void Resolve_GroupCommand_WithArguments()
        {
            // Arrange
            var input = new ParsedCommand("math add --num1 5 --num2 3");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Name.Should().Be("add");
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Resolve_UnknownGroup_NotFound()
        {
            // Arrange
            var input = new ParsedCommand("unknown add");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.CommandInfo.Should().BeNull();
            result.Errors.Should().Contain(e => e.Type == CommandResolutionErrorType.CommandNotFound);
        }

        [TestMethod]
        public void Resolve_CommandByNameOnly_WhenUnique()
        {
            // Arrange - just the command name, should work if unique
            var input = new ParsedCommand("subtract");

            // Act
            var result = _resolver.Resolve(input);

            // Assert - should find the command even without group prefix
            result.CommandInfo.Should().NotBeNull();
            result.CommandInfo.Name.Should().Be("subtract");
        }

        #region Case Sensitivity Tests (T061-T065)

        [TestMethod]
        public void CaseInsensitive_MixedCase_ResolvesSuccessfully()
        {
            // Arrange - default is case-insensitive
            var input = new ParsedCommand("Math Add");

            // Act
            var result = _resolver.Resolve(input);

            // Assert
            result.CommandInfo.Should().NotBeNull("case-insensitive should match");
            result.CommandInfo.Name.Should().Be("add");
        }

        [TestMethod]
        public void CaseSensitive_Enabled_ExactCaseRequired()
        {
            // Arrange - enable case sensitivity
            var builder = new CommandRegistryBuilder();
            builder.CaseSensitive = true;
            builder.RegisterGroup(typeof(MathGroup));
            builder.RegisterCommand<AddCommand>();
            var registry = builder.Build();
            var resolver = new CommandResolver(registry);

            var exactInput = new ParsedCommand("math add");
            var wrongCaseInput = new ParsedCommand("Math Add");

            // Act
            var exactResult = resolver.Resolve(exactInput);
            var wrongCaseResult = resolver.Resolve(wrongCaseInput);

            // Assert
            exactResult.CommandInfo.Should().NotBeNull("exact case should match");
            wrongCaseResult.CommandInfo.Should().BeNull("wrong case should not match when case-sensitive");
        }

        [TestMethod]
        public void CaseSensitive_FindGroup_RespectsSettings()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.CaseSensitive = true;
            builder.RegisterGroup(typeof(MathGroup));
            builder.RegisterCommand<DummyMathCommand>();

            var registry = builder.Build();

            // Act & Assert
            registry.FindGroup("math").Should().NotBeNull("exact case should match");
            registry.FindGroup("MATH").Should().BeNull("wrong case should not match when case-sensitive");
        }

        #endregion

        // Test helper classes
        [Group]
        public class MathGroup { }

        [Command(Group = typeof(MathGroup), Name = "add")]
        public class AddCommand : CommandBase
        {
            [Argument]
            public int Num1 { get; set; }

            [Argument]
            public int Num2 { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(MathGroup), Name = "subtract")]
        public class SubtractCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "version")]
        public class VersionCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(MathGroup), Name = "dummy")]
        public class DummyMathCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }
    }
}
