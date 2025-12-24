using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Tests for CommandRegistry group registration and lookup.
    /// T008: Test RegisterGroup, FindGroup, FindCommand methods
    /// </summary>
    [TestClass]
    public class CommandRegistryGroupTests
    {
        private CommandRegistry _registry;

        [TestInitialize]
        public void TestInitialize()
        {
            _registry = new CommandRegistry();
        }

        #region RegisterGroup Tests

        [TestMethod]
        public void RegisterGroup_SingleGroup_GroupIsRegistered()
        {
            // Arrange & Act
            _registry.RegisterGroup(typeof(MathGroup));

            // Assert
            _registry.Groups.Should().HaveCount(1);
            _registry.Groups[0].Name.Should().Be("math");
            _registry.Groups[0].MarkerType.Should().Be(typeof(MathGroup));
        }

        [TestMethod]
        public void RegisterGroup_WithDescriptionAttribute_DescriptionIsSet()
        {
            // Arrange & Act
            _registry.RegisterGroup(typeof(DescribedGroup));

            // Assert
            _registry.Groups[0].Description.Should().Be("A group with description");
        }

        [TestMethod]
        public void RegisterGroup_WithNameOverride_UsesProvidedName()
        {
            // Arrange & Act
            _registry.RegisterGroup(typeof(CustomNameGroup));

            // Assert
            _registry.Groups[0].Name.Should().Be("custom-name");
        }

        [TestMethod]
        public void RegisterGroup_MultipleGroups_AllRegistered()
        {
            // Arrange & Act
            _registry.RegisterGroup(typeof(MathGroup));
            _registry.RegisterGroup(typeof(FilesGroup));

            // Assert
            _registry.Groups.Should().HaveCount(2);
        }

        [TestMethod]
        public void RootGroups_ReturnsOnlyTopLevelGroups()
        {
            // Arrange
            _registry.RegisterGroup(typeof(MathGroup));

            // Assert
            _registry.RootGroups.Should().HaveCount(1);
            _registry.RootGroups[0].Parent.Should().BeNull();
        }

        #endregion

        #region FindGroup Tests

        [TestMethod]
        public void FindGroup_ExistingGroup_ReturnsGroup()
        {
            // Arrange
            _registry.RegisterGroup(typeof(MathGroup));

            // Act
            var group = _registry.FindGroup("math");

            // Assert
            group.Should().NotBeNull();
            group.Name.Should().Be("math");
        }

        [TestMethod]
        public void FindGroup_NonExistingGroup_ReturnsNull()
        {
            // Arrange
            _registry.RegisterGroup(typeof(MathGroup));

            // Act
            var group = _registry.FindGroup("nonexistent");

            // Assert
            group.Should().BeNull();
        }

        [TestMethod]
        public void FindGroup_CaseInsensitive_ReturnsGroup()
        {
            // Arrange
            _registry.RegisterGroup(typeof(MathGroup));

            // Act
            var group = _registry.FindGroup("MATH");

            // Assert
            group.Should().NotBeNull();
            group.Name.Should().Be("math");
        }

        #endregion

        #region FindCommand Tests

        [TestMethod]
        public void FindCommand_RootCommand_ReturnsCommand()
        {
            // Arrange
            _registry.RegisterCommand<RootCommand>();

            // Act
            var cmd = _registry.FindCommand("rootcmd");

            // Assert
            cmd.Should().NotBeNull();
            cmd.Name.Should().Be("rootcmd");
        }

        [TestMethod]
        public void FindCommand_GroupedCommand_WithGroup_ReturnsCommand()
        {
            // Arrange
            _registry.RegisterGroup(typeof(MathGroup));
            _registry.RegisterCommand<AddCommand>();

            // Act
            var mathGroup = _registry.FindGroup("math");
            var cmd = _registry.FindCommand("add", mathGroup);

            // Assert
            cmd.Should().NotBeNull();
            cmd.Name.Should().Be("add");
        }

        [TestMethod]
        public void FindCommand_NonExisting_ReturnsNull()
        {
            // Arrange
            _registry.RegisterCommand<RootCommand>();

            // Act
            var cmd = _registry.FindCommand("nonexistent");

            // Assert
            cmd.Should().BeNull();
        }

        #endregion

        #region RootCommands Tests

        [TestMethod]
        public void RootCommands_ReturnsOnlyRootLevelCommands()
        {
            // Arrange
            _registry.RegisterCommand<RootCommand>();
            _registry.RegisterGroup(typeof(MathGroup));
            _registry.RegisterCommand<AddCommand>();

            // Assert
            _registry.RootCommands.Should().HaveCount(1);
            _registry.RootCommands[0].Name.Should().Be("rootcmd");
        }

        #endregion

        // Test helper classes
        [Group]
        private class MathGroup { }

        [Group(Description = "A group with description")]
        private class DescribedGroup { }

        [Group(Name = "custom-name")]
        private class CustomNameGroup { }

        [Group]
        private class FilesGroup { }

        [Command(Name = "rootcmd")]
        private class RootCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(MathGroup), Name = "add")]
        private class AddCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }
    }
}
