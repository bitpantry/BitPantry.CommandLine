using BitPantry.CommandLine.API;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Tests for the [InGroup&lt;T&gt;] attribute functionality.
    /// </summary>
    [TestClass]
    public class InGroupAttributeTests
    {
        #region Test Commands and Groups

        [Group]
        [Description("Math operations group")]
        public class MathGroup { }

        [Group]
        public class FilesGroup
        {
            [Group]
            public class IoGroup { }
        }

        [InGroup<MathGroup>]
        [Description("Add command using InGroup attribute")]
        public class AddCommand : CommandBase
        {
            [Argument]
            public int Value { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [InGroup<FilesGroup.IoGroup>]
        [Description("Upload command in nested group")]
        public class UploadCommand : CommandBase
        {
            [Argument]
            public string Path { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        // Second command in MathGroup for multi-command tests
        [InGroup<MathGroup>]
        [Description("Subtract command in math group")]
        public class SubtractCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        // Root-level command (no group)
        [Description("Help command at root")]
        public class HelpCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        #region Basic Registration Tests

        [TestMethod]
        public void InGroupAttribute_RegistersCommandInGroup()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();

            // Act
            builder.RegisterCommand<AddCommand>();
            var registry = builder.Build();

            // Assert
            registry.Commands.Should().HaveCount(1);
            registry.Groups.Should().HaveCount(1);
            
            var cmd = registry.Commands.First();
            cmd.Name.Should().Be("AddCommand");
            cmd.Group.Should().NotBeNull();
            cmd.Group.Name.Should().Be("math");
        }

        [TestMethod]
        public void InGroupAttribute_AutoRegistersGroup()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();

            // Act - don't explicitly register group, just the command
            builder.RegisterCommand<AddCommand>();
            var registry = builder.Build();

            // Assert - group should be auto-registered
            registry.Groups.Should().ContainSingle(g => g.Name == "math");
            registry.Groups.First().MarkerType.Should().Be(typeof(MathGroup));
        }

        [TestMethod]
        public void InGroupAttribute_NestedGroup_RegistersHierarchy()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();

            // Act
            builder.RegisterCommand<UploadCommand>();
            var registry = builder.Build();

            // Assert - both parent and child groups should exist
            registry.Groups.Should().HaveCount(2);
            registry.Groups.Should().ContainSingle(g => g.Name == "files");
            registry.Groups.Should().ContainSingle(g => g.Name == "io");

            var ioGroup = registry.Groups.First(g => g.Name == "io");
            ioGroup.Parent.Should().NotBeNull();
            ioGroup.Parent.Name.Should().Be("files");
            ioGroup.FullPath.Should().Be("files io");
        }

        [TestMethod]
        public void InGroupAttribute_CommandFullyQualifiedName_IncludesGroupPath()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<UploadCommand>();
            var registry = builder.Build();

            // Act
            var cmd = registry.Commands.First();

            // Assert
            cmd.FullyQualifiedName.Should().Be("files io UploadCommand");
        }

        #endregion

        #region Multiple Commands In Same Group Tests

        [TestMethod]
        public void InGroupAttribute_MultipleCommandsInSameGroup_RegistersCorrectly()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();

            // Act - using [InGroup<T>] attribute
            builder.RegisterCommand<SubtractCommand>();
            var registry = builder.Build();

            // Assert
            registry.Commands.Should().HaveCount(1);
            var cmd = registry.Commands.First();
            cmd.Group.Should().NotBeNull();
            cmd.Group.Name.Should().Be("math");
        }

        [TestMethod]
        public void InGroupAttribute_MultipleCommands_AllInSameGroup()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;

            // Act - register multiple commands with [InGroup<T>]
            builder.RegisterCommand<AddCommand>();      // [InGroup<MathGroup>]
            builder.RegisterCommand<SubtractCommand>(); // [InGroup<MathGroup>]
            var registry = builder.Build();

            // Assert - both should be in the same group
            registry.Commands.Should().HaveCount(2);
            registry.Groups.Should().HaveCount(1);
            
            var group = registry.Groups.First();
            group.Commands.Should().HaveCount(2);
        }

        #endregion

        #region Root Command Tests

        [TestMethod]
        public void NoGroupAttribute_CommandIsRootLevel()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();

            // Act
            builder.RegisterCommand<HelpCommand>();
            var registry = builder.Build();

            // Assert
            registry.Commands.Should().HaveCount(1);
            registry.Groups.Should().BeEmpty();
            
            var cmd = registry.Commands.First();
            cmd.Group.Should().BeNull();
            cmd.FullyQualifiedName.Should().Be("HelpCommand");
        }

        #endregion

        #region Resolution Tests

        [TestMethod]
        public void Find_CommandByFullyQualifiedName_Works()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<AddCommand>();
            var registry = builder.Build();

            // Act
            var cmd = registry.Find("math AddCommand");

            // Assert
            cmd.Should().NotBeNull();
            cmd.Type.Should().Be(typeof(AddCommand));
        }

        [TestMethod]
        public void Find_NestedGroupCommand_Works()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<UploadCommand>();
            var registry = builder.Build();

            // Act
            var cmd = registry.Find("files io UploadCommand");

            // Assert
            cmd.Should().NotBeNull();
            cmd.Type.Should().Be(typeof(UploadCommand));
        }

        #endregion
    }
}
