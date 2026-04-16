using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientFileAccess.TestCommands
{
    /// <summary>
    /// Test command that reads a file from the client via IClientFileAccess.
    /// Writes the content to the server console to prove the round-trip.
    /// </summary>
    [Command(Name = "test-get")]
    public class TestGetFileCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string ClientPath { get; set; }

        private readonly IClientFileAccess _clientFiles;

        public TestGetFileCommand(IClientFileAccess clientFiles)
        {
            _clientFiles = clientFiles;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            await using var file = await _clientFiles.GetFileAsync(ClientPath, ct: ctx.CancellationToken);
            using var reader = new StreamReader(file.Stream);
            var content = await reader.ReadToEndAsync(ctx.CancellationToken);
            Console.WriteLine($"GetFile:{content}");
        }
    }
}
