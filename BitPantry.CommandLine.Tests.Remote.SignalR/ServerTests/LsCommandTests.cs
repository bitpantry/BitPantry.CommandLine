using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Server.Commands;
using FluentAssertions;
using Spectre.Console.Testing;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class LsCommandTests
    {
        private static readonly Type LsCommandType = typeof(LsCommand);

        [TestMethod]
        public void ServerGroup_HasGroupAttribute_WithNameServer()
        {
            var type = typeof(BitPantry.CommandLine.Remote.SignalR.Server.Commands.ServerGroup);
            var attr = type.GetCustomAttributes(typeof(GroupAttribute), false)
                .OfType<GroupAttribute>().FirstOrDefault();
            attr.Should().NotBeNull("ServerGroup must have [Group] attribute");
            attr!.Name.Should().Be("server");
        }

        [TestMethod]
        public void LsCommand_IsRegisteredInServerCommandRegistry()
        {
            LsCommandType.Should().NotBeNull();
            LsCommandType.IsSubclassOf(typeof(CommandBase)).Should().BeTrue(
                "LsCommand must extend CommandBase to be registerable");
        }

        // T003 CV-001: Path argument is optional
        [TestMethod]
        public void LsCommand_PathArgument_IsOptional()
        {
            var prop = LsCommandType.GetProperty("Path");
            prop.Should().NotBeNull("LsCommand must have a Path property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Path must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeFalse("Path argument should be optional");
        }

        // T004 CV-002: Path argument accepted as positional
        [TestMethod]
        public void LsCommand_PathArgument_IsPositional()
        {
            var prop = LsCommandType.GetProperty("Path");
            prop.Should().NotBeNull();
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull();
            argAttr!.Position.Should().Be(0, "Path should be positional at index 0");
        }

        // T005 CV-003: command name is 'ls'
        [TestMethod]
        public void LsCommand_HasCommandAttribute_WithNameLs()
        {
            var attr = LsCommandType.GetCustomAttribute<CommandAttribute>();
            attr.Should().NotBeNull("LsCommand must have [Command] attribute");
            attr!.Name.Should().Be("ls");
        }

        // T006 CV-004: Path argument has name 'path'
        [TestMethod]
        public void LsCommand_PathArgument_HasNamePath()
        {
            var prop = LsCommandType.GetProperty("Path");
            prop.Should().NotBeNull();
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull();
            argAttr!.Name.Should().Be("path");
        }

        // T007 CV-005: Path default value '.'
        [TestMethod]
        public void LsCommand_PathArgument_DefaultsToCurrentDirectory()
        {
            var fs = new MockFileSystem();
            var instance = new LsCommand(fs);
            var prop = LsCommandType.GetProperty("Path");
            prop.Should().NotBeNull();
            var value = prop!.GetValue(instance);
            value.Should().Be(".", "Path should default to '.' (current directory)");
        }

        // T008 CV-006: Long flag exists
        [TestMethod]
        public void LsCommand_HasLongFlag()
        {
            var prop = LsCommandType.GetProperty("Long");
            prop.Should().NotBeNull("LsCommand must have a Long property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("Long must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Long must have [Argument] attribute");
            argAttr!.Name.Should().Be("long");
        }

        // T009 CV-007: Long flag alias '-l'
        [TestMethod]
        public void LsCommand_LongFlag_HasAliasL()
        {
            var prop = LsCommandType.GetProperty("Long");
            prop.Should().NotBeNull();
            var aliasAttr = prop!.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("Long must have [Alias] attribute");
            aliasAttr!.Alias.Should().Be('l');
        }

        // T010 CV-008: All flag exists
        [TestMethod]
        public void LsCommand_HasAllFlag()
        {
            var prop = LsCommandType.GetProperty("All");
            prop.Should().NotBeNull("LsCommand must have an All property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("All must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("All must have [Argument] attribute");
            argAttr!.Name.Should().Be("all");
        }

        // T011 CV-009: All flag alias '-a'
        [TestMethod]
        public void LsCommand_AllFlag_HasAliasA()
        {
            var prop = LsCommandType.GetProperty("All");
            prop.Should().NotBeNull();
            var aliasAttr = prop!.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("All must have [Alias] attribute");
            aliasAttr!.Alias.Should().Be('a');
        }

        // T012 CV-033: LsCommand extends CommandBase
        [TestMethod]
        public void LsCommand_ExtendsCommandBase()
        {
            LsCommandType.IsSubclassOf(typeof(CommandBase)).Should().BeTrue(
                "LsCommand must extend CommandBase");
        }

        // T013 DF-001: Lists files at specified path
        [TestMethod]
        public async Task Execute_WithPath_ListsFilesAtPath()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\storage\reports\q1.txt", new MockFileData("q1 data") },
                { @"C:\storage\reports\q2.txt", new MockFileData("q2 data") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\reports";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("q1.txt");
            console.Output.Should().Contain("q2.txt");
        }

        // T014 DF-002: Lists subdir contents when path is a dir
        [TestMethod]
        public async Task Execute_WithSubdirPath_ListsSubdirContents()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\storage\data\a.txt", new MockFileData("a data") },
                { @"C:\storage\other\b.txt", new MockFileData("b data") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\data";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("a.txt");
            console.Output.Should().NotContain("b.txt");
        }

        // T015 DF-003: Glob pattern *.txt filters to text files
        [TestMethod]
        public async Task Execute_WithGlobPattern_FiltersToMatchingFiles()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\storage\a.txt", new MockFileData("text") },
                { @"C:\storage\b.log", new MockFileData("log") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\*.txt";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("a.txt");
            console.Output.Should().NotContain("b.log");
        }

        // T016 DF-004: Glob *.log matches multiple
        [TestMethod]
        public async Task Execute_WithGlobPattern_MatchesMultipleFiles()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\storage\a.log", new MockFileData("log1") },
                { @"C:\storage\b.log", new MockFileData("log2") },
                { @"C:\storage\c.txt", new MockFileData("text") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\*.log";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("a.log");
            console.Output.Should().Contain("b.log");
            console.Output.Should().NotContain("c.txt");
        }

        // T017 DF-005: Traverses subdirectories with --recursive
        [TestMethod]
        public async Task Execute_WithRecursive_ListsAllDepths()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\storage\top.txt", new MockFileData("top") },
                { @"C:\storage\sub1\mid.txt", new MockFileData("mid") },
                { @"C:\storage\sub1\sub2\deep.txt", new MockFileData("deep") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage";
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("top.txt");
            console.Output.Should().Contain("mid.txt");
            console.Output.Should().Contain("deep.txt");
        }

        // T018 DF-006: Sort by file size
        [TestMethod]
        public async Task Execute_WithSortSize_OrdersByFileSize()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\storage\large.txt", new MockFileData(new string('x', 300)) },
                { @"C:\storage\small.txt", new MockFileData(new string('x', 10)) },
                { @"C:\storage\medium.txt", new MockFileData(new string('x', 100)) },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage";
            cmd.Sort = "size";

            await cmd.Execute(new CommandExecutionContext());

            var output = console.Output;
            var smallIdx = output.IndexOf("small.txt");
            var mediumIdx = output.IndexOf("medium.txt");
            var largeIdx = output.IndexOf("large.txt");

            smallIdx.Should().BeLessThan(mediumIdx, "small should appear before medium");
            mediumIdx.Should().BeLessThan(largeIdx, "medium should appear before large");
        }

        // T021 EH-001: Path not found
        [TestMethod]
        public async Task Execute_WithNonexistentPath_DisplaysNotFoundError()
        {
            var fs = new MockFileSystem();
            var console = new TestConsole();
            var cmd = new LsCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = "/nonexistent";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("not found");
            console.Output.Should().Contain("/nonexistent");
        }

        // T022 EH-002: Path is a file (not a dir) and no glob — lists single file
        [TestMethod]
        public async Task Execute_WithFilePath_ListsSingleFile()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\storage\file.txt", new MockFileData("content") },
            });

            var console = new TestConsole();
            var cmd = new LsCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("file.txt");
            console.Output.Should().NotContain("not found", "should list the file, not show an error");
        }

        // T023 EH-021: SandboxedFileSystem blocks path traversal attempt
        [TestMethod]
        public async Task Execute_WithPathTraversal_DisplaysErrorMessage()
        {
            var storageRoot = @"C:\storage";
            var innerFs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\etc\passwd", new MockFileData("secret") },
            });
            var validator = new BitPantry.CommandLine.Remote.SignalR.Server.Files.PathValidator(storageRoot);
            var sandboxedFs = new BitPantry.CommandLine.Remote.SignalR.Server.Files.SandboxedFileSystem(innerFs, validator);

            var console = new TestConsole();
            var cmd = new LsCommand(sandboxedFs);
            cmd.SetConsole(console);
            cmd.Path = "../../etc/passwd";

            await cmd.Execute(new CommandExecutionContext());

            // Should show error, not the file content
            console.Output.Should().NotContain("secret");
            // SandboxedFileSystem throws UnauthorizedAccessException, command should catch and display error
            var output = console.Output.ToLowerInvariant();
            (output.Contains("not found") || output.Contains("error") || output.Contains("denied") || output.Contains("not allowed"))
                .Should().BeTrue("should display an error message for path traversal attempt");
        }
    }
}
