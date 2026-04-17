using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientFileAccess.TestCommands
{
    /// <summary>
    /// Test command that saves in-memory content (a Stream) to the client via IClientFileAccess.
    /// Uses SaveFileAsync(Stream, ...) overload.
    /// </summary>
    [Command(Name = "test-stream-save")]
    public class TestStreamSaveCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string ClientPath { get; set; }

        [Argument(Position = 1)]
        public string Content { get; set; }

        private readonly IClientFileAccess _clientFiles;

        public TestStreamSaveCommand(IClientFileAccess clientFiles)
        {
            _clientFiles = clientFiles;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(Content);
            using var stream = new MemoryStream(bytes);
            await _clientFiles.SaveFileAsync(stream, ClientPath, ct: ctx.CancellationToken);
            Console.WriteLine("StreamSave complete");
        }
    }
}
