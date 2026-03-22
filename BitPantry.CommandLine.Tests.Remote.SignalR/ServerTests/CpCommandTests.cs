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
    public class CpCommandTests
    {
        private static readonly Type CpCommandType = typeof(CpCommand);

        // T093 CV-021: source argument is required
        [TestMethod]
        public void CpCommand_SourceArgument_IsRequired()
        {
            var prop = CpCommandType.GetProperty("Source");
            prop.Should().NotBeNull("CpCommand must have a Source property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Source must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeTrue("Source argument must be required for cp");
        }

        // T094 CV-022: destination argument is required
        [TestMethod]
        public void CpCommand_DestinationArgument_IsRequired()
        {
            var prop = CpCommandType.GetProperty("Destination");
            prop.Should().NotBeNull("CpCommand must have a Destination property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Destination must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeTrue("Destination argument must be required for cp");
        }

        // T095 CV-023: --recursive required for directory copy
        [TestMethod]
        public async Task Execute_DirectoryWithoutRecursive_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "srcdir"));
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "srcdir", "file.txt"), "data");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "srcdir");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dstdir");
            cmd.Recursive = false;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "dstdir")).Should().BeFalse("destination should not be created without --recursive");
            console.Output.Should().Contain("recursive", "error should mention --recursive flag");
        }

        // T096 CV-024: --recursive accepted for directory copy
        [TestMethod]
        public async Task Execute_DirectoryWithRecursive_CopiesSuccessfully()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "srcdir"));
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "srcdir", "file.txt"), "data");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "srcdir");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dstdir");
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "dstdir")).Should().BeTrue("destination directory should exist after recursive copy");
            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "dstdir", "file.txt")).Should().BeTrue("files should be copied recursively");
            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "dstdir", "file.txt")).Should().Be("data");
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "srcdir")).Should().BeTrue("source should still exist (copy, not move)");
            console.Output.Should().Contain("Copied", "output should confirm copy");
        }

        // T097 CV-025: --force / -f flag allows overwrite
        [TestMethod]
        public void CpCommand_HasForceFlag()
        {
            var prop = CpCommandType.GetProperty("Force");
            prop.Should().NotBeNull("CpCommand must have a Force property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("Force must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Force must have [Argument] attribute");
            argAttr!.Name.Should().Be("force");
            var aliasAttr = prop.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("Force must have [Alias] for -f");
            aliasAttr!.Alias.Should().Be('f');
        }

        // T099 DF-027: File copied, original preserved
        [TestMethod]
        public async Task Execute_FileCopy_BothSourceAndDestExist()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "a.txt"), "original content");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "a.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "b.txt");

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "a.txt")).Should().BeTrue("source must still exist after copy");
            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "b.txt")).Should().BeTrue("destination must exist after copy");
            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "a.txt")).Should().Be("original content", "source content must be unchanged");
            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "b.txt")).Should().Be("original content", "destination must have same content as source");
        }

        // T100 DF-028 + T101 DF-029: Directory and contents copied with nested structure preserved
        [TestMethod]
        public async Task Execute_DirectoryRecursive_CopiesNestedStructure()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "src", "sub"));
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src", "top.txt"), "top");
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src", "sub", "nested.txt"), "nested");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "src");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dst");
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            // DF-028: Directory and contents copied
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "dst")).Should().BeTrue("destination directory should exist");
            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "dst", "top.txt")).Should().BeTrue("top-level file should be copied");
            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "dst", "top.txt")).Should().Be("top");
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "src")).Should().BeTrue("source should still exist (copy, not move)");
            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "src", "top.txt")).Should().BeTrue("source files should still exist");

            // DF-029: Nested directory structure preserved 
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "dst", "sub")).Should().BeTrue("nested subdirectory should be copied");
            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "dst", "sub", "nested.txt")).Should().BeTrue("nested file should be copied");
            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "dst", "sub", "nested.txt")).Should().Be("nested");
        }

        // T102 DF-030: Source directory without --recursive fails (no dest created)
        [TestMethod]
        public async Task Execute_SourceDirWithoutRecursive_DestinationNotCreated()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "mydir"));
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "mydir", "file.txt"), "data");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "mydir");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "mydir-copy");
            cmd.Recursive = false;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "mydir-copy")).Should().BeFalse("destination directory must not be created without --recursive");
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "mydir")).Should().BeTrue("source directory must still exist");
        }

        // T104 DF-032: Overwrites existing destination with --force
        [TestMethod]
        public async Task Execute_DestinationExists_WithForce_OverwritesFile()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src.txt"), "new content");
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "dest.txt"), "old content");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "src.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dest.txt");
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "dest.txt")).Should().Be("new content", "destination should be overwritten with --force");
            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "src.txt")).Should().BeTrue("source should still exist after copy");
            console.Output.Should().Contain("Copied:", "output should confirm copy");
        }

        // T103 DF-031 + T108 EH-014: Fails if dest file exists without --force
        [TestMethod]
        public async Task Execute_DestinationExists_WithoutForce_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src.txt"), "new");
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "dest.txt"), "existing");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "src.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dest.txt");
            cmd.Force = false;

            await cmd.Execute(new CommandExecutionContext());

            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "dest.txt")).Should().Be("existing", "destination content must not be overwritten");
            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "src.txt")).Should().BeTrue("source should still exist");
            console.Output.Should().Contain("already exists", "error should indicate destination exists");
        }

        // T106 EH-012: Source not found
        [TestMethod]
        public async Task Execute_SourceNotFound_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "nosuchfile.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dest.txt");

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "dest.txt")).Should().BeFalse("destination should not be created");
            console.Output.Should().Contain("not found", "error should mention source not found");
        }

        // T107 EH-013: Source is directory without --recursive
        [TestMethod]
        public async Task Execute_SourceIsDirectory_WithoutRecursive_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "mydir"));
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "mydir", "file.txt"), "data");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "mydir");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "mydir-copy");
            cmd.Recursive = false;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("recursive", "error should mention --recursive flag");
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "mydir-copy")).Should().BeFalse("destination should not be created");
        }

        // T109 EH-025: Path traversal in destination
        [TestMethod]
        public async Task Execute_PathTraversalInDestination_ProducesAccessDeniedError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src.txt"), "data");
            var pathValidator = new PathValidator(TestPaths.StorageRoot);
            IFileSystem sandboxedFs = new SandboxedFileSystem(fs, pathValidator);
            var console = new TestConsole();
            var cmd = new CpCommand(sandboxedFs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "src.txt");
            cmd.Destination = @"../../evil/stolen.txt";

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "src.txt")).Should().BeTrue("source must not be affected");
            console.Output.Should().Contain("Access denied", "should display access denied for path traversal");
        }

        // T110 UX-017: Success shows source and destination
        [TestMethod]
        public async Task Execute_Success_OutputShowsSourceAndDestination()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "a.txt"), "content");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "a.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "b.txt");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain(Path.Combine(TestPaths.StorageRoot, "a.txt"), "output should show source path");
            console.Output.Should().Contain(Path.Combine(TestPaths.StorageRoot, "b.txt"), "output should show destination path");
            console.Output.Should().Contain("Copied:", "output should contain success prefix");
        }

        // T111 UX-018: Recursive copy summary shows item count
        [TestMethod]
        public async Task Execute_DirectoryRecursive_SummaryShowsItemCount()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "src", "sub"));
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src", "a.txt"), "a");
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src", "b.txt"), "b");
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src", "sub", "c.txt"), "c");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "src");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dst");
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("Copied 3 items", "output should show count of files copied");
        }
    }
}
