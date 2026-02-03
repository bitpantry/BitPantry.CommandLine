using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using Spectre.Console;
using System.Collections.Concurrent;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    /// <summary>
    /// Downloads files from a connected remote server to the local machine.
    /// </summary>
    [InGroup<ServerGroup>]
    [Command(Name = "download")]
    [Description("Downloads files from the remote server")]
    public class DownloadCommand : CommandBase
    {
        private readonly IServerProxy _proxy;
        private readonly FileTransferService _fileTransferService;
        private readonly IAnsiConsole _console;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Remote file path or glob pattern to download.
        /// When using glob patterns from external shells, quote the pattern to prevent shell expansion.
        /// Examples: myfile.txt, *.txt, "**/*.log"
        /// </summary>
        [Argument(Position = 0, Name = "source", IsRequired = true)]
        [Description("Remote file path or glob pattern (quote patterns in external shells)")]
        public string Source { get; set; }

        /// <summary>
        /// Local destination path. If path ends with '/' or '\', source filename is appended.
        /// </summary>
        [Argument(Position = 1, Name = "destination", IsRequired = true)]
        [Description("Local destination path")]
        public string Destination { get; set; }

        public DownloadCommand(
            IServerProxy proxy,
            FileTransferService fileTransferService,
            IAnsiConsole console,
            IFileSystem fileSystem)
        {
            _proxy = proxy;
            _fileTransferService = fileTransferService;
            _console = console;
            _fileSystem = fileSystem;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // 1. Validate source pattern
            var validationResult = GlobPatternHelper.ValidatePattern(Source);
            if (!validationResult.IsValid)
            {
                _console.MarkupLine($"[red]Invalid source pattern:[/] {Markup.Escape(validationResult.ErrorMessage!)}");
                _console.MarkupLine($"[yellow]{Markup.Escape(validationResult.SuggestedFormat!)}[/]");
                return;
            }

            // 2. Verify connection
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
            {
                _console.MarkupLine("[red]Not connected to server[/]");
                return;
            }

            // 3. Check if source is literal path or glob pattern
            if (IsLiteralPath(Source))
            {
                // Single file download
                await DownloadSingleFile(Source, ctx.CancellationToken);
            }
            else
            {
                // Glob pattern download
                await DownloadWithPattern(Source, ctx.CancellationToken);
            }
        }

        /// <summary>
        /// Downloads a single file from the remote server with optional progress display.
        /// </summary>
        private async Task DownloadSingleFile(string remotePath, CancellationToken cancellationToken)
        {
            try
            {
                var localPath = ResolveLocalPath(remotePath);
                var fileName = _fileSystem.Path.GetFileName(remotePath);

                // For single file downloads, we don't know the size upfront without an additional RPC call.
                // We'll use the progress callback which gets total size from Content-Length header.
                // Show progress for all files - the callback will update the total when we receive it.
                
                await _console.Progress()
                    .AutoClear(true)
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new TransferSpeedColumn(),
                        new SpinnerColumn())
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask($"Downloading {fileName}");
                        task.IsIndeterminate = true; // Start indeterminate until we know size

                        await _fileTransferService.DownloadFile(
                            remotePath,
                            localPath,
                            progress =>
                            {
                                if (progress.TotalSize > 0 && task.MaxValue != progress.TotalSize)
                                {
                                    task.MaxValue = progress.TotalSize;
                                    task.IsIndeterminate = false;
                                }
                                task.Value = progress.TotalRead;
                                return Task.CompletedTask;
                            },
                            cancellationToken);

                        task.Value = task.MaxValue > 0 ? task.MaxValue : 100;
                    });

                _console.MarkupLine($"[green]Downloaded[/] {Markup.Escape(remotePath)} [green]to[/] {Markup.Escape(localPath)}");
            }
            catch (FileNotFoundException ex)
            {
                _console.MarkupLine($"[red]File not found:[/] {Markup.Escape(ex.Message)}");
            }
            catch (UnauthorizedAccessException ex)
            {
                _console.MarkupLine($"[red]Permission denied:[/] {Markup.Escape(ex.Message)}");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("disconnected"))
            {
                _console.MarkupLine("[red]Connection lost during download[/]");
            }
            catch (RemoteMessagingException)
            {
                _console.MarkupLine("[red]Connection lost during download[/]");
            }
            catch (InvalidDataException ex)
            {
                _console.MarkupLine($"[red]Checksum verification failed:[/] {Markup.Escape(ex.Message)}");
            }
            catch (IOException ex) when (ex.HResult == unchecked((int)0x80070070) || ex.HResult == unchecked((int)0x80070027))
            {
                // ERROR_DISK_FULL (0x70) or ERROR_HANDLE_DISK_FULL (0x27)
                _console.MarkupLine($"[red]Disk space error:[/] {Markup.Escape(ex.Message)}");
            }
            catch (PathTooLongException ex)
            {
                _console.MarkupLine($"[red]Path too long:[/] {Markup.Escape(ex.Message)}");
            }
            catch (NotSupportedException ex)
            {
                // Invalid filename characters (e.g., :, <, >, |, ?, * on Windows, / on Linux)
                _console.MarkupLine($"[red]Invalid filename:[/] {Markup.Escape(ex.Message)}");
            }
            catch (Exception ex)
            {
                _console.MarkupLine($"[red]Download failed:[/] {Markup.Escape(ex.Message)}");
            }
        }

        /// <summary>
        /// Downloads multiple files matching a glob pattern with aggregate progress.
        /// </summary>
        private async Task DownloadWithPattern(string pattern, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Expand the pattern to get matching files
                var files = await ExpandSourcePattern(pattern, cancellationToken);

                if (files.Count == 0)
                {
                    _console.MarkupLine($"[yellow]No files matched pattern:[/] {Markup.Escape(pattern)}");
                    return;
                }

                // 2. Detect filename collisions (multiple files with same name in different directories)
                var collisions = DetectCollisions(files);
                if (collisions.Count > 0)
                {
                    _console.MarkupLine("[red]Error: Filename collisions detected. The following files have the same name:[/]");
                    foreach (var collision in collisions)
                    {
                        _console.MarkupLine($"[red]  {Markup.Escape(collision.FileName)}:[/]");
                        foreach (var path in collision.Paths)
                        {
                            _console.MarkupLine($"[red]    - {Markup.Escape(path)}[/]");
                        }
                    }
                    return;
                }

                // 3. Calculate total size for aggregate progress
                var totalSize = files.Sum(f => f.Size);
                var showProgress = totalSize >= DownloadConstants.ProgressDisplayThreshold;

                var successCount = 0;
                var failedFiles = new List<(string Path, string Error)>();

                if (showProgress)
                {
                    // Track per-file progress for delta calculation
                    var lastProgressPerFile = new ConcurrentDictionary<string, long>();
                    long totalBytesDownloaded = 0;

                    await _console.Progress()
                        .AutoClear(true)
                        .Columns(
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new TransferSpeedColumn(),
                            new SpinnerColumn())
                        .StartAsync(async ctx =>
                        {
                            var progressTask = ctx.AddTask($"Downloading to {Destination}");
                            progressTask.MaxValue = totalSize;

                            // Use SemaphoreSlim for concurrent downloads with throttling
                            var semaphore = new SemaphoreSlim(DownloadConstants.MaxConcurrentDownloads);
                            var downloadTasks = files.Select(async file =>
                            {
                                await semaphore.WaitAsync(cancellationToken);
                                try
                                {
                                    var localPath = ResolveLocalPath(file.Path);
                                    progressTask.Description = $"Downloading: {_fileSystem.Path.GetFileName(file.Path)}";

                                    await _fileTransferService.DownloadFile(
                                        file.Path,
                                        localPath,
                                        progress =>
                                        {
                                            // Calculate delta from last progress report
                                            var lastValue = lastProgressPerFile.GetOrAdd(file.Path, 0L);
                                            var delta = progress.TotalRead - lastValue;
                                            if (delta > 0)
                                            {
                                                lastProgressPerFile[file.Path] = progress.TotalRead;
                                                Interlocked.Add(ref totalBytesDownloaded, delta);
                                                progressTask.Value = Interlocked.Read(ref totalBytesDownloaded);
                                            }
                                            return Task.CompletedTask;
                                        },
                                        cancellationToken);

                                    Interlocked.Increment(ref successCount);
                                }
                                catch (Exception ex)
                                {
                                    lock (failedFiles)
                                    {
                                        failedFiles.Add((file.Path, ex.Message));
                                    }
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            }).ToArray();

                            await Task.WhenAll(downloadTasks);
                            progressTask.Value = totalSize;
                        });
                }
                else
                {
                    // No progress bar - simple sequential download
                    foreach (var file in files)
                    {
                        try
                        {
                            var localPath = ResolveLocalPath(file.Path);
                            await _fileTransferService.DownloadFile(file.Path, localPath, null, cancellationToken);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            failedFiles.Add((file.Path, ex.Message));
                        }
                    }
                }

                // 4. Display summary
                if (failedFiles.Count == 0)
                {
                    _console.MarkupLine($"[green]Downloaded {successCount} file(s) to[/] {Markup.Escape(Destination.TrimEnd('/', '\\'))}");
                }
                else
                {
                    _console.MarkupLine($"[yellow]Downloaded {successCount} of {files.Count} files[/]");
                    foreach (var failed in failedFiles)
                    {
                        _console.MarkupLine($"[red]  Failed: {Markup.Escape(failed.Path)} - {Markup.Escape(failed.Error)}[/]");
                    }
                }
            }
            catch (Exception ex)
            {
                _console.MarkupLine($"[red]Download failed:[/] {Markup.Escape(ex.Message)}");
            }
        }

        /// <summary>
        /// Expands a glob pattern to a list of matching files on the server.
        /// </summary>
        /// <param name="pattern">The glob pattern to expand.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of matching file info entries with full server-relative paths.</returns>
        public async Task<IReadOnlyList<FileInfoEntry>> ExpandSourcePattern(string pattern, CancellationToken cancellationToken)
        {
            // Parse the pattern to get base directory and pattern
            var (baseDir, searchPattern) = GlobPatternHelper.ParseGlobPattern(pattern, _fileSystem);

            // Normalize baseDir to forward slashes for server-side paths
            // The server always expects forward slashes regardless of client platform
            baseDir = baseDir.Replace('\\', '/');

            // Determine if recursive search is needed
            var recursive = pattern.Contains("**");

            // Call server to enumerate files
            var files = await _fileTransferService.EnumerateFiles(baseDir, searchPattern, recursive, cancellationToken);

            // If pattern contains ?, apply regex post-filtering (FileSystemGlobbing doesn't support ?)
            files = GlobPatternHelper.ApplyQuestionMarkFilter(
                files, 
                searchPattern, 
                f => _fileSystem.Path.GetFileName(f.Path)).ToArray();

            // The server returns relative paths from the search root.
            // Convert to full server-relative paths by prepending the base directory.
            files = files.Select(f => new FileInfoEntry(
                GlobPatternHelper.ReconstructFullPath(baseDir, f.Path),
                f.Size,
                f.LastModified)).ToArray();

            return files;
        }

        /// <summary>
        /// Detects filename collisions in a list of files.
        /// Multiple files with the same filename (from different directories) would overwrite each other.
        /// </summary>
        /// <param name="files">The list of files to check.</param>
        /// <returns>List of collision groups where multiple paths share the same filename.</returns>
        public IReadOnlyList<CollisionGroup> DetectCollisions(IReadOnlyList<FileInfoEntry> files)
        {
            var collisions = files
                .GroupBy(f => _fileSystem.Path.GetFileName(f.Path), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => new CollisionGroup(g.Key, g.Select(f => f.Path).ToList()))
                .ToList();

            return collisions;
        }

        /// <summary>
        /// Resolves the local file path based on the destination and remote file name.
        /// If destination ends with '/' or '\', appends the source filename.
        /// Otherwise, uses the destination as-is.
        /// </summary>
        /// <param name="remotePath">The remote file path</param>
        /// <returns>The resolved local file path</returns>
        public string ResolveLocalPath(string remotePath)
        {
            var fileName = _fileSystem.Path.GetFileName(remotePath);
            var resolved = GlobPatternHelper.ResolveDestinationPath(Destination, fileName);
            
            // Use platform-appropriate path combination for local paths
            if (Destination.EndsWith('/') || Destination.EndsWith('\\'))
            {
                return _fileSystem.Path.Combine(Destination.TrimEnd('/', '\\'), fileName);
            }
            return Destination;
        }

        /// <summary>
        /// Determines if the path is a literal path (no glob characters) or a pattern.
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path is literal (no wildcards), false if it contains glob patterns</returns>
        public bool IsLiteralPath(string path)
        {
            return !GlobPatternHelper.ContainsGlobCharacters(path);
        }
    }

    /// <summary>
    /// Represents a group of files with the same filename that would collide on download.
    /// </summary>
    public record CollisionGroup(string FileName, IReadOnlyList<string> Paths);
}

