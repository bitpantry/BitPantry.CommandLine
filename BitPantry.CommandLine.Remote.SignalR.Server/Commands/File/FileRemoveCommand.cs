using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using Spectre.Console;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands.File
{
    /// <summary>
    /// Removes files or directories from the remote server's sandboxed file system.
    /// Note: Remote commands do not support interactive prompts. Use -f to force removal.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "rm")]
    [Description("Removes a file or directory from the remote file system")]
    public class FileRemoveCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, IsRequired = true)]
        [Alias('p')]
        [Description("The path to remove")]
        [FilePathCompletion]
        public string Path { get; set; } = string.Empty;

        [Argument]
        [Alias('r')]
        [Description("Remove directories and their contents recursively")]
        public Option Recursive { get; set; }

        [Argument]
        [Alias('f')]
        [Description("Force removal without confirmation (required for remote commands)")]
        public Option Force { get; set; }

        public FileRemoveCommand(IFileSystem fileSystem)
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

            // Remote commands don't support interactive prompts - require -f flag
            if (!Force.IsPresent)
            {
                var itemType = isDirectory ? "directory" : "file";
                Console.MarkupLine($"[yellow]Use -f flag to confirm removal of {itemType}: {Markup.Escape(targetPath)}[/]");
                return;
            }

            try
            {
                if (isDirectory)
                {
                    if (!Recursive.IsPresent)
                    {
                        // Check if directory is empty
                        var hasContents = _fileSystem.Directory.GetFileSystemEntries(targetPath).Any();
                        if (hasContents)
                        {
                            Console.MarkupLine($"[red]Directory is not empty. Use -r to remove recursively.[/]");
                            return;
                        }
                    }
                    _fileSystem.Directory.Delete(targetPath, Recursive.IsPresent);
                    Console.MarkupLine($"[green]Removed directory: {Markup.Escape(targetPath)}[/]");
                }
                else
                {
                    _fileSystem.File.Delete(targetPath);
                    Console.MarkupLine($"[green]Removed file: {Markup.Escape(targetPath)}[/]");
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Markup.Escape(targetPath)}[/]");
            }
            catch (IOException ex)
            {
                Console.MarkupLine($"[red]Error removing path: {Markup.Escape(ex.Message)}[/]");
            }
        }
    }
}
