using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using Spectre.Console;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands.File
{
    /// <summary>
    /// Displays the contents of a file from the remote server's sandboxed file system.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "cat")]
    [Description("Displays the contents of a file")]
    public class FileCatCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, IsRequired = true)]
        [Alias('p')]
        [Description("The path of the file to display")]
        [FilePathCompletion]
        public string Path { get; set; } = string.Empty;

        [Argument]
        [Alias('n')]
        [Description("Show line numbers")]
        public Option LineNumbers { get; set; }

        [Argument]
        [Description("Number of lines to show from the beginning")]
        public int? Head { get; set; }

        [Argument]
        [Alias('t')]
        [Description("Number of lines to show from the end")]
        public int? Tail { get; set; }

        public FileCatCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            var targetPath = Path;
            
            // Remove leading slash if present - sandboxed file system expects relative paths
            if (targetPath.StartsWith("/"))
            {
                targetPath = targetPath.TrimStart('/');
            }

            if (!_fileSystem.File.Exists(targetPath))
            {
                Console.MarkupLine($"[red]File not found: {Markup.Escape(targetPath)}[/]");
                return;
            }

            try
            {
                // Check if file might be binary
                if (IsBinaryFile(targetPath))
                {
                    Console.MarkupLine($"[yellow]Warning: File appears to be binary. Display may contain unreadable characters.[/]");
                }

                var lines = _fileSystem.File.ReadAllLines(targetPath);

                // Apply head/tail filters
                if (Head.HasValue && Head.Value > 0)
                {
                    lines = lines.Take(Head.Value).ToArray();
                }
                else if (Tail.HasValue && Tail.Value > 0)
                {
                    lines = lines.TakeLast(Tail.Value).ToArray();
                }

                // Display content
                var lineNumber = Tail.HasValue ? lines.Length - Tail.Value + 1 : 1;
                if (lineNumber < 1) lineNumber = 1;

                foreach (var line in lines)
                {
                    if (LineNumbers.IsPresent)
                    {
                        Console.MarkupLine($"[dim]{lineNumber,4}:[/] {Markup.Escape(line)}");
                    }
                    else
                    {
                        Console.WriteLine(line);
                    }
                    lineNumber++;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Markup.Escape(targetPath)}[/]");
            }
            catch (IOException ex)
            {
                Console.MarkupLine($"[red]Error reading file: {Markup.Escape(ex.Message)}[/]");
            }
        }

        private bool IsBinaryFile(string path)
        {
            try
            {
                // Read first 8KB to check for null bytes (common binary indicator)
                using var stream = _fileSystem.File.OpenRead(path);
                var buffer = new byte[8192];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                
                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == 0)
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
