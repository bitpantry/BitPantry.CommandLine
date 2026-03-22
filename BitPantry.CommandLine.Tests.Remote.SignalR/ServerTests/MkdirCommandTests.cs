using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Server.Commands;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using Spectre.Console.Testing;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using BitPantry.CommandLine.Tests.Infrastructure;
using System.Reflection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class MkdirCommandTests
    {
        private static readonly Type MkdirCommandType = typeof(MkdirCommand);

        // T038 CV-010: path argument is required
        [TestMethod]
        public void MkdirCommand_PathArgument_IsRequired()
        {
            var prop = MkdirCommandType.GetProperty("Path");
            prop.Should().NotBeNull("MkdirCommand must have a Path property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Path must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeTrue("Path argument must be required for mkdir");
        }

        // T039 CV-011: --parents flag activates deep creation
        // Note: -p alias cannot be used — 'p' is reserved by the framework for --profile global argument
        [TestMethod]
        public void MkdirCommand_HasParentsFlag()
        {
            var prop = MkdirCommandType.GetProperty("Parents");
            prop.Should().NotBeNull("MkdirCommand must have a Parents property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("Parents must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Parents must have [Argument] attribute");
            argAttr!.Name.Should().Be("parents");
        }

        // T041 DF-007: Directory created at path
        [TestMethod]
        public async Task Execute_WithPath_CreatesDirectory()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var console = new TestConsole();
            var cmd = new MkdirCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "reports");

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "reports")).Should().BeTrue("directory should be created at the specified path");
        }

        // T042 DF-008: All intermediate dirs created with --parents
        [TestMethod]
        public async Task Execute_WithParents_CreatesAllIntermediateDirs()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var console = new TestConsole();
            var cmd = new MkdirCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "a", "b", "c");
            cmd.Parents = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "a")).Should().BeTrue("intermediate dir /a should exist");
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "a", "b")).Should().BeTrue("intermediate dir /a/b should exist");
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "a", "b", "c")).Should().BeTrue("target dir /a/b/c should exist");
        }

        // T043 DF-009: Fails if parent missing without --parents
        // T046 EH-003: Parent does not exist — error message
        [TestMethod]
        public async Task Execute_ParentMissing_WithoutParentsFlag_FailsWithError()
        {
            var fs = new MockFileSystem();
            // Only root exists, no /a parent
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var console = new TestConsole();
            var cmd = new MkdirCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "a", "b");
            cmd.Parents = false;

            await cmd.Execute(new CommandExecutionContext());

            // DF-009: Directory NOT created
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "a", "b")).Should().BeFalse("directory should not be created when parent missing without --parents");

            // EH-003: Error message displayed
            console.Output.Should().Contain("does not exist", "should display parent-missing error message");
        }

        // T044 DF-010: Idempotent when directory already exists
        [TestMethod]
        public async Task Execute_DirectoryAlreadyExists_NoError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "reports"));
            var console = new TestConsole();
            var cmd = new MkdirCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "reports");

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "reports")).Should().BeTrue("directory should still exist");
            console.Output.Should().NotContain("error", "should not display error for existing directory");
            console.Output.Should().NotContain("Error", "should not display error for existing directory");
        }

        // T047 EH-022: Path traversal attempt blocked
        [TestMethod]
        public async Task Execute_PathTraversal_BlockedByError()
        {
            var innerFs = new MockFileSystem();
            innerFs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var pathValidator = new PathValidator(TestPaths.StorageRoot);
            IFileSystem sandboxedFs = new SandboxedFileSystem(innerFs, pathValidator);

            var console = new TestConsole();
            var cmd = new MkdirCommand(sandboxedFs);
            cmd.SetConsole(console);
            cmd.Path = "../../tmp/evil";

            await cmd.Execute(new CommandExecutionContext());

            innerFs.Directory.Exists(Path.Combine(TestPaths.FileSystemRoot, "tmp", "evil")).Should().BeFalse("traversal path should not be created");
            console.Output.Should().Contain("denied", "should show access denied for path traversal");
        }

        // T048 UX-013: Success message includes path
        [TestMethod]
        public async Task Execute_Success_MessageIncludesPath()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var console = new TestConsole();
            var cmd = new MkdirCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "reports");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("Created:", "should show success message");
            console.Output.Should().Contain(Path.Combine(TestPaths.StorageRoot, "reports"), "success message should include the path");
        }
    }
}
