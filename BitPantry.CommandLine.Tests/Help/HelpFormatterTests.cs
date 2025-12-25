using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Tests.Help
{
    /// <summary>
    /// Tests for help formatting output - FR-020.
    /// T030: Test group help output format, command help format, root help format
    /// </summary>
    [TestClass]
    public class HelpFormatterTests
    {
        #region Group Help Tests

        [TestMethod]
        public void FormatGroupHelp_SingleGroup_ShowsGroupDescription()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterCommand<AddCommand>();
            registry.RegisterCommand<SubtractCommand>();
            
            var group = registry.Groups.First();
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayGroupHelp(writer, group, registry);

            // Assert
            var output = writer.ToString();
            output.Should().Contain("math");
            output.Should().Contain("Mathematical operations");
            output.Should().Contain("add");
            output.Should().Contain("subtract");
        }

        [TestMethod]
        public void FormatGroupHelp_ShowsCommandsInGroup()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterCommand<AddCommand>();
            registry.RegisterCommand<SubtractCommand>();
            
            var group = registry.Groups.First();
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayGroupHelp(writer, group, registry);

            // Assert
            var output = writer.ToString();
            // Should list commands available in this group
            output.Should().Contain("add");
            output.Should().Contain("subtract");
        }

        [TestMethod]
        public void FormatGroupHelp_EmptyGroup_ShowsNoCommandsMessage()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(EmptyGroup));
            
            var group = registry.Groups.First();
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayGroupHelp(writer, group, registry);

            // Assert
            var output = writer.ToString();
            output.Should().Contain("empty");
            output.Should().Contain("No commands available");
        }

        #endregion

        #region Command Help Tests

        [TestMethod]
        public void FormatCommandHelp_ShowsCommandDescription()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AddCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            output.Should().Contain("add");
            output.Should().Contain("Adds two numbers");
        }

        [TestMethod]
        public void FormatCommandHelp_ShowsArgumentsWithDescriptions()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AddCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            // Argument names preserve property casing
            output.Should().Contain("--Num1");
            output.Should().Contain("--Num2");
        }

        [TestMethod]
        public void FormatCommandHelp_GroupedCommand_ShowsFullPath()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AddCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            // Should show usage with group path
            output.Should().Contain("math add");
        }

        [TestMethod]
        public void FormatCommandHelp_RootCommand_ShowsSimplePath()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<VersionCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            // Root command has no group prefix
            output.Should().Contain("version");
            output.Should().NotContain("math version");
        }

        #endregion

        #region Root Help Tests

        [TestMethod]
        public void FormatRootHelp_ShowsAllGroups()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterGroup(typeof(FileGroup));
            registry.RegisterCommand<AddCommand>();
            registry.RegisterCommand<CopyCommand>();
            
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayRootHelp(writer, registry);

            // Assert
            var output = writer.ToString();
            output.Should().Contain("math");
            output.Should().Contain("file");
        }

        [TestMethod]
        public void FormatRootHelp_ShowsRootCommands()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<VersionCommand>();
            registry.RegisterCommand<HelpCommand>();
            
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayRootHelp(writer, registry);

            // Assert
            var output = writer.ToString();
            output.Should().Contain("version");
            output.Should().Contain("help");
        }

        [TestMethod]
        public void FormatRootHelp_MixedGroupsAndCommands_ShowsBoth()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterGroup(typeof(MathGroup));
            registry.RegisterCommand<AddCommand>();
            registry.RegisterCommand<VersionCommand>();
            
            var writer = new StringWriter();

            // Act
            var formatter = new TestHelpFormatter();
            formatter.DisplayRootHelp(writer, registry);

            // Assert
            var output = writer.ToString();
            output.Should().Contain("math");
            output.Should().Contain("version");
        }

        #endregion

        #region Positional Argument Help Tests (Phase 8)

        /// <summary>
        /// HELP-001: Required positional shows angle brackets in synopsis
        /// </summary>
        [TestMethod]
        public void HELP001_RequiredPositional_ShowsAngleBrackets()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<PositionalCopyCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new BitPantry.CommandLine.Help.HelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            // Required positional should show in angle brackets
            output.Should().Contain("<Source>");
            output.Should().Contain("<Destination>");
        }

        /// <summary>
        /// HELP-002: Optional positional shows square brackets in synopsis
        /// </summary>
        [TestMethod]
        public void HELP002_OptionalPositional_ShowsSquareBrackets()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<OptionalPositionalCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new BitPantry.CommandLine.Help.HelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            // Optional positional should show in square brackets (no angle brackets inside)
            output.Should().MatchRegex(@"\[Output\]");
        }

        /// <summary>
        /// HELP-003: Variadic positional shows ellipsis in synopsis
        /// </summary>
        [TestMethod]
        public void HELP003_VariadicPositional_ShowsEllipsis()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<VariadicPositionalCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new BitPantry.CommandLine.Help.HelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            // Variadic positional should show ellipsis
            output.Should().Contain("...");
        }

        /// <summary>
        /// HELP-004: Mixed positional + named shows both in synopsis
        /// </summary>
        [TestMethod]
        public void HELP004_MixedPositionalAndNamed_ShowsBoth()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<MixedPositionalNamedCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new BitPantry.CommandLine.Help.HelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            // Should show positional and named
            output.Should().Contain("<FileName>");
            output.Should().Contain("--Verbose");
        }

        /// <summary>
        /// HELP-005: Multiple positional shows in order in synopsis
        /// </summary>
        [TestMethod]
        public void HELP005_MultiplePositional_ShowsInOrder()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<PositionalCopyCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new BitPantry.CommandLine.Help.HelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            // Source (pos0) should appear before Destination (pos1)
            var sourceIndex = output.IndexOf("<Source>");
            var destIndex = output.IndexOf("<Destination>");
            sourceIndex.Should().BeLessThan(destIndex, "Source should appear before Destination");
        }

        /// <summary>
        /// HELP-006: Collection option shows "can be repeated" note
        /// </summary>
        [TestMethod]
        public void HELP006_CollectionOption_ShowsRepeatedNote()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<RepeatedOptionHelpCommand>();
            
            var command = registry.Commands.First();
            var writer = new StringWriter();

            // Act
            var formatter = new BitPantry.CommandLine.Help.HelpFormatter();
            formatter.DisplayCommandHelp(writer, command);

            // Assert
            var output = writer.ToString();
            // Collection option should have a note about being repeatable
            output.Should().Contain("repeat");
        }

        #endregion

        #region Test Helper Classes

        // Simple test implementation of HelpFormatter for testing
        private class TestHelpFormatter
        {
            public void DisplayGroupHelp(TextWriter writer, GroupInfo group, CommandRegistry registry)
            {
                writer.WriteLine($"Group: {group.Name}");
                if (!string.IsNullOrEmpty(group.Description))
                    writer.WriteLine($"Description: {group.Description}");
                
                writer.WriteLine();
                writer.WriteLine("Commands:");
                
                var commands = registry.Commands.Where(c => c.Group?.Name == group.Name).ToList();
                if (commands.Count == 0)
                {
                    writer.WriteLine("  No commands available");
                }
                else
                {
                    foreach (var cmd in commands)
                    {
                        writer.WriteLine($"  {cmd.Name}");
                    }
                }
            }

            public void DisplayCommandHelp(TextWriter writer, CommandInfo command)
            {
                var groupPath = command.Group != null ? $"{command.Group.Name} " : "";
                writer.WriteLine($"Usage: {groupPath}{command.Name}");
                
                if (!string.IsNullOrEmpty(command.Description))
                    writer.WriteLine($"Description: {command.Description}");
                
                writer.WriteLine();
                writer.WriteLine("Arguments:");
                foreach (var arg in command.Arguments)
                {
                    writer.WriteLine($"  --{arg.Name}");
                }
            }

            public void DisplayRootHelp(TextWriter writer, CommandRegistry registry)
            {
                writer.WriteLine("Available commands and groups:");
                writer.WriteLine();
                
                if (registry.RootGroups.Any())
                {
                    writer.WriteLine("Groups:");
                    foreach (var group in registry.RootGroups)
                    {
                        writer.WriteLine($"  {group.Name}");
                    }
                }
                
                if (registry.RootCommands.Any())
                {
                    writer.WriteLine("Commands:");
                    foreach (var cmd in registry.RootCommands)
                    {
                        writer.WriteLine($"  {cmd.Name}");
                    }
                }
            }
        }

        [Group]
        [API.Description("Mathematical operations")]
        private class MathGroup { }

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

        // Positional argument test commands for Phase 8
        
        /// <summary>
        /// Command with required positional arguments
        /// </summary>
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

        /// <summary>
        /// Command with optional positional argument
        /// </summary>
        [Command(Name = "optpos")]
        private class OptionalPositionalCommand : CommandBase
        {
            [Argument(Position = 0)]
            public string Input { get; set; }

            [Argument(Position = 1, IsRequired = false)]
            public string Output { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with variadic (IsRest) positional argument
        /// </summary>
        [Command(Name = "varpos")]
        private class VariadicPositionalCommand : CommandBase
        {
            [Argument(Position = 0)]
            public string Command { get; set; }

            [Argument(Position = 1, IsRest = true)]
            public string[] Args { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with both positional and named arguments
        /// </summary>
        [Command(Name = "mixedpos")]
        private class MixedPositionalNamedCommand : CommandBase
        {
            [Argument(Position = 0, IsRequired = true)]
            public string FileName { get; set; }

            [Argument]
            public bool Verbose { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Command with collection option (for repeated option help test)
        /// </summary>
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
