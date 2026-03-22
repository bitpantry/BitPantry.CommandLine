using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using Spectre.Console;
using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands
{
    [Command(Name = "cat")]
    [InGroup<ServerGroup>]
    [Description("Display file contents on the remote server")]
    public class CatCommand : CommandBase
    {
        private const int BinaryCheckBytes = 8192;
        private const long LargeFileSizeBytes = 25 * 1024 * 1024; // 25 MB

        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, Name = "path", IsRequired = true)]
        [Description("Path of file to display")]
        [ServerFilePathAutoComplete]
        public string Path { get; set; }

        [Argument(Name = "lines"), Alias('n')]
        [Description("Display only the first N lines")]
        public int? Lines { get; set; }

        [Argument(Name = "tail"), Alias('t')]
        [Description("Display only the last N lines")]
        public int? Tail { get; set; }

        [Argument(Name = "force"), Flag, Alias('f')]
        [Description("Output binary files and skip large-file prompt")]
        public bool Force { get; set; }

        public CatCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task Execute(CommandExecutionContext context)
        {
            try
            {
                if (!_fileSystem.File.Exists(Path))
                {
                    if (_fileSystem.Directory.Exists(Path))
                    {
                        Console.MarkupLine($"[red]Path is a directory: {Path}[/]");
                        return;
                    }
                    Console.MarkupLine($"[red]File not found: {Path}[/]");
                    return;
                }

                // Mutual exclusion
                if (Lines.HasValue && Tail.HasValue)
                {
                    Console.MarkupLine("[red]--lines and --tail cannot be used together.[/]");
                    return;
                }

                // Binary detection
                if (!Force)
                {
                    using var stream = _fileSystem.File.OpenRead(Path);
                    var buffer = new byte[BinaryCheckBytes];
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (Array.IndexOf(buffer, (byte)0, 0, read) >= 0)
                    {
                        Console.MarkupLine("[red]Binary file detected. Use --force to display anyway.[/]");
                        return;
                    }
                }

                // Large-file prompt
                var fileInfo = _fileSystem.FileInfo.New(Path);
                if (!Force && !Lines.HasValue && !Tail.HasValue && fileInfo.Length > LargeFileSizeBytes)
                {
                    var confirmed = Console.Prompt(
                        new ConfirmationPrompt($"File is {FormatSize(fileInfo.Length)}. Display all?"));
                    if (!confirmed)
                        return;
                }

                // Output
                var allLines = _fileSystem.File.ReadAllLines(Path);
                var totalLines = allLines.Length;
                string[] outputLines;

                if (Lines.HasValue)
                    outputLines = allLines.Take(Lines.Value).ToArray();
                else if (Tail.HasValue)
                    outputLines = allLines.TakeLast(Tail.Value).ToArray();
                else
                    outputLines = allLines;

                foreach (var line in outputLines)
                    Console.WriteLine(line);

                if (Lines.HasValue || Tail.HasValue)
                    Console.MarkupLine($"[grey]Showing {(Lines.HasValue ? "first" : "last")} {outputLines.Length} of {totalLines} lines[/]");
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
