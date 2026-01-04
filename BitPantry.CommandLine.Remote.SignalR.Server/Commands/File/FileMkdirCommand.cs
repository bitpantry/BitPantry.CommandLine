using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using Spectre.Console;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands.File
{
    /// <summary>
    /// Creates a directory in the remote server's sandboxed file system.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "mkdir")]
    [Description("Creates a new directory in the remote file system")]
    public class FileMkdirCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, IsRequired = true)]
        [Alias('p')]
        [Description("The path of the directory to create")]
        [DirectoryPathCompletion]
        public string Path { get; set; } = string.Empty;

        [Argument]
        [Description("Create parent directories as needed")]
        public Option Parents { get; set; }

        public FileMkdirCommand(IFileSystem fileSystem)
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

            if (_fileSystem.Directory.Exists(targetPath))
            {
                Console.MarkupLine($"[yellow]Directory already exists: {Markup.Escape(targetPath)}[/]");
                return;
            }

            if (_fileSystem.File.Exists(targetPath))
            {
                Console.MarkupLine($"[red]A file with that name already exists: {Markup.Escape(targetPath)}[/]");
                return;
            }

            try
            {
                // CreateDirectory already creates parent directories automatically
                // The -p/--parents flag is for compatibility with Unix mkdir
                _fileSystem.Directory.CreateDirectory(targetPath);
                Console.MarkupLine($"[green]Created directory: {Markup.Escape(targetPath)}[/]");
            }
            catch (UnauthorizedAccessException)
            {
                Console.MarkupLine($"[red]Access denied: {Markup.Escape(targetPath)}[/]");
            }
            catch (IOException ex)
            {
                Console.MarkupLine($"[red]Error creating directory: {Markup.Escape(ex.Message)}[/]");
            }
        }
    }
}
