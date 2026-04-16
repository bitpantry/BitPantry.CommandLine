using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Processing.Execution;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Client
{
    // Test Validity Check:
    //   Invokes code under test: YES (commands inject IClientFileAccess via DI and call its methods)
    //   Breakage detection: YES (if DI wiring breaks, commands fail; if file ops break, assertions fail)
    //   Not a tautology: YES (verifies end-to-end DI + file operation behavior)

    /// <summary>
    /// Command that injects IClientFileAccess and reads a file, storing the result
    /// in a static field so the test can verify it.
    /// </summary>
    [Command(Name = "testGetFile")]
    public class TestGetFileCommand : CommandBase
    {
        private readonly IClientFileAccess _fileAccess;

        public static string LastReadContent { get; set; }

        public TestGetFileCommand(IClientFileAccess fileAccess)
        {
            _fileAccess = fileAccess;
        }

        [Argument(Position = 0)]
        public string Path { get; set; }

        public async Task Execute(CommandExecutionContext ctx)
        {
            await using var file = await _fileAccess.GetFileAsync(Path);
            using var reader = new StreamReader(file.Stream);
            LastReadContent = await reader.ReadToEndAsync();
        }
    }

    /// <summary>
    /// Command that injects IClientFileAccess and saves content to a file.
    /// </summary>
    [Command(Name = "testSaveFile")]
    public class TestSaveFileCommand : CommandBase
    {
        private readonly IClientFileAccess _fileAccess;

        public TestSaveFileCommand(IClientFileAccess fileAccess)
        {
            _fileAccess = fileAccess;
        }

        [Argument(Position = 0)]
        public string SourcePath { get; set; }

        [Argument(Position = 1)]
        public string DestPath { get; set; }

        public async Task Execute(CommandExecutionContext ctx)
        {
            await _fileAccess.SaveFileAsync(SourcePath, DestPath);
        }
    }

    [TestClass]
    public class ClientFileAccessIntegrationTests
    {
        [TestMethod]
        public async Task GetFile_LocalCommand_ReadsDirectly()
        {
            // Arrange
            var fs = new MockFileSystem();
            fs.AddFile("/data/hello.txt", new MockFileData("hello from file"));
            TestGetFileCommand.LastReadContent = null;

            using var app = new CommandLineApplicationBuilder()
                .UsingFileSystem(fs)
                .RegisterCommand<TestGetFileCommand>()
                .Build();

            // Act
            var result = await app.RunOnce("testGetFile /data/hello.txt");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            TestGetFileCommand.LastReadContent.Should().Be("hello from file");
        }

        [TestMethod]
        public async Task SaveFile_LocalCommand_WritesDirectly()
        {
            // Arrange
            var fs = new MockFileSystem();
            fs.AddFile("/source/data.txt", new MockFileData("save me"));

            using var app = new CommandLineApplicationBuilder()
                .UsingFileSystem(fs)
                .RegisterCommand<TestSaveFileCommand>()
                .Build();

            // Act
            var result = await app.RunOnce("testSaveFile /source/data.txt /dest/output.txt");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            fs.File.ReadAllText("/dest/output.txt").Should().Be("save me");
        }
    }
}
