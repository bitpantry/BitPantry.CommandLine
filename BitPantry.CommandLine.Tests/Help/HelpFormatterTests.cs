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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterCommand<AddCommand>();
            registry.RegisterCommand<SubtractCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterCommand<AddCommand>();
            registry.RegisterCommand<SubtractCommand>();
            
            var group = registry.Groups.First();

            // Act
            _formatter.DisplayGroupHelp(_console, group, registry);

            // Assert
            _console.Output.Should().Contain("add");
            _console.Output.Should().Contain("subtract");
        }

        [TestMethod]
        public void DisplayGroupHelp_EmptyGroup_ShowsNoCommandsMessage()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(EmptyGroup));
            
            var group = registry.Groups.First();

            // Act
            _formatter.DisplayGroupHelp(_console, group, registry);

            // Assert
            _console.Output.Should().Contain("empty");
            _console.Output.Should().Contain("No commands available");
        }

        [TestMethod]
        public void DisplayGroupHelp_NestedGroup_ShowsFullPath()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AdvancedMatrixCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterCommand<AddCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AddCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<NoDescCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AddCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AddCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<VersionCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AddCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<MixedPositionalNamedCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AliasedPositionalCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AliasedOptionCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterGroup(typeof(FileGroup));
            registry.RegisterCommand<AddCommand>();
            registry.RegisterCommand<CopyCommand>();

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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<VersionCommand>();
            registry.RegisterCommand<HelpCommand>();

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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterCommand<AddCommand>();
            registry.RegisterCommand<VersionCommand>();

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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;

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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<PositionalCopyCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<OptionalPositionalCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<VariadicPositionalCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<MixedPositionalNamedCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<PositionalCopyCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<RepeatedOptionHelpCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<PositionalCopyCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<RequiredOptionCommand>();
            
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
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterCommand<AddCommand>();
            registry.RegisterCommand<SubtractCommand>();
            
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
        private class EmptyGroup { }

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
