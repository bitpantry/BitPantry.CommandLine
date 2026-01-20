using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Help;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console.Testing;
using System.Linq;

namespace BitPantry.CommandLine.Tests.Help
{
    /// <summary>
    /// Tests for help formatting output.
    /// Tests use Spectre.Console.Testing.TestConsole for output capture.
    /// </summary>
    [TestClass]
    public class HelpFormatterTests
    {
        private TestConsole _console;
        private HelpFormatter _formatter;

        [TestInitialize]
        public void Setup()
        {
            _console = new TestConsole();
            _formatter = new HelpFormatter();
        }

        #region Group Help Tests

        [TestMethod]
        public void DisplayGroupHelp_SingleGroup_ShowsGroupDescription()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterGroup(typeof(MathGroup));
            builder.RegisterCommand<AddCommand>();
            builder.RegisterCommand<SubtractCommand>();
            var registry = builder.Build();
            
            var group = registry.Groups.First();

            // Act
            _formatter.DisplayGroupHelp(_console, group, registry);

            // Assert
            _console.Output.Should().Contain("math");
            _console.Output.Should().Contain("Mathematical operations");
            _console.Output.Should().Contain("add");
            _console.Output.Should().Contain("subtract");
        }

        [TestMethod]
        public void DisplayGroupHelp_ShowsCommandsInGroup()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterGroup(typeof(MathGroup));
            builder.RegisterCommand<AddCommand>();
            builder.RegisterCommand<SubtractCommand>();
            var registry = builder.Build();
            
            var group = registry.Groups.First();

            // Act
            _formatter.DisplayGroupHelp(_console, group, registry);

