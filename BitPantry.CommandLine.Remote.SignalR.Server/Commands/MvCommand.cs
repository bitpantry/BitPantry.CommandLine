using BitPantry.CommandLine.API;
using Spectre.Console;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands
{
    [Command(Name = "mv")]
    [InGroup<ServerGroup>]
    [Description("Moves or renames files and directories on the remote server")]
    public class MvCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, Name = "source", IsRequired = true)]
        [Description("Source path to move")]
        public string Source { get; set; }

        [Argument(Position = 1, Name = "destination", IsRequired = true)]
        [Description("Destination path")]
        public string Destination { get; set; }

        [Argument(Name = "force"), Flag, Alias('f')]
        [Description("Overwrite destination if it exists")]
        public bool Force { get; set; }

        public MvCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task Execute(CommandExecutionContext context)
        {
            try
            {
                if (string.Equals(Source, Destination, System.StringComparison.OrdinalIgnoreCase))
                {
                    Console.MarkupLine($"[red]Source and destination are the same: {Source}[/]");
                    return;
                }

                var isFile = _fileSystem.File.Exists(Source);
                var isDir = _fileSystem.Directory.Exists(Source);

            if (!isFile && !isDir)
            {
                Console.MarkupLine($"[red]Source not found: {Source}[/]");
                return;
            }

            if (isFile)
            {
                if (_fileSystem.File.Exists(Destination) && !Force)
                {
                    Console.MarkupLine($"[red]Destination already exists: {Destination}. Use --force to overwrite.[/]");
                    return;
                }

                if (_fileSystem.File.Exists(Destination) && Force)
                {
                    _fileSystem.File.Delete(Destination);
                }

                _fileSystem.File.Move(Source, Destination);
                Console.MarkupLine($"[green]Moved: {Source} → {Destination}[/]");
                return;
            }

            // Directory move
            if (_fileSystem.Directory.Exists(Destination) && !Force)
            {
                Console.MarkupLine($"[red]Destination already exists: {Destination}. Use --force to overwrite.[/]");
                return;
            }

            if (_fileSystem.Directory.Exists(Destination) && Force)
            {
                _fileSystem.Directory.Delete(Destination, true);
            }

            _fileSystem.Directory.Move(Source, Destination);
            Console.MarkupLine($"[green]Moved: {Source} → {Destination}[/]");
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Source}[/]");
            }

            await Task.CompletedTask;
        }
    }
}
