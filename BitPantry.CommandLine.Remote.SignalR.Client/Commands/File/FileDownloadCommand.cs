using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Client;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.File
{
    /// <summary>
    /// Downloads a file from the remote server's sandboxed file system to the local machine.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "download")]
    [Description("Downloads a file from the remote server to the local machine")]
    public class FileDownloadCommand : CommandBase
    {
        private readonly FileTransferService _transferService;
        private readonly IServerProxy _proxy;
        private readonly IAnsiConsole _console;

        [Argument(Position = 0, IsRequired = true)]
        [Alias('s')]
        [Description("The remote file path to download")]
        [RemoteFilePathCompletion]
        public string Source { get; set; } = string.Empty;

        [Argument(Position = 1)]
        [Alias('d')]
        [Description("The local destination path. If not specified, uses the source filename in the current directory.")]
        [FilePathCompletion]
        public string Destination { get; set; } = string.Empty;

        [Argument]
        [Alias('f')]
        [Description("Force overwrite if local file exists")]
        public Option Force { get; set; }

        public FileDownloadCommand(FileTransferService transferService, IServerProxy proxy, IAnsiConsole console)
        {
            _transferService = transferService;
            _proxy = proxy;
            _console = console;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // Check connection
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
            {
                Console.MarkupLine("[red]Not connected to a remote server. Use 'server connect' first.[/]");
                return;
            }

            // Normalize source path - strip leading slash if present (server expects relative paths)
            var sourcePath = Source;
            if (sourcePath.StartsWith("/"))
            {
                sourcePath = sourcePath.TrimStart('/');
            }

            // Determine destination path
            var destinationPath = Destination;
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                destinationPath = Path.GetFileName(sourcePath);
            }

            // Make destination absolute if relative
            if (!Path.IsPathRooted(destinationPath))
            {
                destinationPath = Path.Combine(Environment.CurrentDirectory, destinationPath);
            }

            // Check if local file exists
            if (System.IO.File.Exists(destinationPath) && !Force.IsPresent)
            {
                if (!_console.Confirm($"Local file '{destinationPath}' already exists. Overwrite?", defaultValue: false))
                {
                    Console.MarkupLine("[dim]Download cancelled[/]");
                    return;
                }
            }

            try
            {
                // Download the file
                await _transferService.DownloadFile(
                    sourcePath,
                    destinationPath,
                    ctx.CancellationToken);
            }
            catch (FileNotFoundException)
            {
                Console.MarkupLine($"[red]Remote file not found: {Markup.Escape(sourcePath)}[/]");
            }
            catch (OperationCanceledException)
            {
                Console.MarkupLine("[yellow]Download cancelled[/]");
            }
            catch (InvalidDataException)
            {
                Console.MarkupLine("[red]Download failed: File integrity check failed (checksum mismatch)[/]");
            }
            catch (Exception ex)
            {
                Console.MarkupLine($"[red]Download failed: {Markup.Escape(ex.Message)}[/]");
            }
        }
    }
}
