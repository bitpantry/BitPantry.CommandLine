using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Server.Commands;
using FluentAssertions;
using Spectre.Console.Testing;
using System.IO.Abstractions.TestingHelpers;
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
            fs.Directory.CreateDirectory(@"C:\storage\reports");
            fs.File.WriteAllText(@"C:\storage\reports\q1.txt", "quarterly report");
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\reports\q1.txt";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("q1.txt", "output should contain file name");
            console.Output.Should().Contain(@"C:\storage\reports\q1.txt", "output should contain full path");
        }

        // T143 DF-044: Returns correct size for file
        [TestMethod]
        public async Task Execute_File_ShowsCorrectSize()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            var content = new byte[512];
            fs.File.WriteAllBytes(@"C:\storage\data.bin", content);
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\data.bin";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("512", "output should show file size in bytes");
        }

        // T144 DF-045: Returns created and modified timestamps
        [TestMethod]
        public async Task Execute_File_ShowsTimestamps()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "content");
            var console = new TestConsole();
            var cmd = new StatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("Created:", "output should show created timestamp");
            console.Output.Should().Contain("Last Modified:", "output should show last modified timestamp");
        }
    }
}
