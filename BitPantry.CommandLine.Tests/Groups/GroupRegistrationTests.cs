using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Tests.Commands.AutoCompleteCommands;
using BitPantry.CommandLine.Tests.CmdAssemblies.Groups;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Integration tests for group discovery and registration from assembly.
    /// T021: Test group discovery and registration from assembly
    /// </summary>
    [TestClass]
    public class GroupRegistrationTests
    {
        [TestMethod]
        public void RegisterGroup_FromType_Registered()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;

            // Act
            registry.RegisterGroup(typeof(MathGroup));

            // Assert
            registry.Groups.Should().HaveCount(1);
            registry.Groups[0].Name.Should().Be("math");
            registry.Groups[0].MarkerType.Should().Be(typeof(MathGroup));
        }

        [TestMethod]
        public void RegisterCommand_WithGroup_LinkedToGroup()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;

            // Act - group is auto-registered when command is registered
            registry.RegisterCommand<AddCommand>();

            // Assert
            registry.Groups.Should().HaveCount(1);
            registry.Commands.Should().HaveCount(1);
            registry.Commands.First().Group.Should().NotBeNull();
            registry.Commands.First().Group.Name.Should().Be("math");
        }

        [TestMethod]
        public void RegisterCommands_ViaAssemblyScanning_GroupsDiscovered()
        {
            // Arrange & Act - use CmdAssemblies project which is a clean assembly for scanning
            var app = new CommandLineApplicationBuilder()
                .RegisterCommands(typeof(TestMathGroup));

            // Assert - groups should be discovered from the assembly
            app.CommandRegistry.Groups.Should().ContainSingle(g => g.Name == "testmath");
        }

        [TestMethod]
        public void RegisterGroup_WithCustomName_UsesCustomName()
        {
            // Arrange
            var registry = new CommandRegistry();

            // Act
            registry.RegisterGroup(typeof(CustomNamedGroup));

            // Assert
            registry.Groups.Should().HaveCount(1);
            registry.Groups[0].Name.Should().Be("custom-ops");
        }

        [TestMethod]
        public void RegisterGroup_WithDescription_DescriptionSet()
        {
            // Arrange
            var registry = new CommandRegistry();

            // Act
            registry.RegisterGroup(typeof(DescribedTestGroup));

            // Assert
            registry.Groups[0].Description.Should().Be("A group with a description");
        }

        [TestMethod]
        public void RootGroups_ReturnsOnlyTopLevel()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.RegisterGroup(typeof(MathGroup));

            // Act & Assert
            registry.RootGroups.Should().HaveCount(1);
            registry.RootGroups[0].Parent.Should().BeNull();
        }

        [TestMethod]
        public void RootCommands_ReturnsOnlyUngroupedCommands()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<AddCommand>(); // grouped under MathGroup
            registry.RegisterCommand<RootLevelCommand>(); // no group

            // Act & Assert
            registry.RootCommands.Should().HaveCount(1);
            registry.RootCommands[0].Name.Should().Be("rootlevel");
        }

        [TestMethod]
        public void FindGroup_ByName_Found()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.RegisterGroup(typeof(MathGroup));

            // Act
            var group = registry.FindGroup("math");

            // Assert
            group.Should().NotBeNull();
            group.Name.Should().Be("math");
        }

        [TestMethod]
        public void FindGroup_CaseInsensitive_Found()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.RegisterGroup(typeof(MathGroup));

            // Act
            var group = registry.FindGroup("MATH");

            // Assert
            group.Should().NotBeNull();
        }

        // Test helper classes
        [Group]
        public class MathGroup { }

        [Group(Name = "custom-ops")]
        public class CustomNamedGroup { }

        [Group(Description = "A group with a description")]
        public class DescribedTestGroup { }

        [Command(Group = typeof(MathGroup), Name = "add")]
        public class AddCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "rootlevel")]
        public class RootLevelCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }
    }
}
