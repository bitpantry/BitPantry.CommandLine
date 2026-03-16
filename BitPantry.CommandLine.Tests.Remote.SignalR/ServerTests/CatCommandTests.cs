using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Server.Commands;
using FluentAssertions;
using Spectre.Console.Testing;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    [TestClass]
    public class CatCommandTests
    {
        private static readonly Type CatCommandType = typeof(CatCommand);

        // T112 CV-026: path argument is required
        [TestMethod]
        public void CatCommand_PathArgument_IsRequired()
        {
            var prop = CatCommandType.GetProperty("Path");
            prop.Should().NotBeNull("CatCommand must have a Path property");
            var argAttr = prop!.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Path must have [Argument] attribute");
            argAttr!.IsRequired.Should().BeTrue("Path argument must be required for cat");
        }

        // T113 CV-027: --lines / -n accepts integer
        [TestMethod]
        public void CatCommand_LinesArgument_AcceptsInteger()
        {
            var prop = CatCommandType.GetProperty("Lines");
            prop.Should().NotBeNull("CatCommand must have a Lines property");
            prop!.PropertyType.Should().Be(typeof(int?), "Lines should be nullable int");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Lines must have [Argument] attribute");
            argAttr!.Name.Should().Be("lines");
            var aliasAttr = prop.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("Lines must have [Alias] for -n");
            aliasAttr!.Alias.Should().Be('n');
        }

        // T114 CV-028: --tail / -t accepts integer
        [TestMethod]
        public void CatCommand_TailArgument_AcceptsInteger()
        {
            var prop = CatCommandType.GetProperty("Tail");
            prop.Should().NotBeNull("CatCommand must have a Tail property");
            prop!.PropertyType.Should().Be(typeof(int?), "Tail should be nullable int");
            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            argAttr.Should().NotBeNull("Tail must have [Argument] attribute");
            argAttr!.Name.Should().Be("tail");
            var aliasAttr = prop.GetCustomAttribute<AliasAttribute>();
            aliasAttr.Should().NotBeNull("Tail must have [Alias] for -t");
            aliasAttr!.Alias.Should().Be('t');
        }

        // T115 CV-029: --lines and --tail mutually exclusive
        [TestMethod]
        public async Task Execute_LinesAndTailBothSet_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "line1\nline2\nline3");
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";
            cmd.Lines = 2;
            cmd.Tail = 1;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("cannot be used together", "should report mutual exclusion error");
        }

        // T116 CV-030: --force bypasses binary check
        [TestMethod]
        public async Task Execute_BinaryFileWithForce_DisplaysContent()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            // Write a file with null bytes (binary)
            var binaryContent = new byte[] { 72, 101, 108, 108, 111, 0, 87, 111, 114, 108, 100 }; // "Hello\0World"
            fs.File.WriteAllBytes(@"C:\storage\binary.dat", binaryContent);
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\binary.dat";
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().NotContain("Binary file detected", "with --force, binary check should be bypassed");
        }

        // T117 CV-031: --force bypasses large-file prompt
        [TestMethod]
        public async Task Execute_LargeFileWithForce_DisplaysWithoutPrompt()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            // Create a file larger than 25MB threshold
            var lines = Enumerable.Range(1, 100).Select(i => new string('x', 300_000)).ToArray();
            fs.File.WriteAllLines(@"C:\storage\large.txt", lines);
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\large.txt";
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().NotContain("large", "with --force, large-file check should be bypassed");
        }

        // T118 DF-033: Outputs all lines of text file
        [TestMethod]
        public async Task Execute_TextFile_OutputsAllLines()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "line1\nline2\nline3");
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("line1");
            console.Output.Should().Contain("line2");
            console.Output.Should().Contain("line3");
        }

        // T119 DF-034: Outputs only first 2 lines
        [TestMethod]
        public async Task Execute_WithLines2_OutputsFirstTwoLinesOnly()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "line1\nline2\nline3\nline4\nline5");
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";
            cmd.Lines = 2;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("line1");
            console.Output.Should().Contain("line2");
            console.Output.Should().NotContain("line3", "line3 should not appear with --lines=2");
            console.Output.Should().Contain("first 2 of 5 lines", "footer should show line count");
        }

        // T120 DF-035: --lines > file length: all lines shown, no error
        [TestMethod]
        public async Task Execute_WithLinesGreaterThanFileLength_OutputsAllLines()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "line1\nline2\nline3\nline4\nline5");
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";
            cmd.Lines = 100;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("line1");
            console.Output.Should().Contain("line5");
            console.Output.Should().Contain("first 5 of 5 lines", "footer should reflect actual lines shown");
        }

        // T121 DF-036: Outputs only last 2 lines
        [TestMethod]
        public async Task Execute_WithTail2_OutputsLastTwoLinesOnly()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "line1\nline2\nline3\nline4\nline5");
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";
            cmd.Tail = 2;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("line4");
            console.Output.Should().Contain("line5");
            console.Output.Should().NotContain("line3", "line3 should not appear with --tail=2");
            console.Output.Should().Contain("last 2 of 5 lines", "footer should show tail count");
        }

        // T122 DF-037: --tail > file length: all lines shown
        [TestMethod]
        public async Task Execute_WithTailGreaterThanFileLength_OutputsAllLines()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "line1\nline2\nline3\nline4\nline5");
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";
            cmd.Tail = 100;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("line1");
            console.Output.Should().Contain("line5");
            console.Output.Should().Contain("last 5 of 5 lines", "footer should reflect actual lines shown");
        }

        // T123 DF-038: Binary file detected — aborts
        [TestMethod]
        public async Task Execute_BinaryFile_AbortsWithError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            var binaryContent = new byte[] { 72, 101, 108, 0, 111 }; // contains null byte
            fs.File.WriteAllBytes(@"C:\storage\binary.dat", binaryContent);
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\binary.dat";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("Binary file detected", "should report binary file");
        }

        // T124 DF-039: Binary file with --force — outputs content
        [TestMethod]
        public async Task Execute_BinaryFileWithForce_OutputsContent()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            // Write text content that ReadAllLines can read (with a null byte embedded)
            fs.File.WriteAllBytes(@"C:\storage\mixed.dat", new byte[] { 72, 101, 108, 108, 111, 0 }); // "Hello\0"
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\mixed.dat";
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            // With --force, content should be output (not blocked by binary detection)
            console.Output.Should().NotContain("Binary file detected");
        }

        // T125 DF-040: Large file without --lines prompts, user confirms (yes)
        [TestMethod]
        public async Task Execute_LargeFile_UserConfirmsYes_OutputsContent()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            // Create a file larger than 25MB threshold
            var lines = Enumerable.Range(1, 100).Select(i => $"line{i}: " + new string('x', 300_000)).ToArray();
            fs.File.WriteAllLines(@"C:\storage\large.txt", lines);
            var console = new TestConsole();
            console.Input.PushTextWithEnter("y");
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\large.txt";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("line1:", "content should be displayed when user confirms");
        }

        // T126 DF-041: Large file without --lines prompts, user declines (no)
        [TestMethod]
        public async Task Execute_LargeFile_UserDeclinesNo_NoOutput()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            var lines = Enumerable.Range(1, 100).Select(i => $"line{i}: " + new string('x', 300_000)).ToArray();
            fs.File.WriteAllLines(@"C:\storage\large.txt", lines);
            var console = new TestConsole();
            console.Input.PushTextWithEnter("n");
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\large.txt";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().NotContain("line1:", "content should not be displayed when user declines");
        }

        // T127 DF-042: Large file with --force — no prompt
        [TestMethod]
        public async Task Execute_LargeFileWithForce_NoPrompt_OutputsContent()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            var lines = Enumerable.Range(1, 100).Select(i => $"line{i}: " + new string('x', 300_000)).ToArray();
            fs.File.WriteAllLines(@"C:\storage\large.txt", lines);
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\large.txt";
            cmd.Force = true;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("line1:", "content should be displayed with --force");
            console.Output.Should().NotContain("Display all?", "no prompt should appear with --force");
        }

        // T129 EH-015: File not found
        [TestMethod]
        public async Task Execute_FileNotFound_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\nosuchfile.txt";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("not found", "should show file not found error");
        }

        // T130 EH-016: Path is a directory
        [TestMethod]
        public async Task Execute_PathIsDirectory_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage\mydir");
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\mydir";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("directory", "should report that path is a directory");
        }

        // T131 EH-017: Binary content without --force
        [TestMethod]
        public async Task Execute_BinaryContentWithoutForce_ProducesError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllBytes(@"C:\storage\data.bin", new byte[] { 0, 1, 2, 3, 4 });
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\data.bin";

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().Contain("Binary file detected", "should detect binary content");
            console.Output.Should().Contain("--force", "should suggest using --force");
        }

        // T132 EH-018: --lines=0 produces no output, no error
        [TestMethod]
        public async Task Execute_WithLinesZero_NoOutputNoError()
        {
            var fs = new MockFileSystem();
            fs.Directory.CreateDirectory(@"C:\storage");
            fs.File.WriteAllText(@"C:\storage\file.txt", "line1\nline2\nline3");
            var console = new TestConsole();
            var cmd = new CatCommand(fs);
            cmd.SetConsole(console);
            cmd.Path = @"C:\storage\file.txt";
            cmd.Lines = 0;

            await cmd.Execute(new CommandExecutionContext());

            console.Output.Should().NotContain("line1", "no lines should be output with --lines=0");
            console.Output.Should().NotContain("Error", "should not produce an error");
        }
    }
}
