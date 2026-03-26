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

        #region Remote Group FindCommand Tests

        /// <summary>
        /// Test Case 3: FindCommand with a remote group should return the remote command
        /// that is actually in that group (by reference equality, not MarkerType).
        /// </summary>
        [TestMethod]
        public void FindCommand_RemoteGroupedCommand_WithRemoteGroup_ReturnsCommand()
        {
            // Test Validity Check:
            //   Invokes code under test: YES - calls FindCommand
            //   Breakage detection: YES - would fail if MarkerType comparison used
            //   Not a tautology: YES - verifies command lookup by group reference

            // Arrange
            _registry = _builder.Build();

            // Register a remote command in a remote group
            var remoteCmd = new CommandInfo
            {
                Name = "remote-action",
                Type = typeof(CommandBase),
                Description = "A remote command",
                Arguments = new System.Collections.Generic.List<ArgumentInfo>()
            };
            remoteCmd.GroupPath = "admin";

            _registry.RegisterCommandsAsRemote(new[] { remoteCmd });

            // Get the remote group that was created
            var adminGroup = _registry.FindGroup("admin");
            adminGroup.Should().NotBeNull("remote group should be created");
            adminGroup.MarkerType.Should().BeNull("remote groups have null MarkerType");

            // Act
            var found = _registry.FindCommand("remote-action", adminGroup);

            // Assert
            found.Should().NotBeNull("remote command should be found in its group");
            found.Name.Should().Be("remote-action");
            found.IsRemote.Should().BeTrue();
        }

        /// <summary>
        /// Test Case 4: FindCommand for a root command (no group) should NOT match
        /// when searching within a remote group, even though both have null MarkerType.
        /// This tests the fix for the null-collision bug.
        /// </summary>
        [TestMethod]
        public void FindCommand_RootCommand_DoesNotMatchRemoteGroup()
        {
            // Test Validity Check:
            //   Invokes code under test: YES - calls FindCommand
            //   Breakage detection: YES - would return wrongly if MarkerType (null==null) comparison used
            //   Not a tautology: YES - verifies null-group and remote-group are distinguished

            // Arrange
            _builder.RegisterCommand<RootCommand>(); // root command with no group
            _registry = _builder.Build();

            // Create a remote group (with null MarkerType)
            var remoteCmd = new CommandInfo
            {
                Name = "dummy",
                Type = typeof(CommandBase),
                Description = "Dummy to create group",
                Arguments = new System.Collections.Generic.List<ArgumentInfo>()
            };
            remoteCmd.GroupPath = "remote-group";
            _registry.RegisterCommandsAsRemote(new[] { remoteCmd });

            var remoteGroup = _registry.FindGroup("remote-group");
            remoteGroup.Should().NotBeNull();
            remoteGroup.MarkerType.Should().BeNull("remote group has null MarkerType");

            // Act - try to find the root command using the remote group
            var found = _registry.FindCommand("rootcmd", remoteGroup);

            // Assert - should NOT find the root command because it's not in the remote group
            found.Should().BeNull("root command is not in the remote group, even though both have null MarkerType");
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
