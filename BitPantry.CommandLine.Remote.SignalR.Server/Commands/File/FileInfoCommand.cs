using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using Spectre.Console;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands.File
{
    /// <summary>
    /// Displays detailed information about a file or directory in the remote server's sandboxed file system.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "info")]
    [Description("Displays detailed information about a file or directory")]
    public class FileInfoCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, IsRequired = true)]
        [Alias('p')]
        [Description("The path to get information about")]
        [FilePathCompletion]
        public string Path { get; set; } = string.Empty;

        public FileInfoCommand(IFileSystem fileSystem)
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

            var isDirectory = _fileSystem.Directory.Exists(targetPath);
            var isFile = _fileSystem.File.Exists(targetPath);

            if (!isDirectory && !isFile)
            {
                Console.MarkupLine($"[red]Path not found: {Markup.Escape(targetPath)}[/]");
                return;
            }

            try
            {
                if (isDirectory)
                {
                    DisplayDirectoryInfo(targetPath);
                }
                else
                {
                    DisplayFileInfo(targetPath);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Markup.Escape(targetPath)}[/]");
            }
            catch (IOException ex)
            {
                Console.MarkupLine($"[red]Error reading info: {Markup.Escape(ex.Message)}[/]");
            }
        }

        private void DisplayFileInfo(string path)
        {
            var fileInfo = _fileSystem.FileInfo.New(path);
            
            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("Type", "File");
            table.AddRow("Name", Markup.Escape(fileInfo.Name));
            table.AddRow("Path", Markup.Escape(path));
            table.AddRow("Size", FormatSize(fileInfo.Length));
            table.AddRow("Created", fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("Modified", fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("Accessed", fileInfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("Extension", string.IsNullOrEmpty(fileInfo.Extension) ? "[dim]none[/]" : fileInfo.Extension);
            table.AddRow("Read-Only", fileInfo.IsReadOnly ? "Yes" : "No");

            Console.Write(table);
        }

        private void DisplayDirectoryInfo(string path)
        {
            var dirInfo = _fileSystem.DirectoryInfo.New(path);
            
            // Count contents
            var files = _fileSystem.Directory.GetFiles(path);
            var dirs = _fileSystem.Directory.GetDirectories(path);
            long totalSize = 0;
            foreach (var file in files)
            {
                totalSize += _fileSystem.FileInfo.New(file).Length;
            }

            var table = new Table();
            table.Border = TableBorder.Rounded;
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("Type", "Directory");
            table.AddRow("Name", Markup.Escape(dirInfo.Name));
            table.AddRow("Path", Markup.Escape(path));
            table.AddRow("Files", files.Length.ToString());
            table.AddRow("Subdirectories", dirs.Length.ToString());
            table.AddRow("Total Size", FormatSize(totalSize) + " [dim](files only, non-recursive)[/]");
            table.AddRow("Created", dirInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("Modified", dirInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));

            Console.Write(table);
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
