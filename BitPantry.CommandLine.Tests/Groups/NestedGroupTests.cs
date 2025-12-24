using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Execution;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Groups
{
    /// <summary>
    /// Tests for nested groups via C# class nesting.
    /// T042-T050: User Story 3 - Nested Groups
    /// </summary>
    [TestClass]
    public class NestedGroupTests
    {
        private CommandLineApplication _app;

        [TestInitialize]
        public void Setup()
        {
            _app = new CommandLineApplicationBuilder()
                .RegisterCommand<UploadCommand>()
                .RegisterCommand<DownloadCommand>()
                .RegisterCommand<FilesListCommand>()
                .Build();
        }

        #region Registration Tests (T042)

        [TestMethod]
        public void RegisterNestedGroup_ParentChildRelationship()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;

            // Act - register a command that references a nested group
            registry.RegisterCommand<UploadCommand>();

            // Assert - both Files and Io groups should be registered
            registry.Groups.Should().HaveCount(2);
            registry.Groups.Should().ContainSingle(g => g.Name == "files");
            registry.Groups.Should().ContainSingle(g => g.Name == "io");
            
            // Parent-child relationship
            var ioGroup = registry.Groups.First(g => g.Name == "io");
            ioGroup.Parent.Should().NotBeNull();
            ioGroup.Parent.Name.Should().Be("files");
        }

        [TestMethod]
        public void RegisterNestedGroup_FullPathDerived()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;

            // Act
            registry.RegisterCommand<UploadCommand>();

            // Assert - nested group has full path
            var ioGroup = registry.Groups.First(g => g.Name == "io");
            ioGroup.FullPath.Should().Be("files io");
        }

        #endregion

        #region Resolution Tests (T043)

        [TestMethod]
        public void Resolve_NestedGroupCommand_Success()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;
            registry.RegisterCommand<UploadCommand>();

            // Act
            var cmdInfo = registry.Find("files io upload");

            // Assert
            cmdInfo.Should().NotBeNull();
            cmdInfo.Name.Should().Be("upload");
        }

        [TestMethod]
        public async Task Invoke_NestedGroupCommand_Success()
        {
            // Arrange & Act
            var result = await _app.Run("files io upload --path /test/file.txt");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            result.Result.Should().Be("/test/file.txt");
        }

        #endregion

        #region Deep Nesting Tests (T044)

        [TestMethod]
        public void RegisterDeeplyNested_ThreeLevels_Success()
        {
            // Arrange
            var registry = new CommandRegistry();
            registry.ReplaceDuplicateCommands = true;

            // Act
            registry.RegisterCommand<DeepNestedCommand>();

            // Assert
            registry.Groups.Should().HaveCount(3);
            var level3 = registry.Groups.First(g => g.Name == "level3");
            level3.FullPath.Should().Be("level1 level2 level3");
        }

        [TestMethod]
        public async Task Invoke_DeeplyNestedCommand_Success()
        {
            // Arrange
            var app = new CommandLineApplicationBuilder()
                .RegisterCommand<DeepNestedCommand>()
                .Build();

            // Act
            var result = await app.Run("level1 level2 level3 deepcmd");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        #endregion

        #region Group With Only Subgroups Tests (T045)

        [TestMethod]
        public async Task GroupWithOnlySubgroups_ShowsSubgroupsInHelp()
        {
            // Arrange - files group has no direct commands, only io subgroup
            var app = new CommandLineApplicationBuilder()
                .RegisterCommand<UploadCommand>()  // This is in files.io, not directly in files
                .Build();

            // Act - requesting help for 'files' group
            var result = await app.Run("files --help");

            // Assert - should show io subgroup
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public async Task GroupWithSubgroupsAndCommands_ShowsBoth()
        {
            // Arrange - files group has both direct command and io subgroup

            // Act
            var result = await _app.Run("files --help");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        #endregion

        #region Parent Group Help Tests (T050)

        [TestMethod]
        public async Task FilesGroup_ShowsIoSubgroup()
        {
            // Arrange & Act
            var result = await _app.Run("files");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        [TestMethod]
        public async Task FilesIoGroup_ShowsUploadCommand()
        {
            // Arrange & Act
            var result = await _app.Run("files io");

            // Assert  
            result.ResultCode.Should().Be(RunResultCode.Success);
        }

        #endregion

        #region Test Helper Classes

        [Group(Description = "File operations")]
        public class FilesGroup 
        {
            [Group(Description = "I/O operations")]
            public class IoGroup { }
        }

        [Command(Group = typeof(FilesGroup.IoGroup), Name = "upload")]
        public class UploadCommand : CommandBase
        {
            [Argument]
            public string Path { get; set; }

            public string Execute(CommandExecutionContext ctx) => Path;
        }

        [Command(Group = typeof(FilesGroup.IoGroup), Name = "download")]
        public class DownloadCommand : CommandBase
        {
            [Argument]
            public string Url { get; set; }

            public string Execute(CommandExecutionContext ctx) => Url;
        }

        [Command(Group = typeof(FilesGroup), Name = "list")]
        public class FilesListCommand : CommandBase
        {
            public string Execute(CommandExecutionContext ctx) => "files listed";
        }

        // For deep nesting tests
        [Group]
        public class Level1Group
        {
            [Group]
            public class Level2Group
            {
                [Group]
                public class Level3Group { }
            }
        }

        [Command(Group = typeof(Level1Group.Level2Group.Level3Group), Name = "deepcmd")]
        public class DeepNestedCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion
    }
}
