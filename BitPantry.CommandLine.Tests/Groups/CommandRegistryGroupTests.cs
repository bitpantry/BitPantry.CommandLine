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
        private CommandRegistryBuilder _builder;
        private ICommandRegistry _registry;

        [TestInitialize]
        public void TestInitialize()
        {
            _builder = new CommandRegistryBuilder();
        }

        #region RegisterGroup Tests

        [TestMethod]
        public void RegisterGroup_SingleGroup_GroupIsRegistered()
        {
            // Arrange & Act
            _builder.RegisterGroup(typeof(MathGroup));
            _builder.RegisterCommand<DummyMathCommand>();

            _registry = _builder.Build();

            // Assert
            _registry.Groups.Should().HaveCount(1);
            _registry.Groups[0].Name.Should().Be("math");
            _registry.Groups[0].MarkerType.Should().Be(typeof(MathGroup));
        }

        [TestMethod]
        public void RegisterGroup_WithDescriptionAttribute_DescriptionIsSet()
        {
            // Arrange & Act
            _builder.RegisterGroup(typeof(DescribedGroup));
            _builder.RegisterCommand<DummyDescribedGroupCommand>();

            _registry = _builder.Build();

            // Assert
            _registry.Groups[0].Description.Should().Be("A group with description");
        }

        [TestMethod]
        public void RegisterGroup_WithNameOverride_UsesProvidedName()
        {
            // Arrange & Act
            _builder.RegisterGroup(typeof(CustomNameGroup));
            _builder.RegisterCommand<DummyCustomNameGroupCommand>();

            _registry = _builder.Build();

            // Assert
            _registry.Groups[0].Name.Should().Be("custom-name");
        }

        [TestMethod]
        public void RegisterGroup_MultipleGroups_AllRegistered()
        {
            // Arrange & Act
            _builder.RegisterGroup(typeof(MathGroup));
            _builder.RegisterGroup(typeof(FilesGroup));
            _builder.RegisterCommand<DummyMathCommand>();
            _builder.RegisterCommand<DummyFilesCommand>();

            _registry = _builder.Build();

            // Assert
            _registry.Groups.Should().HaveCount(2);
        }

        [TestMethod]
        public void RootGroups_ReturnsOnlyTopLevelGroups()
        {
            // Arrange
            _builder.RegisterGroup(typeof(MathGroup));
            _builder.RegisterCommand<DummyMathCommand>();

            _registry = _builder.Build();

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
            _builder.RegisterGroup(typeof(MathGroup));
            _builder.RegisterCommand<DummyMathCommand>();

            _registry = _builder.Build();

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
            _builder.RegisterGroup(typeof(MathGroup));
            _builder.RegisterCommand<DummyMathCommand>();

            _registry = _builder.Build();

            // Act
            var group = _registry.FindGroup("nonexistent");

            // Assert
            group.Should().BeNull();
        }

        [TestMethod]
        public void FindGroup_CaseInsensitive_ReturnsGroup()
        {
            // Arrange
            _builder.RegisterGroup(typeof(MathGroup));
            _builder.RegisterCommand<DummyMathCommand>();

            _registry = _builder.Build();

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
            _builder.RegisterCommand<RootCommand>();

            _registry = _builder.Build();

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
            _builder.RegisterGroup(typeof(MathGroup));
            _builder.RegisterCommand<AddCommand>();

            _registry = _builder.Build();

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
            _builder.RegisterCommand<RootCommand>();

            _registry = _builder.Build();

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
            _builder.RegisterCommand<RootCommand>();
            _builder.RegisterGroup(typeof(MathGroup));
            _builder.RegisterCommand<AddCommand>();

            _registry = _builder.Build();

            // Assert
            _registry.RootCommands.Should().HaveCount(1);
            _registry.RootCommands[0].Name.Should().Be("rootcmd");
        }

        #endregion

        // Test helper classes
        [Group]
        private class MathGroup { }

        [Group]
        [API.Description("A group with description")]
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

        [InGroup<MathGroup>]
        [Command(Name = "add")]
        private class AddCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [InGroup<MathGroup>]
        [Command(Name = "dummy")]
        private class DummyMathCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [InGroup<DescribedGroup>]
        [Command(Name = "dummy")]
        private class DummyDescribedGroupCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [InGroup<CustomNameGroup>]
        [Command(Name = "dummy")]
        private class DummyCustomNameGroupCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [InGroup<FilesGroup>]
        [Command(Name = "dummy")]
        private class DummyFilesCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }
    }
}
