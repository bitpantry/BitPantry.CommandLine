using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Tests for <see cref="CommandRegistry.RegisterCommandsAsRemote"/> behavior,
    /// including duplicate handling when local and remote commands share the same name/group.
    /// </summary>
    [TestClass]
    public class RemoteCommandRegistrationTests
    {
        #region Test Helpers

        [Group(Name = "explore")]
        public class ExploreGroup { }

        [InGroup<ExploreGroup>]
        [Command(Name = "browse")]
        public class LocalBrowseCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [InGroup<ExploreGroup>]
        [Command(Name = "theme")]
        public class LocalThemeCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "status")]
        public class LocalStatusCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        private static CommandInfo CreateRemoteCommandInfo(string name, string groupPath = null)
        {
            var info = new CommandInfo
            {
                Name = name,
                Type = typeof(CommandBase), // placeholder type
                Description = $"Remote {name} command",
                IsRemote = false, // RegisterCommandsAsRemote will set this to true
                Arguments = new List<ArgumentInfo>()
            };

            if (groupPath != null)
            {
                // Set group path via the internal setter for serialized group path
                info.GroupPath = groupPath;
            }

            return info;
        }

        #endregion

        #region Local-takes-precedence tests

        /// <summary>
        /// Given: A local command "browse" exists in group "explore"
        /// When: RegisterCommandsAsRemote is called with a remote "browse" in the same group
        /// Then: The remote command is silently skipped; only the local command remains
        /// </summary>
        [TestMethod]
        public void RegisterCommandsAsRemote_LocalDuplicateInGroup_SkipsRemote()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<LocalBrowseCommand>();

            var registry = builder.Build();

            var remoteBrowse = CreateRemoteCommandInfo("browse", "explore");

            // Act
            var skipped = registry.RegisterCommandsAsRemote(new[] { remoteBrowse });

            // Assert
            var browseCommands = registry.Commands
                .Where(c => c.Name == "browse")
                .ToList();

            browseCommands.Should().HaveCount(1, "local command should take precedence — remote is skipped");
            browseCommands[0].IsRemote.Should().BeFalse("the surviving command should be the local one");

            skipped.Should().ContainSingle()
                .Which.Should().Be("explore browse", "skipped list should contain the fully qualified name");
        }

        /// <summary>
        /// Given: A local command "browse" and "theme" in group "explore"
        /// When: RegisterCommandsAsRemote is called with remote "browse", "theme", and "stats" in the same group
        /// Then: Remote duplicates are skipped; only the new "stats" command is added
        /// </summary>
        [TestMethod]
        public void RegisterCommandsAsRemote_MultipleDuplicates_SkipsAllLocalConflicts()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<LocalBrowseCommand>();
            builder.RegisterCommand<LocalThemeCommand>();

            var registry = builder.Build();

            var remoteBrowse = CreateRemoteCommandInfo("browse", "explore");
            var remoteTheme = CreateRemoteCommandInfo("theme", "explore");
            var remoteStats = CreateRemoteCommandInfo("stats", "explore");

            // Act
            var skipped = registry.RegisterCommandsAsRemote(new[] { remoteBrowse, remoteTheme, remoteStats });

            // Assert
            registry.Commands.Where(c => c.Name == "browse").Should().HaveCount(1);
            registry.Commands.Where(c => c.Name == "theme").Should().HaveCount(1);
            registry.Commands.Where(c => c.Name == "stats").Should().HaveCount(1);

            registry.Commands.Single(c => c.Name == "browse").IsRemote.Should().BeFalse("local browse wins");
            registry.Commands.Single(c => c.Name == "theme").IsRemote.Should().BeFalse("local theme wins");
            registry.Commands.Single(c => c.Name == "stats").IsRemote.Should().BeTrue("stats is new, added as remote");

            skipped.Should().HaveCount(2);
            skipped.Should().Contain("explore browse");
            skipped.Should().Contain("explore theme");
        }

        #endregion

        #region Remote-replaces-remote tests

        /// <summary>
        /// Given: A remote command "info" already registered in group "explore"
        /// When: RegisterCommandsAsRemote is called again with a new remote "info" in the same group
        /// Then: The original remote command is replaced by the new one
        /// </summary>
        [TestMethod]
        public void RegisterCommandsAsRemote_RemoteDuplicate_ReplacesExistingRemote()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<LocalBrowseCommand>(); // register local to create the group

            var registry = builder.Build();

            var remoteInfoV1 = CreateRemoteCommandInfo("info", "explore");
            remoteInfoV1.Description = "Version 1";

            var remoteInfoV2 = CreateRemoteCommandInfo("info", "explore");
            remoteInfoV2.Description = "Version 2";

            // Act — register V1 then V2
            var skippedV1 = registry.RegisterCommandsAsRemote(new[] { remoteInfoV1 });
            var skippedV2 = registry.RegisterCommandsAsRemote(new[] { remoteInfoV2 });

            // Assert
            var infoCmds = registry.Commands.Where(c => c.Name == "info").ToList();
            infoCmds.Should().HaveCount(1, "second registration should replace the first");
            infoCmds[0].Description.Should().Be("Version 2");
            infoCmds[0].IsRemote.Should().BeTrue();

            skippedV1.Should().BeEmpty("first remote registration has no conflict");
            skippedV2.Should().BeEmpty("replacing a remote with another remote is not a skip");
        }

        #endregion

        #region Normal registration (no collision)

        /// <summary>
        /// Given: A local command "browse" in group "explore"
        /// When: RegisterCommandsAsRemote is called with a non-conflicting remote "info" in the same group
        /// Then: Both commands exist (local browse + remote info)
        /// </summary>
        [TestMethod]
        public void RegisterCommandsAsRemote_NoDuplicate_AddsRemoteCommand()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<LocalBrowseCommand>();

            var registry = builder.Build();

            var remoteInfo = CreateRemoteCommandInfo("info", "explore");

            // Act
            var skipped = registry.RegisterCommandsAsRemote(new[] { remoteInfo });

            // Assert
            registry.Commands.Should().HaveCount(2);
            registry.Commands.Should().ContainSingle(c => c.Name == "browse" && !c.IsRemote);
            registry.Commands.Should().ContainSingle(c => c.Name == "info" && c.IsRemote);
            skipped.Should().BeEmpty("no conflicts means nothing skipped");
        }

        /// <summary>
        /// Given: A local root command "status" (no group)
        /// When: RegisterCommandsAsRemote is called with a remote "status" in a group
        /// Then: Both commands exist — different groups means no collision
        /// </summary>
        [TestMethod]
        public void RegisterCommandsAsRemote_SameNameDifferentGroup_NoDuplicate()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<LocalStatusCommand>();

            var registry = builder.Build();

            var remoteStatus = CreateRemoteCommandInfo("status", "explore");

            // Act
            var skipped = registry.RegisterCommandsAsRemote(new[] { remoteStatus });

            // Assert
            var statusCmds = registry.Commands.Where(c => c.Name == "status").ToList();
            statusCmds.Should().HaveCount(2, "different groups = no collision");
            statusCmds.Should().ContainSingle(c => !c.IsRemote, "local root-level status");
            statusCmds.Should().ContainSingle(c => c.IsRemote, "remote status in 'explore' group");
            skipped.Should().BeEmpty("different groups means no conflict");
        }

        #endregion

        #region DropRemoteCommands

        /// <summary>
        /// Given: Remote commands registered alongside local commands
        /// When: DropRemoteCommands is called
        /// Then: Only local commands remain
        /// </summary>
        [TestMethod]
        public void DropRemoteCommands_RemovesAllRemote_KeepsLocal()
        {
            // Arrange
            var builder = new CommandRegistryBuilder();
            builder.ReplaceDuplicateCommands = true;
            builder.RegisterCommand<LocalBrowseCommand>();

            var registry = builder.Build();

            var remoteInfo = CreateRemoteCommandInfo("info", "explore");
            registry.RegisterCommandsAsRemote(new[] { remoteInfo });

            // Sanity check
            registry.Commands.Should().HaveCount(2);

            // Act
            registry.DropRemoteCommands();

            // Assert
            registry.Commands.Should().HaveCount(1);
            registry.Commands.Single().Name.Should().Be("browse");
            registry.Commands.Single().IsRemote.Should().BeFalse();
        }

        #endregion
    }
}
