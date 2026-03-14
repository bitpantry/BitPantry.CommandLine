using BitPantry.CommandLine.API;
using Spectre.Console;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands
{
    [Command(Name = "cp")]
    [InGroup<ServerGroup>]
    [Description("Copies files and directories on the remote server")]
    public class CpCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, Name = "source", IsRequired = true)]
        [Description("Source path to copy")]
        public string Source { get; set; }

        [Argument(Position = 1, Name = "destination", IsRequired = true)]
        [Description("Destination path")]
        public string Destination { get; set; }

        [Argument(Name = "recursive"), Flag, Alias('r')]
        [Description("Copy directories recursively")]
        public bool Recursive { get; set; }

        public CpCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task Execute(CommandExecutionContext context)
        {
            try
            {
                var isFile = _fileSystem.File.Exists(Source);
                var isDir = _fileSystem.Directory.Exists(Source);

                if (!isFile && !isDir)
                {
                    Console.MarkupLine($"[red]Source not found: {Source}[/]");
                    return;
                }

                if (isFile)
                {
                    _fileSystem.File.Copy(Source, Destination);
                    Console.MarkupLine($"[green]Copied: {Source} → {Destination}[/]");
                    return;
                }

                // Directory copy
                if (!Recursive)
                {
                    Console.MarkupLine($"[red]Cannot copy directory without --recursive flag: {Source}[/]");
                    return;
                }

                CopyDirectory(Source, Destination);
                Console.MarkupLine($"[green]Copied: {Source} → {Destination}[/]");
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Source}[/]");
            }

            await Task.CompletedTask;
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            _fileSystem.Directory.CreateDirectory(destDir);

            foreach (var file in _fileSystem.Directory.GetFiles(sourceDir))
            {
                var fileName = _fileSystem.Path.GetFileName(file);
                var destFile = _fileSystem.Path.Combine(destDir, fileName);
                _fileSystem.File.Copy(file, destFile);
            }

            foreach (var dir in _fileSystem.Directory.GetDirectories(sourceDir))
            {
                var dirName = _fileSystem.Path.GetFileName(dir);
                var destSubDir = _fileSystem.Path.Combine(destDir, dirName);
                CopyDirectory(dir, destSubDir);
            }
        }
    }
}