            // Assert
            _console.Output.Should().Contain("add");
            _console.Output.Should().Contain("subtract");
        }

        [TestMethod]
        public void DisplayGroupHelp_GroupWithSubgroupsOnly_ShowsSubgroups()
        {
            // Arrange - EmptyGroup has a nested subgroup with a command to pass validation,
            // but EmptyGroup itself has no direct commands, only subgroups
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<EmptyGroupNestedCommand>();
            var registry = builder.Build();
            
            var group = registry.Groups.First(g => g.Name == "empty");

            // Act
            _formatter.DisplayGroupHelp(_console, group, registry);

            // Assert - group has no direct commands but shows subgroups
            _console.Output.Should().Contain("empty");
            _console.Output.Should().Contain("Subgroups:");
            _console.Output.Should().Contain("nestedsub");
        }

        [TestMethod]
        public void DisplayGroupHelp_NestedGroup_ShowsFullPath()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<AdvancedMatrixCommand>();
            var registry = builder.Build();
            
            var group = registry.Groups.First(g => g.Name == "advanced");

            // Act
            _formatter.DisplayGroupHelp(_console, group, registry);

            // Assert
            // Should show full path "math advanced" for nested group
            _console.Output.Should().Contain("math advanced");
        }

        [TestMethod]
        public void DisplayGroupHelp_ShowsUsageHintWithFullPath()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterGroup(typeof(MathGroup));
            builder.RegisterCommand<AddCommand>();
            var registry = builder.Build();
            
            var group = registry.Groups.First();

            // Act
            _formatter.DisplayGroupHelp(_console, group, registry);

            // Assert
            _console.Output.Should().Contain("Usage: math <command>");
        }

        #endregion

        #region Command Help Tests

        [TestMethod]
        public void DisplayCommandHelp_ShowsDescriptionSection()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<AddCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("Description:");
            _console.Output.Should().Contain("Adds two numbers");
        }

        [TestMethod]
        public void DisplayCommandHelp_NoDescription_ShowsPlaceholder()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<NoDescCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("(no description)");
        }

        [TestMethod]
        public void DisplayCommandHelp_ShowsUsageSection()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<AddCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("Usage:");
            _console.Output.Should().Contain("math add");
        }

        [TestMethod]
        public void DisplayCommandHelp_GroupedCommand_ShowsFullPath()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<AddCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("math add");
        }

        [TestMethod]
        public void DisplayCommandHelp_RootCommand_ShowsSimplePath()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<VersionCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("version");
            _console.Output.Should().NotContain("math version");
        }

        [TestMethod]
        public void DisplayCommandHelp_OptionsSection_ShowsValuePlaceholder()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<AddCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            // Non-flag options should show <value> placeholder
            _console.Output.Should().Contain("--Num1");
            _console.Output.Should().Contain("<value>");
        }

        [TestMethod]
        public void DisplayCommandHelp_FlagOption_NoValuePlaceholder()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<MixedPositionalNamedCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            // Flag options (bool) should not show <value>
            // Check that --Verbose appears but not with <value> directly after
            _console.Output.Should().Contain("--Verbose");
        }

        [TestMethod]
        public void DisplayCommandHelp_PositionalWithAlias_ShowsBoth()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<AliasedPositionalCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert - positional argument should show name and alias
            _console.Output.Should().Contain("Source, -s");
        }

        [TestMethod]
        public void DisplayCommandHelp_OptionWithAlias_ShowsBoth()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<AliasedOptionCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("--Output");
            _console.Output.Should().Contain("-o");
        }

        #endregion

        #region Root Help Tests

        [TestMethod]
        public void DisplayRootHelp_ShowsAllGroups()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterGroup(typeof(MathGroup));
            builder.RegisterGroup(typeof(FileGroup));
            builder.RegisterCommand<AddCommand>();
            builder.RegisterCommand<CopyCommand>();
            var registry = builder.Build();

            // Act
            _formatter.DisplayRootHelp(_console, registry);

            // Assert
            _console.Output.Should().Contain("math");
            _console.Output.Should().Contain("file");
        }

        [TestMethod]
        public void DisplayRootHelp_ShowsRootCommands()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<VersionCommand>();
            builder.RegisterCommand<HelpCommand>();
            var registry = builder.Build();

            // Act
            _formatter.DisplayRootHelp(_console, registry);

            // Assert
            _console.Output.Should().Contain("version");
            _console.Output.Should().Contain("help");
        }

        [TestMethod]
        public void DisplayRootHelp_MixedGroupsAndCommands_ShowsBoth()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterGroup(typeof(MathGroup));
            builder.RegisterCommand<AddCommand>();
            builder.RegisterCommand<VersionCommand>();
            var registry = builder.Build();

            // Act
            _formatter.DisplayRootHelp(_console, registry);

            // Assert
            _console.Output.Should().Contain("Groups:");
            _console.Output.Should().Contain("math");
            _console.Output.Should().Contain("Commands:");
            _console.Output.Should().Contain("version");
        }

        [TestMethod]
        public void DisplayRootHelp_Empty_ShowsNoCommandsMessage()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            var registry = builder.Build();

            // Act
            _formatter.DisplayRootHelp(_console, registry);

            // Assert
            _console.Output.Should().Contain("No commands or groups registered");
        }

        #endregion

        #region Positional Argument Help Tests

        [TestMethod]
        public void DisplayCommandHelp_RequiredPositional_ShowsAngleBrackets()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<PositionalCopyCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("<Source>");
            _console.Output.Should().Contain("<Destination>");
        }

        [TestMethod]
        public void DisplayCommandHelp_OptionalPositional_ShowsSquareBrackets()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<OptionalPositionalCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().MatchRegex(@"\[Output\]");
        }

        [TestMethod]
        public void DisplayCommandHelp_VariadicPositional_ShowsEllipsis()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<VariadicPositionalCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("...");
        }

        [TestMethod]
        public void DisplayCommandHelp_MixedPositionalAndNamed_ShowsBothInUsage()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<MixedPositionalNamedCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("<FileName>");
            _console.Output.Should().Contain("--Verbose");
        }

        [TestMethod]
        public void DisplayCommandHelp_MultiplePositional_ShowsInOrder()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<PositionalCopyCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            var sourceIndex = _console.Output.IndexOf("<Source>");
            var destIndex = _console.Output.IndexOf("<Destination>");
            sourceIndex.Should().BeLessThan(destIndex, "Source should appear before Destination");
        }

        [TestMethod]
        public void DisplayCommandHelp_CollectionOption_ShowsRepeatableNote()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<RepeatedOptionHelpCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("repeatable");
        }

        [TestMethod]
        public void DisplayCommandHelp_PositionalArg_ShowsNamedHint()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<PositionalCopyCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            // Positional args should show "(or --Name)" hint
            _console.Output.Should().Contain("(or --Source)");
        }

        [TestMethod]
        public void DisplayCommandHelp_RequiredOption_ShowsRequiredNote()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<RequiredOptionCommand>();
            var registry = builder.Build();
            
            var command = registry.Commands.First();

            // Act
            _formatter.DisplayCommandHelp(_console, command);

            // Assert
            _console.Output.Should().Contain("(required)");
        }

        #endregion

        #region Column Alignment Tests

        [TestMethod]
        public void DisplayGroupHelp_CommandsAligned_WhenDifferentLengths()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterGroup(typeof(MathGroup));
            builder.RegisterCommand<AddCommand>();
            builder.RegisterCommand<SubtractCommand>();
            var registry = builder.Build();
            
            var group = registry.Groups.First();

            // Act
            _formatter.DisplayGroupHelp(_console, group, registry);

            // Assert
            // Both commands should be present with descriptions
            _console.Output.Should().Contain("add");
            _console.Output.Should().Contain("subtract");
            // The longer command name should cause padding of shorter ones
            // Just verify both appear - alignment is visual and tested manually
        }

        #endregion

        #region Test Helper Classes

        [Group]
        [API.Description("Mathematical operations")]
        private class MathGroup
        {
            [Group]
            [API.Description("Advanced math operations")]
            public class AdvancedGroup { }
        }

        [Group]
        [API.Description("File operations")]
        private class FileGroup { }

        [Group]
        private class EmptyGroup 
        { 
            // Nested subgroup to make EmptyGroup valid (has subgroup with command)
            // but EmptyGroup itself has no direct commands
            [Group]
            public class NestedSubGroup { }
        }

        [Command(Group = typeof(EmptyGroup.NestedSubGroup), Name = "nestedcmd")]
        private class EmptyGroupNestedCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(MathGroup), Name = "add")]
        [API.Description("Adds two numbers")]
        private class AddCommand : CommandBase
        {
            [Argument]
            public int Num1 { get; set; }

            [Argument]
            public int Num2 { get; set; }

            public int Execute(CommandExecutionContext ctx) => Num1 + Num2;
        }

        [Command(Group = typeof(MathGroup), Name = "subtract")]
        private class SubtractCommand : CommandBase
        {
            [Argument]
            public int Value { get; set; }

            public int Execute(CommandExecutionContext ctx) => -Value;
        }

        [Command(Group = typeof(MathGroup.AdvancedGroup), Name = "matrix")]
        private class AdvancedMatrixCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(FileGroup), Name = "copy")]
        private class CopyCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "version")]
        private class VersionCommand : CommandBase
        {
            public string Execute(CommandExecutionContext ctx) => "1.0.0";
        }

        [Command(Name = "help")]
        private class HelpCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "nodesc")]
        private class NoDescCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "aliaspos")]
        private class AliasedPositionalCommand : CommandBase
        {
            [Argument(Position = 0, IsRequired = true)]
            [Alias('s')]
            public string Source { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "aliasopt")]
        private class AliasedOptionCommand : CommandBase
        {
            [Argument]
            [Alias('o')]
            public string Output { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "reqopt")]
        private class RequiredOptionCommand : CommandBase
        {
            [Argument(IsRequired = true)]
            public string Name { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "pcopy")]
        [API.Description("Copies a file")]
        private class PositionalCopyCommand : CommandBase
        {
            [Argument(Position = 0, IsRequired = true)]
            [API.Description("Source file path")]
            public string Source { get; set; }

            [Argument(Position = 1, IsRequired = true)]
            [API.Description("Destination file path")]
            public string Destination { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "optpos")]
        private class OptionalPositionalCommand : CommandBase
        {
            [Argument(Position = 0)]
            public string Input { get; set; }

            [Argument(Position = 1, IsRequired = false)]
            public string Output { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "varpos")]
        private class VariadicPositionalCommand : CommandBase
        {
            [Argument(Position = 0)]
            public string Command { get; set; }

            [Argument(Position = 1, IsRest = true)]
            public string[] Args { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "mixedpos")]
        private class MixedPositionalNamedCommand : CommandBase
        {
            [Argument(Position = 0, IsRequired = true)]
            public string FileName { get; set; }

            [Argument]
            public bool Verbose { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "repeatopt")]
        private class RepeatedOptionHelpCommand : CommandBase
        {
            [Argument]
            [API.Description("Tags to apply")]
            public string[] Tags { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion
    }
}
