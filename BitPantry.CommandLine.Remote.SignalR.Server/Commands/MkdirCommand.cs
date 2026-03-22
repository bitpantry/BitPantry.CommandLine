using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using Spectre.Console;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands
{
    [Command(Name = "mkdir")]
    [InGroup<ServerGroup>]
    [Description("Creates a directory on the remote server")]
    public class MkdirCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, Name = "path", IsRequired = true)]
        [Description("Directory path to create")]
        [ServerDirectoryPathAutoComplete]
        public string Path { get; set; }

        [Argument(Name = "parents"), Flag]
        [Description("Create parent directories as needed")]
        public bool Parents { get; set; }

        public MkdirCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task Execute(CommandExecutionContext context)
        {
            try
            {
                if (_fileSystem.Directory.Exists(Path))
                {
                    Console.MarkupLine($"[green]Created: {Path}[/]");
                    return;
                }

                var parentDir = _fileSystem.Path.GetDirectoryName(Path);
                if (!Parents && !string.IsNullOrEmpty(parentDir) && !_fileSystem.Directory.Exists(parentDir))
                {
                    Console.MarkupLine($"[red]Parent directory does not exist: {parentDir}[/]");
                    return;
                }

                _fileSystem.Directory.CreateDirectory(Path);
                Console.MarkupLine($"[green]Created: {Path}[/]");
            }
            catch (UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Path}[/]");
            }

            await Task.CompletedTask;
        }
    }
}
