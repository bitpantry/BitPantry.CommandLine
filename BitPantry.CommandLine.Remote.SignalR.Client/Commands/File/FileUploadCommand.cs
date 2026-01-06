using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Client;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.File
{
    /// <summary>
    /// Uploads a local file to the remote server's sandboxed file system.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "upload")]
    [Description("Uploads a local file to the remote server")]
    public class FileUploadCommand : CommandBase
    {
        private const long ProgressThreshold = 1024 * 1024; // 1 MB - show progress bar for files larger than this
        
        private readonly FileTransferService _transferService;
        private readonly IServerProxy _proxy;
        private readonly IAnsiConsole _console;

        [Argument(Position = 0, IsRequired = true)]
        [Alias('s')]
        [Description("The local file path to upload")]
        [FilePathCompletion]
        public string Source { get; set; } = string.Empty;

        [Argument(Position = 1)]
        [Alias('d')]
        [Description("The destination path on the remote server. If not specified, uses the source filename in the root directory.")]
        public string Destination { get; set; } = string.Empty;

        [Argument]
        [Alias('f')]
        [Description("Force overwrite if file exists")]
        public Option Force { get; set; }

        public FileUploadCommand(FileTransferService transferService, IServerProxy proxy, IAnsiConsole console)
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

            // Validate source file exists
            if (!System.IO.File.Exists(Source))
            {
                Console.MarkupLine($"[red]Source file not found: {Markup.Escape(Source)}[/]");
                return;
            }

            // Determine destination path - use relative path (no leading slash)
            var destinationPath = Destination;
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                destinationPath = System.IO.Path.GetFileName(Source);
            }
            else if (destinationPath.StartsWith("/"))
            {
                destinationPath = destinationPath.TrimStart('/');
            }

            // Get file info
            var fileInfo = new FileInfo(Source);
            var fileSize = fileInfo.Length;

            try
            {
                // Only show progress bar for larger files
                if (fileSize > ProgressThreshold)
                {
                    Console.MarkupLine($"Uploading [cyan]{Markup.Escape(Source)}[/] ({FormatSize(fileSize)}) to [cyan]{Markup.Escape(destinationPath)}[/]...");
                    
                    await _console.Progress()
                        .AutoClear(true)
                        .Columns(new ProgressColumn[]
                        {
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new TransferSpeedColumn(),
                            new RemainingTimeColumn()
                        })
                        .StartAsync(async progressCtx =>
                        {
                            var task = progressCtx.AddTask($"[cyan]Uploading {System.IO.Path.GetFileName(Source)}[/]", maxValue: fileSize);

                            await _transferService.UploadFile(
                                Source, 
                                destinationPath, 
                                progress =>
                                {
                                    task.Value = progress.TotalRead;
                                    return Task.CompletedTask;
                                },
                                ctx.CancellationToken);

                            task.Value = fileSize; // Ensure 100% shown
                        });
                }
                else
                {
                    // Small file - just upload without progress bar
                    await _transferService.UploadFile(Source, destinationPath, null, ctx.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.MarkupLine("[yellow]Upload cancelled[/]");
            }
            catch (Exception ex)
            {
                Console.MarkupLine($"[red]Upload failed: {Markup.Escape(ex.Message)}[/]");
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        }
    }
}
