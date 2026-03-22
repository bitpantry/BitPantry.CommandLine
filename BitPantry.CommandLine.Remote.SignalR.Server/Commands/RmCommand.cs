using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using Microsoft.Extensions.FileSystemGlobbing;
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
        internal const int ConfirmationThreshold = 4;

        private readonly IFileSystem _fileSystem;
        private readonly FileTransferOptions _options;

        [Argument(Position = 0, Name = "path", IsRequired = true)]
        [Description("Path to remove")]
        [ServerFilePathAutoComplete]
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

        public RmCommand(IFileSystem fileSystem) : this(fileSystem, null)
        {
        }

        public RmCommand(IFileSystem fileSystem, FileTransferOptions options)
        {
            _fileSystem = fileSystem;
            _options = options;
        }

        public async Task Execute(CommandExecutionContext context)
        {
            try
            {
                if (IsStorageRoot(Path))
                {
                    Console.MarkupLine("[red]Cannot delete the server storage root directory.[/]");
                    return;
                }

                if (ContainsGlobCharacters(Path))
                {
                    await ExecuteGlob();
                    return;
                }

                await ExecuteSingle();
            }
            catch (UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Path}[/]");
            }

            await Task.CompletedTask;
        }

        private async Task ExecuteSingle()
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

        private async Task ExecuteGlob()
        {
            var (baseDir, pattern) = ParseGlobPattern(Path);

            if (!_fileSystem.Directory.Exists(baseDir))
            {
                if (Force)
                    return;

                Console.MarkupLine($"[red]Path not found: {baseDir}[/]");
                return;
            }

            var matcher = new Matcher();
            matcher.AddInclude(pattern);

            var allFiles = _fileSystem.Directory.GetFiles(baseDir);
            var matchedFiles = new List<string>();
            foreach (var file in allFiles)
            {
                var fileName = _fileSystem.Path.GetFileName(file);
                if (matcher.Match(fileName).HasMatches)
                    matchedFiles.Add(file);
            }

            if (matchedFiles.Count == 0)
            {
                if (!Force)
                    Console.MarkupLine($"[red]No files matching: {Path}[/]");
                return;
            }

            if (!Force && matchedFiles.Count >= ConfirmationThreshold)
            {
                var confirmed = Console.Prompt(
                    new ConfirmationPrompt($"Delete {matchedFiles.Count} files?"));
                if (!confirmed)
                    return;
            }

            foreach (var file in matchedFiles)
            {
                _fileSystem.File.Delete(file);
            }

            Console.MarkupLine($"[green]Removed {matchedFiles.Count} file(s)[/]");

            await Task.CompletedTask;
        }

        private static bool ContainsGlobCharacters(string path)
        {
            return path.Contains('*') || path.Contains('?');
        }

        private (string baseDir, string pattern) ParseGlobPattern(string source)
        {
            var normalized = source.Replace('\\', '/');
            var lastSlash = normalized.LastIndexOf('/');
            if (lastSlash < 0)
                return (".", source);

            var baseDir = source.Substring(0, lastSlash);
            var pattern = source.Substring(lastSlash + 1);
            return (baseDir, pattern);
        }

        private bool IsStorageRoot(string path)
        {
            if (_options?.StorageRootPath == null)
                return false;

            var resolvedPath = _fileSystem.Path.GetFullPath(path).TrimEnd('\\', '/');
            var resolvedRoot = _fileSystem.Path.GetFullPath(_options.StorageRootPath).TrimEnd('\\', '/');
            return string.Equals(resolvedPath, resolvedRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
