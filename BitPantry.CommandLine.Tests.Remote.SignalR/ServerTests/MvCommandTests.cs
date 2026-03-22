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
    public class MvCommandTests
    {
        private static readonly Type MvCommandType = typeof(MvCommand);

        // T077 CV-018 + T078 CV-019 + T079 CV-020: Consolidated attribute tests
        [TestMethod]
        public void MvCommand_SourceArgument_IsRequired()
        {
            var prop = MvCommandType.GetProperty("Source");
            prop.Should().NotBeNull("MvCommand must have a Source property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Source must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeTrue("Source argument must be required for mv");
        }

        [TestMethod]
        public void MvCommand_DestinationArgument_IsRequired()
        {
            var prop = MvCommandType.GetProperty("Destination");
            prop.Should().NotBeNull("MvCommand must have a Destination property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Destination must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeTrue("Destination argument must be required for mv");
        }

        [TestMethod]
        public void MvCommand_HasForceFlag()
        {
            var prop = MvCommandType.GetProperty("Force");
            prop.Should().NotBeNull("MvCommand must have a Force property");
            var flagAttr = prop!.GetCustomAttribute<FlagAttribute>();
            flagAttr.Should().NotBeNull("Force must have [Flag] attribute");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Force must have [Argument] attribute");
            argAttr!.Name.Should().Be("force");
            var aliasAttr = prop.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("Force must have [Alias] for -f");
            aliasAttr!.Alias.Should().Be('f');
        }

        // T081 DF-021: File moved to new location
        [TestMethod]
        public async Task Execute_File_MovedToNewLocation()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "a.txt"), "content");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "a.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "b.txt");

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "b.txt")).Should().BeTrue("destination should exist after move");
            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "a.txt")).Should().BeFalse("source should be gone after move");
            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "b.txt")).Should().Be("content");
        }

        // T082 DF-022: Directory moved
        [TestMethod]
        public async Task Execute_Directory_MovedToNewLocation()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(Path.Combine(TestPaths.StorageRoot, "src"));
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src", "file.txt"), "data");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "src");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dst");

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "dst")).Should().BeTrue("destination directory should exist");
            fs.Directory.Exists(Path.Combine(TestPaths.StorageRoot, "src")).Should().BeFalse("source directory should be gone");
        }

        // T083 DF-023: Fails if source not found
        [TestMethod]
        public async Task Execute_SourceNotFound_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "nonexistent.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dest.txt");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("not found", "error should indicate source not found");
        }

        // T084 DF-024: Fails if destination exists without --force
        [TestMethod]
        public async Task Execute_DestinationExists_WithoutForce_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src.txt"), "source");
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "dest.txt"), "existing");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "src.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dest.txt");
            cmd.Force = false;

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "src.txt")).Should().BeTrue("source should remain when move fails");
            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "dest.txt")).Should().Be("existing", "destination should keep original content");
            console.Output.Should().Contain("already exists", "error should indicate destination exists");
        }

        // T086 DF-026 + T090 EH-011: Fails if source same as destination
        [TestMethod]
        public async Task Execute_SourceEqualsDestination_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "a.txt"), "content");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "a.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "a.txt");

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "a.txt")).Should().BeTrue("file should still exist");
            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "a.txt")).Should().Be("content", "file content should be unchanged");
            console.Output.Should().Contain("same", "error should indicate source and destination are the same");
        }

        // T079 CV-020: --force allows overwrite of existing destination
        [TestMethod]
        public async Task Execute_DestinationExists_WithForce_Overwrites()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "src.txt"), "new content");
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "dest.txt"), "old content");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "src.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dest.txt");
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "dest.txt")).Should().BeTrue("destination should exist");
            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "src.txt")).Should().BeFalse("source should be gone");
            fs.File.ReadAllText(Path.Combine(TestPaths.StorageRoot, "dest.txt")).Should().Be("new content", "destination should have new content");
        }

        // T091 EH-024: Path traversal in source
        [TestMethod]
        public async Task Execute_PathTraversalInSource_ProducesAccessDeniedError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "legit.txt"), "ok");
            var pathValidator = new PathValidator(TestPaths.StorageRoot);
            IFileSystem sandboxedFs = new SandboxedFileSystem(fs, pathValidator);
            var console = new TestConsole();
            var cmd = new MvCommand(sandboxedFs);
            cmd.SetConsole(console);
            cmd.Source = @"../../etc/passwd";
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "dst.txt");

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(Path.Combine(TestPaths.StorageRoot, "legit.txt")).Should().BeTrue("files inside sandbox must not be affected");
            console.Output.Should().Contain("Access denied", "should display access denied for path traversal");
        }

        // T092 UX-016: Success shows source and destination
        [TestMethod]
        public async Task Execute_Success_OutputShowsSourceAndDestination()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(TestPaths.StorageRoot);
            fs.File.WriteAllText(Path.Combine(TestPaths.StorageRoot, "a.txt"), "content");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = Path.Combine(TestPaths.StorageRoot, "a.txt");
            cmd.Destination = Path.Combine(TestPaths.StorageRoot, "b.txt");

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("Moved:", "output should contain the Moved verb");
            console.Output.Should().Contain(Path.Combine(TestPaths.StorageRoot, "a.txt"), "output should show source path");
            console.Output.Should().Contain(Path.Combine(TestPaths.StorageRoot, "b.txt"), "output should show destination path");
        }
    }
}
