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
    }
}
