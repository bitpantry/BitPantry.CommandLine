using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Server.Commands;
using FluentAssertions;
using Spectre.Console.Testing;
using System.IO.Abstractions.TestingHelpers;
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
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\a.txt", "content");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\a.txt";
            cmd.Destination = @"C:\storage\b.txt";

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\b.txt").Should().BeTrue("destination should exist after move");
            fs.File.Exists(@"C:\storage\a.txt").Should().BeFalse("source should be gone after move");
            fs.File.ReadAllText(@"C:\storage\b.txt").Should().Be("content");
        }

        // T082 DF-022: Directory moved
        [TestMethod]
        public async Task Execute_Directory_MovedToNewLocation()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\src");
            fs.File.WriteAllText(@"C:\storage\src\file.txt", "data");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\src";
            cmd.Destination = @"C:\storage\dst";

            await cmd.Execute(new CommandExecutionContext());

            fs.Directory.Exists(@"C:\storage\dst").Should().BeTrue("destination directory should exist");
            fs.Directory.Exists(@"C:\storage\src").Should().BeFalse("source directory should be gone");
        }

        // T083 DF-023: Fails if source not found
        [TestMethod]
        public async Task Execute_SourceNotFound_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\nonexistent.txt";
            cmd.Destination = @"C:\storage\dest.txt";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("not found", "error should indicate source not found");
        }

        // T084 DF-024: Fails if destination exists without --force
        [TestMethod]
        public async Task Execute_DestinationExists_WithoutForce_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\src.txt", "source");
            fs.File.WriteAllText(@"C:\storage\dest.txt", "existing");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\src.txt";
            cmd.Destination = @"C:\storage\dest.txt";
            cmd.Force = false;

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\src.txt").Should().BeTrue("source should remain when move fails");
            fs.File.ReadAllText(@"C:\storage\dest.txt").Should().Be("existing", "destination should keep original content");
            console.Output.Should().Contain("already exists", "error should indicate destination exists");
        }

        // T079 CV-020: --force allows overwrite of existing destination
        [TestMethod]
        public async Task Execute_DestinationExists_WithForce_Overwrites()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\src.txt", "new content");
            fs.File.WriteAllText(@"C:\storage\dest.txt", "old content");
            var console = new TestConsole();
            var cmd = new MvCommand(fs);
            cmd.SetConsole(console);
            cmd.Source = @"C:\storage\src.txt";
            cmd.Destination = @"C:\storage\dest.txt";
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            fs.File.Exists(@"C:\storage\dest.txt").Should().BeTrue("destination should exist");
            fs.File.Exists(@"C:\storage\src.txt").Should().BeFalse("source should be gone");
            fs.File.ReadAllText(@"C:\storage\dest.txt").Should().Be("new content", "destination should have new content");
        }
    }
}
