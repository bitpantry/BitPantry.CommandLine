using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Execution;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Tests for startup validation of group/command configurations.
    /// T051-T060: User Story 4 - Startup Validation
    /// </summary>
    [TestClass]
    public class GroupValidationTests
    {
        #region Empty Group Validation (FR-022)

        [TestMethod]
        public void EmptyGroup_NoCommandsNoSubgroups_ThrowsOnBuild()
        {
            // Arrange - group with no commands and no subgroups
            var builder = new CommandLineApplicationBuilder()
                .RegisterGroup<EmptyGroup>();

            // Act & Assert - should throw on Build()
            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*empty*", "empty group should cause validation error");
        }

        [TestMethod]
        public void GroupWithOnlyCommands_DoesNotThrow()
        {
            // Arrange - group with commands but no subgroups
            var builder = new CommandLineApplicationBuilder()
                .RegisterCommand<ValidGroupCommand>();

            // Act & Assert - should not throw
            Action act = () => builder.Build();
            act.Should().NotThrow("group with commands is valid");
        }

        [TestMethod]
        public void GroupWithOnlySubgroups_DoesNotThrow()
        {
            // Arrange - group with subgroups that have commands
            var builder = new CommandLineApplicationBuilder()
                .RegisterCommand<SubgroupCommand>();

            // Act & Assert - should not throw
            Action act = () => builder.Build();
            act.Should().NotThrow("group with subgroups is valid");
        }

        #endregion

        #region Name Collision Detection

        [TestMethod]
        public void CommandGroupSameName_ThrowsOnBuild()
        {
            // Arrange - command and group at same level with same name
            // This would be "collision" as both a group and a command under root
            var builder = new CommandLineApplicationBuilder()
                .RegisterGroup<CollisionGroup>()
                .RegisterCommand<RootCollisionCommand>();

            // Act & Assert - validation happens on Build()
            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*collision*", "same name for group and command should fail");
        }

        [TestMethod]
        public void DuplicateCommand_DefaultBehavior_ThrowsOnRegister()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.RegisterCommand<ValidGroupCommand>();

            // Act - register same command again
            Action act = () => builder.RegisterCommand<ValidGroupCommand>();

            // Assert - with default ReplaceDuplicateCommands = false
            act.Should().Throw<ArgumentException>()
                .WithMessage("*already registered*");
        }

        [TestMethod]
        public void DuplicateCommand_WithReplace_Succeeds()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<ValidGroupCommand>();

            // Act - register same command again
            Action act = () => builder.RegisterCommand<ValidGroupCommand>();

            // Assert - should not throw when replacing is allowed
            act.Should().NotThrow();
        }

        #endregion

        #region Reserved Names (FR-027)

        [TestMethod]
        public void ArgumentNamedHelp_ThrowsOnBuild()
        {
            // Arrange - command with argument named "help"
            var builder = new CommandLineApplicationBuilder()
                .RegisterCommand<CommandWithHelpArgument>();

            // Act & Assert
            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*help*reserved*", "argument named 'help' should be reserved");
        }

        [TestMethod]
        public void ArgumentWithAliasH_ThrowsOnBuild()
        {
            // Arrange - command with argument using alias 'h'
            var builder = new CommandLineApplicationBuilder()
                .RegisterCommand<CommandWithHAliasArgument>();

            // Act & Assert
            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*alias*h*reserved*", "alias 'h' should be reserved");
        }

        #endregion

        #region Non-Group Class Reference

        [TestMethod]
        public void CommandReferencesNonGroupClass_ThrowsOnRegister()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();

            // Act - register command that references a non-group class
            Action act = () => builder.RegisterCommand<CommandWithInvalidGroup>();

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*[Group]*", "referencing non-group class should fail");
        }

        #endregion

        #region Valid Configuration

        [TestMethod]
        public void ValidConfiguration_StartsSuccessfully()
        {
            // Arrange - valid group structure with commands
            var builder = new CommandLineApplicationBuilder()
                .RegisterCommand<ValidGroupCommand>()
                .RegisterCommand<SubgroupCommand>();

            // Act & Assert
            Action act = () => builder.Build();
            act.Should().NotThrow("valid configuration should build successfully");
        }

        #endregion

        #region Built-in Override Tests (T066-T069)

        [TestMethod]
        public void BuiltInConflict_DefaultBehavior_ThrowsWithBuiltInIdentified()
        {
            // Arrange - try to register a command with same name as built-in 'lc'
            var builder = new CommandLineApplicationBuilder();

            // Act & Assert - exception thrown at registration time
            Action act = () => builder.RegisterCommand<ConflictingLcCommand>();
            act.Should().Throw<ArgumentException>()
                .WithMessage("*already registered*ListCommandsCommand*");
        }

        [TestMethod]
        public void BuiltInConflict_WithReplace_CustomOverridesBuiltIn()
        {
            // Arrange - enable replacement
            var appBuilder = new CommandLineApplicationBuilder();
            appBuilder.CommandRegistryBuilder.ReplaceDuplicateCommands = true;
            appBuilder.RegisterCommand<ConflictingLcCommand>();

            // Act & Assert - should not throw, custom replaces built-in
            CommandLineApplication app = null;
            Action act = () => app = appBuilder.Build();
            act.Should().NotThrow("ReplaceDuplicateCommands should allow override");

            // Verify custom command is registered (only one 'lc' command)
            var registry = app.Services.GetRequiredService<ICommandRegistry>();
            registry.Commands.Where(c => c.Name == "lc").Should().HaveCount(1);
            registry.Commands.Should().Contain(c => c.Name == "lc" && c.Type == typeof(ConflictingLcCommand));
        }

        #endregion

        #region Test Helper Classes

        [Group]
        [API.Description("Empty group for testing")]
        public class EmptyGroup { }

        [Group]
        [API.Description("Valid group")]
        public class ValidGroup { }

        [Command(Group = typeof(ValidGroup), Name = "validcmd")]
        public class ValidGroupCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Group]
        [API.Description("Parent group")]
        public class ParentWithSubgroup
        {
            [Group]
            [API.Description("Subgroup")]
            public class SubGroup { }
        }

        [Command(Group = typeof(ParentWithSubgroup.SubGroup), Name = "subcmd")]
        public class SubgroupCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Group(Name = "collision")]
        public class CollisionGroup { }

        [Command(Name = "collision")]
        public class RootCollisionCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "badhelp")]
        public class CommandWithHelpArgument : CommandBase
        {
            [Argument(Name = "help")]
            public string HelpArg { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "badhalias")]
        public class CommandWithHAliasArgument : CommandBase
        {
            [Argument]
            [Alias('h')]
            public string SomeArg { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        // Non-group class for invalid reference test
        public class NotAGroup { }

        [Command(Group = typeof(NotAGroup), Name = "invalidref")]
        public class CommandWithInvalidGroup : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        // Command that conflicts with built-in 'lc' command
        [Command(Name = "lc")]
        public class ConflictingLcCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion
    }
}
