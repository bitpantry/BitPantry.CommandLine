using BitPantry.CommandLine.API;
using Spectre.Console;
using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands
{
    [Command(Name = "stat")]
    [InGroup<ServerGroup>]
    [Description("Display file or directory information on the remote server")]
    public class StatCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, Name = "path", IsRequired = true)]
        [Description("Path to inspect")]
        public string Path { get; set; }

        public StatCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task Execute(CommandExecutionContext context)
        {
            try
            {
                var isFile = _fileSystem.File.Exists(Path);
                var isDir = _fileSystem.Directory.Exists(Path);

                if (!isFile && !isDir)
                {
                    Console.MarkupLine($"[red]Path not found: {Path}[/]");
                    return;
                }

                if (isFile)
                {
                    var info = _fileSystem.FileInfo.New(Path);
                    Console.MarkupLine($"[bold]Name:[/] {info.Name}");
                    Console.MarkupLine($"[bold]Type:[/] File");
                    Console.MarkupLine($"[bold]Path:[/] {Path}");
                    Console.MarkupLine($"[bold]Size:[/] {FormatSize(info.Length)} ({info.Length:N0} bytes)");
                    Console.MarkupLine($"[bold]Created:[/] {info.CreationTime}");
                    Console.MarkupLine($"[bold]Last Modified:[/] {info.LastWriteTime}");
                }
                else
                {
                    var dirInfo = _fileSystem.DirectoryInfo.New(Path);
                    var files = _fileSystem.Directory.GetFiles(Path, "*", System.IO.SearchOption.AllDirectories);
                    var dirs = _fileSystem.Directory.GetDirectories(Path, "*", System.IO.SearchOption.AllDirectories);

                    Console.MarkupLine($"[bold]Name:[/] {dirInfo.Name}");
                    Console.MarkupLine($"[bold]Type:[/] Directory");
                    Console.MarkupLine($"[bold]Path:[/] {Path}");
                    Console.MarkupLine($"[bold]ItemCount:[/] {files.Length + dirs.Length}");
                    Console.MarkupLine($"[bold]FileCount:[/] {files.Length}");
                    Console.MarkupLine($"[bold]DirectoryCount:[/] {dirs.Length}");
                    Console.MarkupLine($"[bold]Created:[/] {dirInfo.CreationTime}");
                    Console.MarkupLine($"[bold]Last Modified:[/] {dirInfo.LastWriteTime}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Path}[/]");
            }

            await Task.CompletedTask;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }
    }
}
