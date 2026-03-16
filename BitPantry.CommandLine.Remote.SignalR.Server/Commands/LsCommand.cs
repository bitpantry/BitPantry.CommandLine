using BitPantry.CommandLine.API;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Spectre.Console;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands
{
    [Command(Name = "ls")]
    [InGroup<ServerGroup>]
    [Description("Lists directory contents on the remote server")]
    public class LsCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, Name = "path")]
        [Description("Directory path to list")]
        public string Path { get; set; } = ".";

        [Argument(Name = "long"), Flag, Alias('l')]
        [Description("Use long listing format with details")]
        public bool Long { get; set; }

        [Argument(Name = "all"), Flag, Alias('a')]
        [Description("Include hidden entries")]
        public bool All { get; set; }

        [Argument(Name = "recursive"), Flag]
        [Description("List contents recursively, including subdirectories")]
        public bool Recursive { get; set; }

        [Argument(Name = "sort")]
        [Description("Sort results by: name (default), size, modified")]
        public string Sort { get; set; } = "name";

        [Argument(Name = "reverse"), Flag]
        [Description("Reverse the sort order")]
        public bool Reverse { get; set; }

        public LsCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task Execute(CommandExecutionContext context)
        {
            try
            {
                var (dirPath, pattern) = ParsePathAndPattern(Path);

                // If path points to a file, list that single file
                if (_fileSystem.File.Exists(dirPath))
                {
                    Console.WriteLine(_fileSystem.Path.GetFileName(dirPath));
                    return;
                }

                if (!_fileSystem.Directory.Exists(dirPath))
                {
                    Console.MarkupLine($"[red]Directory not found: {Path}[/]");
                    return;
                }

                IEnumerable<string> entries;

                if (pattern != null)
                {
                    entries = _fileSystem.Directory.GetFiles(dirPath)
                        .Where(f => MatchesGlob(_fileSystem.Path.GetFileName(f), pattern));
                }
                else if (Recursive)
                {
                    entries = _fileSystem.Directory.GetFileSystemEntries(dirPath, "*", SearchOption.AllDirectories);
                }
                else
                {
                    entries = _fileSystem.Directory.GetFileSystemEntries(dirPath);
                }

                var sorted = ApplySort(entries).ToList();

                if (sorted.Count == 0 && pattern != null)
                {
                    Console.MarkupLine($"[yellow]No files matching: {Path}[/]");
                    return;
                }

                if (Long)
                {
                    var table = new Table();
                    table.AddColumn("Type");
                    table.AddColumn("Name");
                    table.AddColumn("Size");
                    table.AddColumn("Last Modified");

                    foreach (var entry in sorted)
                    {
                        var name = _fileSystem.Path.GetFileName(entry);
                        var isDir = _fileSystem.Directory.Exists(entry);

                        var type = isDir ? "DIR" : "FILE";
                        var displayName = isDir ? $"{name}/" : name;
                        var size = isDir ? "\u2014" : FormatFileSize(_fileSystem.FileInfo.New(entry).Length);
                        var modified = _fileSystem.FileInfo.New(entry).LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm");

                        table.AddRow(type, displayName, size, modified);
                    }

                    Console.Write(table);
                }
                else
                {
                    foreach (var entry in sorted)
                    {
                        string displayName;
                        if (Recursive)
                        {
                            var relativePath = _fileSystem.Path.GetRelativePath(dirPath, entry);
                            displayName = _fileSystem.Directory.Exists(entry) ? $"{relativePath}/" : relativePath;
                        }
                        else
                        {
                            var name = _fileSystem.Path.GetFileName(entry);
                            displayName = _fileSystem.Directory.Exists(entry) ? $"{name}/" : name;
                        }
                        Console.WriteLine(displayName);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Path}[/]");
            }

            await Task.CompletedTask;
        }

        private IEnumerable<string> ApplySort(IEnumerable<string> entries)
        {
            IOrderedEnumerable<string> ordered = Sort?.ToLowerInvariant() switch
            {
                "size" => Reverse
                    ? entries.OrderByDescending(e => _fileSystem.FileInfo.New(e).Length)
                    : entries.OrderBy(e => _fileSystem.FileInfo.New(e).Length),
                "modified" => Reverse
                    ? entries.OrderByDescending(e => _fileSystem.FileInfo.New(e).LastWriteTimeUtc)
                    : entries.OrderBy(e => _fileSystem.FileInfo.New(e).LastWriteTimeUtc),
                _ => Reverse
                    ? entries.OrderByDescending(e => e)
                    : entries.OrderBy(e => e),
            };
            return ordered;
        }

        private (string dirPath, string pattern) ParsePathAndPattern(string path)
        {
            var fileName = _fileSystem.Path.GetFileName(path);
            if (fileName != null && ContainsGlobChars(fileName))
            {
                var dir = _fileSystem.Path.GetDirectoryName(path);
                return (string.IsNullOrEmpty(dir) ? "." : dir, fileName);
            }

            return (path, null);
        }

        private static bool ContainsGlobChars(string value)
            => value.Contains('*') || value.Contains('?') || value.Contains('[');

        private static bool MatchesGlob(string fileName, string pattern)
        {
            var matcher = new Matcher();
            matcher.AddInclude(pattern);
            return matcher.Match(fileName).HasMatches;
        }

        private static string FormatFileSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            return unitIndex == 0 ? $"{size:0} {units[unitIndex]}" : $"{size:0.0} {units[unitIndex]}";
        }
    }
}
