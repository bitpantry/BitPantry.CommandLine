using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientFileAccess.TestCommands
{
    /// <summary>
    /// Test command that reads multiple files from the client via IClientFileAccess.GetFilesAsync.
    /// Writes each file name and content to the server console to prove the round-trip.
    /// Supports --limit to test lazy enumeration (partial iteration).
    /// </summary>
    [Command(Name = "test-get-files")]
    public class TestGetFilesCommand : CommandBase
    {
        [Argument(Position = 0)]
        public string GlobPattern { get; set; }

        [Argument(Name = "--limit")]
        public int Limit { get; set; } = 0;

        private readonly IClientFileAccess _clientFiles;

        public TestGetFilesCommand(IClientFileAccess clientFiles)
        {
            _clientFiles = clientFiles;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            int count = 0;
            await foreach (var file in _clientFiles.GetFilesAsync(GlobPattern, ct: ctx.CancellationToken))
            {
                await using (file)
                {
                    using var reader = new StreamReader(file.Stream);
                    var content = await reader.ReadToEndAsync(ctx.CancellationToken);
                    Console.WriteLine($"GetFiles:{file.FileName}:{content}");
                }
                count++;
                if (Limit > 0 && count >= Limit)
                    break;
            }
            Console.WriteLine($"GetFiles:total={count}");
        }
    }
}
