using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Server.Commands;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using Spectre.Console.Testing;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class RmCommandTests
    {
        private static readonly Type RmCommandType = typeof(RmCommand);

        // T050 CV-012: path argument is required
        [TestMethod]
        public void RmCommand_PathArgument_IsRequired()
        {
            var prop = RmCommandType.GetProperty("Path");
            prop.Should().NotBeNull("RmCommand must have a Path property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Path must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeTrue("Path argument must be required for rm");
        }

        // T051 CV-013: --recursive / -r flag allows non-empty dir deletion
        [TestMethod]
        public void RmCommand_HasRecursiveFlag()
        {
            var prop = RmCommandType.GetProperty("Recursive");
            prop.Should().NotBeNull("RmCommand must have a Recursive property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("Recursive must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Recursive must have [Argument] attribute");
            argAttr!.Name.Should().Be("recursive");
            var aliasAttr = prop.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("Recursive must have [Alias] for -r");
            aliasAttr!.Alias.Should().Be('r');
        }

        // T052 CV-014: --directory / -d flag allows empty dir deletion
        [TestMethod]
        public void RmCommand_HasDirectoryFlag()
        {
            var prop = RmCommandType.GetProperty("Directory");
            prop.Should().NotBeNull("RmCommand must have a Directory property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("Directory must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Directory must have [Argument] attribute");
            argAttr!.Name.Should().Be("directory");
            var aliasAttr = prop.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("Directory must have [Alias] for -d");
            aliasAttr!.Alias.Should().Be('d');
        }

        // T053 CV-015: --force / -f flag skips confirmation
        [TestMethod]
        public void RmCommand_HasForceFlag()
        {
            var prop = RmCommandType.GetProperty("Force");
            prop.Should().NotBeNull("RmCommand must have a Force property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("Force must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Force must have [Argument] attribute");
            argAttr!.Name.Should().Be("force");
            var aliasAttr = prop.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("Force must have [Alias] for -f");
            aliasAttr!.Alias.Should().Be('f');
        }

        // T054 CV-016: Without -r deleting non-empty dir produces error
        [TestMethod]
        public async Task Execute_NonEmptyDir_WithoutRecursive_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\mydir");
            fs.File.WriteAllText(@"C:\storage\mydir\file.txt", "content");
            var console = new TestConsole();
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\mydir";
            cmd.Recursive = false;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(@"C:\storage\mydir").Should().BeTrue("directory should not be deleted without --recursive");
            console.Output.Should().Contain("--recursive", "error should mention --recursive flag");
        }

        // T055 CV-017: Without -d deleting empty dir produces error
        [TestMethod]
        public async Task Execute_EmptyDir_WithoutDirectoryFlag_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\emptydir");
            var console = new TestConsole();
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\emptydir";

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(@"C:\storage\emptydir").Should().BeTrue("empty dir should not be deleted without --directory");
            console.Output.Should().Contain("--directory", "error should mention --directory flag");
        }

        // T057 DF-011: Single file deleted
        [TestMethod]
        public async Task Execute_SingleFile_Deleted()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "content");
            var console = new TestConsole();
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\file.txt").Should().BeFalse("file should be deleted after rm");
            console.Output.Should().Contain("Removed", "should display removal confirmation");
        }

        // T058 DF-012: Empty directory deleted with -d
        [TestMethod]
        public async Task Execute_EmptyDir_WithDirectoryFlag_Deleted()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\emptydir2");
            var console = new TestConsole();
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\emptydir2";
            cmd.Directory = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(@"C:\storage\emptydir2").Should().BeFalse("empty directory should be deleted with --directory flag");
            console.Output.Should().Contain("Removed", "should display removal confirmation");
        }

        // T059 DF-013: Non-existent path with --force produces no error
        [TestMethod]
        public async Task Execute_NonExistentPath_WithForce_NoError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            var console = new TestConsole();
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\nonexistent.txt";
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().NotContain("error", "no error should be shown with --force on missing path");
            console.Output.Should().NotContain("Error", "no error should be shown with --force on missing path");
            console.Output.Should().NotContain("not found", "no error should be shown with --force on missing path");
        }

        // T060 DF-014: Non-existent path without --force produces error
        [TestMethod]
        public async Task Execute_NonExistentPath_WithoutForce_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            var console = new TestConsole();
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\nonexistent.txt";
            cmd.Force = false;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("not found", "error should indicate path not found");
        }

        // T061 DF-015: Non-empty directory deleted recursively
        [TestMethod]
        public async Task Execute_NonEmptyDir_WithRecursive_DeletesAll()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\mydir\sub");
            fs.File.WriteAllText(@"C:\storage\mydir\file1.txt", "content1");
            fs.File.WriteAllText(@"C:\storage\mydir\sub\file2.txt", "content2");
            var console = new TestConsole();
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\mydir";
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(@"C:\storage\mydir").Should().BeFalse("directory should be deleted with --recursive");
            fs.File.Exists(@"C:\storage\mydir\file1.txt").Should().BeFalse("nested file should be deleted");
            fs.File.Exists(@"C:\storage\mydir\sub\file2.txt").Should().BeFalse("deeply nested file should be deleted");
            console.Output.Should().Contain("Removed", "should confirm removal");
        }

        // T062 DF-016: Glob pattern matches and deletes multiple files
        [TestMethod]
        public async Task Execute_GlobPattern_DeletesMatchingFiles()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\a.log", "log1");
            fs.File.WriteAllText(@"C:\storage\b.log", "log2");
            fs.File.WriteAllText(@"C:\storage\c.txt", "text");
            var console = new TestConsole();
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\*.log";
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\a.log").Should().BeFalse("a.log should be deleted by glob *.log");
            fs.File.Exists(@"C:\storage\b.log").Should().BeFalse("b.log should be deleted by glob *.log");
            fs.File.Exists(@"C:\storage\c.txt").Should().BeTrue("c.txt should not be deleted by *.log pattern");
        }

        // T063 DF-017: Glob with fewer than threshold — no prompt
        [TestMethod]
        public async Task Execute_GlobBelowThreshold_NoPrompt_DeletesFiles()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\a.log", "1");
            fs.File.WriteAllText(@"C:\storage\b.log", "2");
            fs.File.WriteAllText(@"C:\storage\c.log", "3");
            // 3 matches < ConfirmationThreshold (4) — no prompt expected
            var console = new TestConsole();
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\*.log";
            cmd.Force = false; // explicitly not forcing

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\a.log").Should().BeFalse("should delete without prompting when below threshold");
            fs.File.Exists(@"C:\storage\b.log").Should().BeFalse("should delete without prompting when below threshold");
            fs.File.Exists(@"C:\storage\c.log").Should().BeFalse("should delete without prompting when below threshold");
            console.Output.Should().NotContain("Delete", "should not prompt when matches below threshold");
        }

        // T064 DF-018: Glob with ≥ threshold — prompts (answered yes)
        [TestMethod]
        public async Task Execute_GlobAboveThreshold_ConfirmYes_DeletesAll()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\a.log", "1");
            fs.File.WriteAllText(@"C:\storage\b.log", "2");
            fs.File.WriteAllText(@"C:\storage\c.log", "3");
            fs.File.WriteAllText(@"C:\storage\d.log", "4");
            fs.File.WriteAllText(@"C:\storage\e.log", "5");
            // 5 matches >= ConfirmationThreshold (4) — prompt expected
            var console = new TestConsole();
            console.Input.PushTextWithEnter("y");
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\*.log";

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\a.log").Should().BeFalse("all files should be deleted when user confirms");
            fs.File.Exists(@"C:\storage\b.log").Should().BeFalse("all files should be deleted when user confirms");
            fs.File.Exists(@"C:\storage\c.log").Should().BeFalse("all files should be deleted when user confirms");
            fs.File.Exists(@"C:\storage\d.log").Should().BeFalse("all files should be deleted when user confirms");
            fs.File.Exists(@"C:\storage\e.log").Should().BeFalse("all files should be deleted when user confirms");
            console.Output.Should().Contain("Delete 5 files?", "should prompt user for confirmation");
        }

        // T065 DF-019: Glob with ≥ threshold — prompts (answered no)
        [TestMethod]
        public async Task Execute_GlobAboveThreshold_ConfirmNo_KeepsAll()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\a.log", "1");
            fs.File.WriteAllText(@"C:\storage\b.log", "2");
            fs.File.WriteAllText(@"C:\storage\c.log", "3");
            fs.File.WriteAllText(@"C:\storage\d.log", "4");
            fs.File.WriteAllText(@"C:\storage\e.log", "5");
            // 5 matches >= ConfirmationThreshold (4) — prompt expected, user declines
            var console = new TestConsole();
            console.Input.PushTextWithEnter("n");
            var cmd = new RmCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\*.log";

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\a.log").Should().BeTrue("no files should be deleted when user declines");
            fs.File.Exists(@"C:\storage\b.log").Should().BeTrue("no files should be deleted when user declines");
            fs.File.Exists(@"C:\storage\c.log").Should().BeTrue("no files should be deleted when user declines");
            fs.File.Exists(@"C:\storage\d.log").Should().BeTrue("no files should be deleted when user declines");
            fs.File.Exists(@"C:\storage\e.log").Should().BeTrue("no files should be deleted when user declines");
        }

        // T066 DF-020 + T072 EH-008: Cannot delete storage root
        [TestMethod]
        public async Task Execute_StorageRoot_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "content");
            var options = new FileTransferOptions { StorageRootPath = @"C:\storage" };
            var console = new TestConsole();
            var cmd = new RmCommand(fs, options);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage";
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(@"C:\storage").Should().BeTrue("storage root must not be deleted");
            console.Output.Should().Contain("storage root", "error should mention storage root");
        }
    }
}
