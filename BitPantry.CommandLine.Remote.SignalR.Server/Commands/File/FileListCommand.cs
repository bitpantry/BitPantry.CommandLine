using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using Spectre.Console;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands.File
{
    /// <summary>
    /// Lists files and directories in the remote server's sandboxed file system.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "ls")]
    [Description("Lists files and directories in the specified path")]
    public class FileListCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0)]
        [Alias('p')]
        [Description("The path to list. Defaults to current directory (/).")]
        [DirectoryPathCompletion]
        public string Path { get; set; } = "/";

        [Argument]
        [Alias('l')]
        [Description("Use long listing format showing size, date, and attributes")]
        public Option Long { get; set; }

        [Argument]
        [Alias('a')]
        [Description("Include hidden files (files starting with .)")]
        public Option All { get; set; }

        public FileListCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            var targetPath = string.IsNullOrWhiteSpace(Path) ? "" : Path;
            
            // Remove leading slash if present - sandboxed file system expects relative paths
            if (targetPath.StartsWith("/"))
            {
                targetPath = targetPath.TrimStart('/');
            }
            
            // Use "." to represent the root directory (empty string is not valid)
            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = ".";
            }

            if (!_fileSystem.Directory.Exists(targetPath))
            {
                Console.MarkupLine($"[red]Directory not found: {Markup.Escape(targetPath)}[/]");
                return;
            }

            var entries = new List<FileSystemEntry>();

            // Get directories
            foreach (var dir in _fileSystem.Directory.GetDirectories(targetPath))
            {
                var dirInfo = _fileSystem.DirectoryInfo.New(dir);
                var name = _fileSystem.Path.GetFileName(dir);

                // Skip hidden files unless -a is specified
                if (!All.IsPresent && name.StartsWith("."))
                    continue;

                entries.Add(new FileSystemEntry
                {
                    Name = name,
                    IsDirectory = true,
                    Size = 0,
                    LastModified = dirInfo.LastWriteTime
                });
            }

            // Get files
            foreach (var file in _fileSystem.Directory.GetFiles(targetPath))
            {
                var fileInfo = _fileSystem.FileInfo.New(file);
                var name = _fileSystem.Path.GetFileName(file);

                // Skip hidden files unless -a is specified
                if (!All.IsPresent && name.StartsWith("."))
                    continue;

                entries.Add(new FileSystemEntry
                {
                    Name = name,
                    IsDirectory = false,
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                });
            }

            // Sort: directories first, then alphabetically
            entries = entries
                .OrderByDescending(e => e.IsDirectory)
                .ThenBy(e => e.Name)
                .ToList();

            if (entries.Count == 0)
            {
                Console.MarkupLine("[dim]Directory is empty[/]");
                return;
            }

            if (Long.IsPresent)
            {
                RenderLongFormat(entries);
            }
            else
            {
                RenderShortFormat(entries);
            }
        }

        private void RenderLongFormat(List<FileSystemEntry> entries)
        {
            var table = new Table();
            table.Border = TableBorder.None;
            table.ShowHeaders = false;
            table.AddColumn(new TableColumn("Type").NoWrap());
            table.AddColumn(new TableColumn("Size").RightAligned());
            table.AddColumn(new TableColumn("Date").NoWrap());
            table.AddColumn(new TableColumn("Name"));

            foreach (var entry in entries)
            {
                var typeIndicator = entry.IsDirectory ? "[blue]d[/]" : "[dim]-[/]";
                var size = entry.IsDirectory ? "[dim]<DIR>[/]" : FormatSize(entry.Size);
                var date = entry.LastModified.ToString("yyyy-MM-dd HH:mm");
                var name = entry.IsDirectory 
                    ? $"[blue]{Markup.Escape(entry.Name)}/[/]" 
                    : Markup.Escape(entry.Name);

                table.AddRow(typeIndicator, size, date, name);
            }

            Console.Write(table);
        }

        private void RenderShortFormat(List<FileSystemEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.IsDirectory)
                {
                    Console.MarkupLine($"[blue]{Markup.Escape(entry.Name)}/[/]");
                }
                else
                {
                    Console.WriteLine(entry.Name);
                }
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        }

        private class FileSystemEntry
        {
            public string Name { get; set; } = string.Empty;
            public bool IsDirectory { get; set; }
            public long Size { get; set; }
            public DateTime LastModified { get; set; }
        }
    }
}
