using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientFileAccess.TestCommands
{
    /// <summary>
    /// Test command that saves a server-side file to the client via IClientFileAccess.
    /// The sourcePath is relative to the server's storage root.
    /// </summary>
    [Command(Name = "test-save")]
    public class TestSaveFileCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string SourcePath { get; set; }

        [Argument(Position = 1)]
        public string ClientPath { get; set; }

        private readonly IClientFileAccess _clientFiles;

        public TestSaveFileCommand(IClientFileAccess clientFiles)
        {
            _clientFiles = clientFiles;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            await _clientFiles.SaveFileAsync(SourcePath, ClientPath, ct: ctx.CancellationToken);
            Console.WriteLine("SaveFile complete");
        }
    }
}
