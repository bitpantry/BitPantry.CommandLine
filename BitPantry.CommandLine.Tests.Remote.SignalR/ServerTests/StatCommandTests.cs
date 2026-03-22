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
    public class StatCommandTests
    {
        private static readonly Type StatCommandType = typeof(StatCommand);

        // T141 CV-032: path argument is required
        [TestMethod]
        public void StatCommand_PathArgument_IsRequired()
        {
            var prop = StatCommandType.GetProperty("Path");
            prop.Should().NotBeNull("StatCommand must have a Path property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Path must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeTrue("Path argument must be required for stat");
        }

        // T142 DF-043: Returns correct name and path for file
        [TestMethod]
        public async Task Execute_File_ShowsNameAndPath()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "reports"));
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "reports", "q1.txt"), "quarterly report");
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "reports", "q1.txt");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("q1.txt", "output should contain file name");
            console.Output.Should().Contain(Path.Combine(TestPaths.StorageRoot, "reports", "q1.txt"), "output should contain full path");
        }

        // T143 DF-044: Returns correct size for file
        [TestMethod]
        public async Task Execute_File_ShowsCorrectSize()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var content = new byte[512];
            fs.File.WriteAllBytes(Path.Combine(TestPaths.StorageRoot, "data.bin"), content);
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "data.bin");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("512", "output should show file size in bytes");
        }

        // T144 DF-045: Returns created and modified timestamps
        [TestMethod]
        public async Task Execute_File_ShowsTimestamps()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "file.txt"), "content");
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "file.txt");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("Created:", "output should show created timestamp");
            console.Output.Should().Contain("Last Modified:", "output should show last modified timestamp");
        }

        // T145 DF-046: Returns correct file count for directory
        // Also covers T152 UX-025: Directory shows ItemCount, FileCount, DirectoryCount
        [TestMethod]
        public async Task Execute_Directory_ShowsCorrectCounts()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "project"));
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "project", "subdir"));
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "project", "a.txt"), "aaa");
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "project", "b.txt"), "bbb");
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "project", "subdir", "c.txt"), "ccc");
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "project");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("FileCount:", "output should show file count label");
            console.Output.Should().Contain("3", "output should show 3 files");
            console.Output.Should().Contain("DirectoryCount:", "output should show directory count label");
            console.Output.Should().Contain("1", "output should show 1 subdirectory");
            console.Output.Should().Contain("ItemCount:", "output should show item count label");
            console.Output.Should().Contain("4", "output should show total of 4 items");
        }

        // T146 DF-047: Directory total size is recursive sum
        [TestMethod]
        public async Task Execute_Directory_ShowsRecursiveTotalSize()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "project"));
            fs.File.WriteAllBytes(Path.Combine(TestPaths.StorageRoot, "project", "a.bin"), new byte[100]);
            fs.File.WriteAllBytes(Path.Combine(TestPaths.StorageRoot, "project", "b.bin"), new byte[100]);
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "project");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("Size:", "directory stat should show total size");
            console.Output.Should().Contain("200", "directory size should be recursive sum of all files (100+100=200)");
        }

        // T148 EH-020: Path not found
        [TestMethod]
        public async Task Execute_NonexistentPath_ShowsError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "nosuchpath");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("not found", "should display path not found error");
        }

        // T149 EH-027: Path traversal attempt
        [TestMethod]
        public async Task Execute_PathTraversal_ProducesAccessDeniedError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var pathValidator = new PathValidator(TestPaths.StorageRoot);
            IFileSystem sandboxedFs = new SandboxedFileSystem(fs, pathValidator);
            var console = new TestConsole();
            var cmd = new StatCommand(sandboxedFs);
            cmd.SetConsole(console);
            cmd.Path = @"../../etc/";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("Access denied", "should display access denied for path traversal");
        }

        // T150 UX-023: All fields rendered for a file
        [TestMethod]
        public async Task Execute_File_ShowsAllExpectedFields()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "reports"));
            fs.File.WriteAllBytes(Path.Combine(TestPaths.StorageRoot, "reports", "report.txt"), new byte[512]);
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "reports", "report.txt");

            await cmd.Execute(new CommandExecutionContext());

            // Verify all 6 expected fields for a file
            console.Output.Should().Contain("Name:", "output should contain Name field");
            console.Output.Should().Contain("Type:", "output should contain Type field");
            console.Output.Should().Contain("Path:", "output should contain Path field");
            console.Output.Should().Contain("Size:", "output should contain Size field");
            console.Output.Should().Contain("Created:", "output should contain Created field");
            console.Output.Should().Contain("Last Modified:", "output should contain Last Modified field");
        }

        // T151 UX-024: Size shown in human-readable and raw bytes
        [TestMethod]
        public async Task Execute_File_ShowsSizeInHumanReadableAndRawBytes()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllBytes(Path.Combine(TestPaths.StorageRoot, "data.bin"), new byte[1024]);
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = Path.Combine(TestPaths.StorageRoot, "data.bin");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("1.0 KB", "output should show human-readable size");
            console.Output.Should().Contain("1,024 bytes", "output should show raw byte count in parentheses");
        }
    }
}
