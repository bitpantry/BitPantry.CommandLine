using BitPantry.CommandLine.API;
using Spectre.Console;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands
{
    [Command(Name = "rm")]
    [InGroup<ServerGroup>]
    [Description("Removes files or directories on the remote server")]
    public class RmCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, Name = "path", IsRequired = true)]
        [Description("Path to remove")]
        public string Path { get; set; }

        [Argument(Name = "recursive"), Flag, Alias('r')]
        [Description("Recursively remove directories and their contents")]
        public bool Recursive { get; set; }

        [Argument(Name = "directory"), Flag, Alias('d')]
        [Description("Allow removal of empty directories")]
        public bool Directory { get; set; }

        [Argument(Name = "force"), Flag, Alias('f')]
        [Description("Ignore nonexistent files and do not prompt")]
        public bool Force { get; set; }

        public RmCommand(IFileSystem fileSystem)
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
                    if (Force)
                        return;

                    Console.MarkupLine($"[red]Path not found: {Path}[/]");
                    return;
                }

                if (isFile)
                {
                    _fileSystem.File.Delete(Path);
                    Console.MarkupLine($"[green]Removed: {Path}[/]");
                    return;
                }

                // It's a directory
                var entries = _fileSystem.Directory.GetFileSystemEntries(Path);
                var isEmpty = entries.Length == 0;

                if (!isEmpty && !Recursive)
                {
                    Console.MarkupLine($"[red]Cannot remove '{Path}': Directory is not empty. Use --recursive to remove.[/]");
                    return;
                }

                if (isEmpty && !Directory && !Recursive)
                {
                    Console.MarkupLine($"[red]Cannot remove '{Path}': Is a directory. Use --directory to remove empty directories.[/]");
                    return;
                }

                _fileSystem.Directory.Delete(Path, Recursive);
                Console.MarkupLine($"[green]Removed: {Path}[/]");
            }
            catch (UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Path}[/]");
            }

            await Task.CompletedTask;
        }
    }
}
