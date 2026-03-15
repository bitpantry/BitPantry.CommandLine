using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Server.Commands;
using FluentAssertions;
using Spectre.Console.Testing;
using System.IO.Abstractions.TestingHelpers;
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
            fs.Directory.CreateDirectory(@"C:\storage\srcdir");
            fs.File.WriteAllText(@"C:\storage\srcdir\file.txt", "data");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\srcdir";
            cmd.Destination = @"C:\storage\dstdir";
            cmd.Recursive = false;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(@"C:\storage\dstdir").Should().BeFalse("destination should not be created without --recursive");
            console.Output.Should().Contain("recursive", "error should mention --recursive flag");
        }

        // T096 CV-024: --recursive accepted for directory copy
        [TestMethod]
        public async Task Execute_DirectoryWithRecursive_CopiesSuccessfully()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\srcdir");
            fs.File.WriteAllText(@"C:\storage\srcdir\file.txt", "data");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\srcdir";
            cmd.Destination = @"C:\storage\dstdir";
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(@"C:\storage\dstdir").Should().BeTrue("destination directory should exist after recursive copy");
            fs.File.Exists(@"C:\storage\dstdir\file.txt").Should().BeTrue("files should be copied recursively");
            fs.File.ReadAllText(@"C:\storage\dstdir\file.txt").Should().Be("data");
            fs.Directory.Exists(@"C:\storage\srcdir").Should().BeTrue("source should still exist (copy, not move)");
            console.Output.Should().Contain("Copied:", "output should confirm copy");
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
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\a.txt", "original content");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\a.txt";
            cmd.Destination = @"C:\storage\b.txt";

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\a.txt").Should().BeTrue("source must still exist after copy");
            fs.File.Exists(@"C:\storage\b.txt").Should().BeTrue("destination must exist after copy");
            fs.File.ReadAllText(@"C:\storage\a.txt").Should().Be("original content", "source content must be unchanged");
            fs.File.ReadAllText(@"C:\storage\b.txt").Should().Be("original content", "destination must have same content as source");
        }

        // T100 DF-028 + T101 DF-029: Directory and contents copied with nested structure preserved
        [TestMethod]
        public async Task Execute_DirectoryRecursive_CopiesNestedStructure()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\src\sub");
            fs.File.WriteAllText(@"C:\storage\src\top.txt", "top");
            fs.File.WriteAllText(@"C:\storage\src\sub\nested.txt", "nested");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\src";
            cmd.Destination = @"C:\storage\dst";
            cmd.Recursive = true;

            await cmd.Execute(new CommandExecutionContext());

            // DF-028: Directory and contents copied
            fs.Directory.Exists(@"C:\storage\dst").Should().BeTrue("destination directory should exist");
            fs.File.Exists(@"C:\storage\dst\top.txt").Should().BeTrue("top-level file should be copied");
            fs.File.ReadAllText(@"C:\storage\dst\top.txt").Should().Be("top");
            fs.Directory.Exists(@"C:\storage\src").Should().BeTrue("source should still exist (copy, not move)");
            fs.File.Exists(@"C:\storage\src\top.txt").Should().BeTrue("source files should still exist");

            // DF-029: Nested directory structure preserved 
            fs.Directory.Exists(@"C:\storage\dst\sub").Should().BeTrue("nested subdirectory should be copied");
            fs.File.Exists(@"C:\storage\dst\sub\nested.txt").Should().BeTrue("nested file should be copied");
            fs.File.ReadAllText(@"C:\storage\dst\sub\nested.txt").Should().Be("nested");
        }

        // T102 DF-030: Source directory without --recursive fails (no dest created)
        [TestMethod]
        public async Task Execute_SourceDirWithoutRecursive_DestinationNotCreated()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\mydir");
            fs.File.WriteAllText(@"C:\storage\mydir\file.txt", "data");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\mydir";
            cmd.Destination = @"C:\storage\mydir-copy";
            cmd.Recursive = false;

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(@"C:\storage\mydir-copy").Should().BeFalse("destination directory must not be created without --recursive");
            fs.Directory.Exists(@"C:\storage\mydir").Should().BeTrue("source directory must still exist");
        }

        // T104 DF-032: Overwrites existing destination with --force
        [TestMethod]
        public async Task Execute_DestinationExists_WithForce_OverwritesFile()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\src.txt", "new content");
            fs.File.WriteAllText(@"C:\storage\dest.txt", "old content");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\src.txt";
            cmd.Destination = @"C:\storage\dest.txt";
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.File.ReadAllText(@"C:\storage\dest.txt").Should().Be("new content", "destination should be overwritten with --force");
            fs.File.Exists(@"C:\storage\src.txt").Should().BeTrue("source should still exist after copy");
            console.Output.Should().Contain("Copied:", "output should confirm copy");
        }

        // T103 DF-031 + T108 EH-014: Fails if dest file exists without --force
        [TestMethod]
        public async Task Execute_DestinationExists_WithoutForce_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\src.txt", "new");
            fs.File.WriteAllText(@"C:\storage\dest.txt", "existing");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\src.txt";
            cmd.Destination = @"C:\storage\dest.txt";
            cmd.Force = false;

            await cmd.Execute(new CommandExecutionContext());

            fs.File.ReadAllText(@"C:\storage\dest.txt").Should().Be("existing", "destination content must not be overwritten");
            fs.File.Exists(@"C:\storage\src.txt").Should().BeTrue("source should still exist");
            console.Output.Should().Contain("already exists", "error should indicate destination exists");
        }

        // T106 EH-012: Source not found
        [TestMethod]
        public async Task Execute_SourceNotFound_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\nosuchfile.txt";
            cmd.Destination = @"C:\storage\dest.txt";

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\dest.txt").Should().BeFalse("destination should not be created");
            console.Output.Should().Contain("not found", "error should mention source not found");
        }

        // T107 EH-013: Source is directory without --recursive
        [TestMethod]
        public async Task Execute_SourceIsDirectory_WithoutRecursive_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\mydir");
            fs.File.WriteAllText(@"C:\storage\mydir\file.txt", "data");
            var console = new TestConsole();
            var cmd = new CpCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\mydir";
            cmd.Destination = @"C:\storage\mydir-copy";
            cmd.Recursive = false;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("recursive", "error should mention --recursive flag");
            fs.Directory.Exists(@"C:\storage\mydir-copy").Should().BeFalse("destination should not be created");
        }
    }
}
