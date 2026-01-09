using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR;
using Microsoft.Extensions.FileSystemGlobbing;
using GlobbingDirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;
using Spectre.Console;
using System.IO.Abstractions;
using System.Net;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Uploads files from the local machine to a connected remote server.
    /// </summary>
    [Command(Group = typeof(ServerGroup), Name = "upload")]
    [Description("Uploads files to the remote server")]
    public class UploadCommand : CommandBase
    {
        private readonly IServerProxy _proxy;
        private readonly FileTransferService _fileTransferService;
        private readonly IAnsiConsole _console;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Local file path or glob pattern to upload.
        /// When using glob patterns from external shells, quote the pattern to prevent shell expansion.
        /// Examples: myfile.txt, *.txt, "**/*.log"
        /// </summary>
        [Argument(Position = 0, Name = "source", IsRequired = true)]
        [Description("Local file path or glob pattern (quote patterns in external shells)")]
        public string Source { get; set; }

        /// <summary>
        /// Remote destination path. If path ends with '/', source filename is appended.
        /// </summary>
        [Argument(Position = 1, Name = "destination", IsRequired = true)]
        [Description("Remote destination path")]
        public string Destination { get; set; }

        /// <summary>
        /// Skip files that already exist on the server instead of overwriting.
        /// </summary>
        [Argument]
        [Alias('s')]
        [Description("Skip files that already exist on the server")]
        public Option SkipExisting { get; set; }

        public UploadCommand(
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
            // Verify connection state
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
            {
                _console.MarkupLine("[red]Not connected to server[/]");
                return;
            }

            // Expand source pattern to file list
            var (existingFiles, missingFiles) = ExpandSource(Source);

            // Handle missing files for literal paths
            if (missingFiles.Count > 0 && existingFiles.Count == 0 && !ContainsGlobCharacters(Source))
            {
                _console.MarkupLineInterpolated($"[red]File not found: {Source}[/]");
                return;
            }

            // Handle zero matches for glob patterns
            if (existingFiles.Count == 0)
            {
                _console.MarkupLineInterpolated($"[yellow]No files matched pattern: {Source}[/]");
                return;
            }

            // Check for skip-existing and filter files
            var filesToUpload = existingFiles.ToList();
            var skippedFiles = new List<string>();
            var oversizedFiles = new List<(string path, long size)>();

            if (SkipExisting.IsPresent)
            {
                try
                {
                    var existsOnServer = await _fileTransferService.CheckFilesExist(
                        Destination,
                        existingFiles.Select(f => _fileSystem.Path.GetFileName(f)).ToArray(),
                        ctx.CancellationToken);

                    filesToUpload = existingFiles
                        .Where(f => !existsOnServer.TryGetValue(_fileSystem.Path.GetFileName(f), out var exists) || !exists)
                        .ToList();

                    skippedFiles = existingFiles
                        .Where(f => existsOnServer.TryGetValue(_fileSystem.Path.GetFileName(f), out var exists) && exists)
                        .ToList();
                }
                catch (Exception ex)
                {
                    _console.MarkupLineInterpolated($"[yellow]Warning: Could not check existing files, uploading all: {ex.Message}[/]");
                    // Fall back to uploading all files
                }
            }

            // Filter out files that exceed the server's max file size limit
            var maxFileSize = _proxy.Server.MaxFileSizeBytes;
            if (maxFileSize > 0)
            {
                var validFiles = new List<string>();
                foreach (var file in filesToUpload)
                {
                    var fileInfo = _fileSystem.FileInfo.New(file);
                    if (fileInfo.Length > maxFileSize)
                    {
                        oversizedFiles.Add((file, fileInfo.Length));
                    }
                    else
                    {
                        validFiles.Add(file);
                    }
                }
                filesToUpload = validFiles;

                // Display warning for oversized files
                if (oversizedFiles.Count > 0)
                {
                    var maxSizeFormatted = ServerCapabilities.FormatFileSize(maxFileSize);
                    foreach (var (path, size) in oversizedFiles)
                    {
                        var fileName = _fileSystem.Path.GetFileName(path);
                        var fileSizeFormatted = ServerCapabilities.FormatFileSize(size);
                        _console.MarkupLineInterpolated($"[yellow]Skipped: {fileName} ({fileSizeFormatted}) exceeds server limit of {maxSizeFormatted}[/]");
                    }
                }
            }

            // If all files were filtered out, exit early
            if (filesToUpload.Count == 0)
            {
                if (oversizedFiles.Count > 0 && skippedFiles.Count == 0)
                {
                    _console.MarkupLine("[yellow]No files to upload - all files exceed the server's size limit[/]");
                }
                else if (skippedFiles.Count > 0)
                {
                    _console.MarkupLineInterpolated($"[yellow]No files to upload - {skippedFiles.Count} already exist on server[/]");
                }
                return;
            }

            // Upload files
            if (filesToUpload.Count == 1)
            {
                await UploadSingleFileAsync(filesToUpload[0], skippedFiles.Count, oversizedFiles.Count, ctx.CancellationToken);
            }
            else
            {
                await UploadMultipleFilesAsync(filesToUpload, skippedFiles, oversizedFiles.Count, ctx.CancellationToken);
            }
        }

        /// <summary>
        /// Expands a source path or glob pattern into matching files.
        /// </summary>
        internal (List<string> existing, List<string> missing) ExpandSource(string source)
        {
            var existing = new List<string>();
            var missing = new List<string>();

            // Check if it's a literal path (no glob characters)
            if (!ContainsGlobCharacters(source))
            {
                var fullPath = _fileSystem.Path.GetFullPath(source);
                if (_fileSystem.File.Exists(fullPath))
                {
                    existing.Add(fullPath);
                }
                else
                {
                    missing.Add(fullPath);
                }
                return (existing, missing);
            }

            // Parse glob pattern to extract base directory and pattern
            var (baseDir, pattern) = ParseGlobPattern(source);

            if (!_fileSystem.Directory.Exists(baseDir))
            {
                return (existing, missing);
            }

            // Note: Microsoft.Extensions.FileSystemGlobbing does NOT support ? wildcards
            // (see https://github.com/dotnet/runtime/issues/82406)
            // Workaround: Replace ? with * for the Matcher, then post-filter using regex
            var originalPattern = pattern;
            var matcherPattern = pattern.Replace('?', '*');
            
            var matcher = new Matcher();
            matcher.AddInclude(matcherPattern);

            var directoryInfo = new DirectoryInfo(baseDir);
            var result = matcher.Execute(new GlobbingDirectoryInfoWrapper(directoryInfo));

            // If pattern contained ?, apply regex filtering to enforce single-character match
            var regex = pattern.Contains('?') ? GlobPatternToRegex(originalPattern) : null;

            foreach (var file in result.Files)
            {
                var filePath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(baseDir, file.Path));
                
                // Apply regex filter if we had ? wildcards
                if (regex != null)
                {
                    var fileName = _fileSystem.Path.GetFileName(filePath);
                    if (!regex.IsMatch(fileName))
                        continue;
                }
                
                existing.Add(filePath);
            }

            return (existing, missing);
        }

        /// <summary>
        /// Parses a glob pattern to extract the base directory and pattern portion.
        /// </summary>
        internal (string baseDir, string pattern) ParseGlobPattern(string source)
        {
            // Normalize path separators
            var normalizedSource = source.Replace('\\', '/');
            var segments = normalizedSource.Split('/');

            var baseSegments = new List<string>();
            var patternSegments = new List<string>();
            var inPattern = false;

            foreach (var seg in segments)
            {
                if (inPattern || seg.Contains('*') || seg.Contains('?'))
                {
                    inPattern = true;
                    patternSegments.Add(seg);
                }
                else
                {
                    baseSegments.Add(seg);
                }
            }

            var baseDir = baseSegments.Count > 0
                ? string.Join(_fileSystem.Path.DirectorySeparatorChar.ToString(), baseSegments)
                : _fileSystem.Directory.GetCurrentDirectory();

            // Handle empty base dir for patterns like "*.txt"
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                baseDir = _fileSystem.Directory.GetCurrentDirectory();
            }

            var pattern = string.Join("/", patternSegments);

            return (baseDir, pattern);
        }

        private bool ContainsGlobCharacters(string path)
        {
            return path.Contains('*') || path.Contains('?');
        }

        /// <summary>
        /// Converts a glob pattern to a regex for post-filtering.
        /// Handles * (any characters) and ? (single character) wildcards.
        /// </summary>
        private static Regex GlobPatternToRegex(string pattern)
        {
            // Extract just the filename pattern (last segment)
            var segments = pattern.Replace('\\', '/').Split('/');
            var filePattern = segments[^1];
            
            // Escape regex special characters except our glob wildcards
            var regexPattern = Regex.Escape(filePattern)
                .Replace("\\*", ".*")    // * -> .* (any characters)
                .Replace("\\?", ".");     // ? -> . (single character)
            
            return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase);
        }

        internal string ResolveDestinationPath(string sourcePath)
        {
            // If destination ends with '/', append the source filename
            if (Destination.EndsWith('/') || Destination.EndsWith('\\'))
            {
                return Destination.TrimEnd('/', '\\') + "/" + _fileSystem.Path.GetFileName(sourcePath);
            }
            return Destination;
        }

        private async Task UploadSingleFileAsync(string filePath, int skippedCount, int oversizedCount, CancellationToken ct)
        {
            var fileName = _fileSystem.Path.GetFileName(filePath);
            var destPath = ResolveDestinationPath(filePath);
            var fileSize = _fileSystem.FileInfo.New(filePath).Length;
            var showProgress = fileSize >= UploadConstants.ProgressDisplayThreshold;
            var skipIfExists = SkipExisting.IsPresent;

            try
            {
                if (showProgress)
                {
                    await _console.Progress()
                        .Columns(
                            new TaskDescriptionColumn(),
                            new ProgressBarColumn(),
                            new PercentageColumn(),
                            new SpinnerColumn())
                        .StartAsync(async ctx =>
                        {
                            var task = ctx.AddTask(fileName);
                            task.MaxValue = 100;

                            var result = await _fileTransferService.UploadFile(
                                filePath,
                                destPath,
                                progress =>
                                {
                                    var percentage = (double)progress.TotalRead / fileSize * 100;
                                    task.Value = percentage;
                                    return Task.CompletedTask;
                                },
                                ct,
                                skipIfExists);

                            if (result?.Status == UploadConstants.StatusSkipped)
                            {
                                task.Description = $"{fileName} [yellow]Skipped (server)[/]";
                            }
                            else
                            {
                                task.Value = 100;
                                task.Description = $"{fileName} [green]Completed[/]";
                            }
                        });
                }
                else
                {
                    var result = await _fileTransferService.UploadFile(filePath, destPath, null, ct, skipIfExists);
                    
                    if (result?.Status == UploadConstants.StatusSkipped)
                    {
                        _console.MarkupLineInterpolated($"[yellow]Skipped (exists): {fileName}[/]");
                        skippedCount++;
                    }
                }

                if (skippedCount > 0 || oversizedCount > 0)
                {
                    var totalSkipped = skippedCount + oversizedCount;
                    var skipParts = new List<string>();
                    if (skippedCount > 0) skipParts.Add($"{skippedCount} already exist");
                    if (oversizedCount > 0) skipParts.Add($"{oversizedCount} too large");
                    var skipSummary = skipParts.Count > 0 ? $" ({string.Join(", ", skipParts)})" : "";
                    _console.MarkupLineInterpolated($"Uploaded 1 file to {Destination}. {totalSkipped} skipped{skipSummary}.");
                }
                else
                {
                    _console.MarkupLineInterpolated($"Uploaded {fileName} to {destPath}");
                }
            }
            catch (FileNotFoundException)
            {
                _console.MarkupLineInterpolated($"[red]File not found: {filePath}[/]");
            }
            catch (UnauthorizedAccessException ex)
            {
                _console.MarkupLineInterpolated($"[red]Permission denied: {ex.Message}[/]");
            }
            catch (HttpRequestException ex)
            {
                _console.MarkupLineInterpolated($"[red]Upload failed: {GetFriendlyErrorMessage(ex)}[/]");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("disconnected"))
            {
                _console.MarkupLine("[red]Connection lost during upload[/]");
            }
            catch (RemoteMessagingException)
            {
                _console.MarkupLine("[red]Connection lost during upload[/]");
            }
        }

        private async Task UploadMultipleFilesAsync(List<string> files, List<string> alreadySkipped, int oversizedCount, CancellationToken ct)
        {
            var successCount = 0;
            var failureCount = 0;
            var skippedCount = alreadySkipped.Count;
            var serverSkippedCount = 0;
            var failedFiles = new List<(string path, string error)>();
            var notFoundFiles = new List<string>();
            var skipIfExists = SkipExisting.IsPresent;

            var semaphore = new SemaphoreSlim(UploadConstants.MaxConcurrentUploads);

            await _console.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    // Create all tasks upfront as "Pending"
                    var uploadTasks = files.Select(f =>
                    {
                        var fileName = _fileSystem.Path.GetFileName(f);
                        var task = ctx.AddTask($"{fileName} [grey]Pending[/]");
                        task.MaxValue = 100;
                        return (filePath: f, progressTask: task);
                    }).ToList();

                    var tasks = uploadTasks.Select(async item =>
                    {
                        await semaphore.WaitAsync(ct);
                        try
                        {
                            var fileName = _fileSystem.Path.GetFileName(item.filePath);
                            var destPath = ResolveDestinationPath(item.filePath);
                            var fileSize = _fileSystem.FileInfo.New(item.filePath).Length;

                            item.progressTask.Description = fileName;

                            var result = await _fileTransferService.UploadFile(
                                item.filePath,
                                destPath,
                                progress =>
                                {
                                    var percentage = fileSize > 0 ? (double)progress.TotalRead / fileSize * 100 : 100;
                                    item.progressTask.Value = percentage;
                                    return Task.CompletedTask;
                                },
                                ct,
                                skipIfExists);

                            if (result?.Status == UploadConstants.StatusSkipped)
                            {
                                Interlocked.Increment(ref serverSkippedCount);
                                item.progressTask.Description = $"{fileName} [yellow]Skipped (server)[/]";
                            }
                            else
                            {
                                Interlocked.Increment(ref successCount);
                                item.progressTask.Value = 100;
                                item.progressTask.Description = $"{fileName} [green]Completed[/]";
                            }
                        }
                        catch (FileNotFoundException)
                        {
                            var fileName = _fileSystem.Path.GetFileName(item.filePath);
                            lock (notFoundFiles)
                            {
                                notFoundFiles.Add(item.filePath);
                            }
                            item.progressTask.Description = $"{fileName} [red]Not Found[/]";
                            item.progressTask.Value = 100;
                        }
                        catch (Exception ex)
                        {
                            var fileName = _fileSystem.Path.GetFileName(item.filePath);
                            Interlocked.Increment(ref failureCount);
                            lock (failedFiles)
                            {
                                var errorMessage = ex is HttpRequestException httpEx 
                                    ? GetFriendlyErrorMessage(httpEx) 
                                    : ex.Message;
                                failedFiles.Add((item.filePath, errorMessage));
                            }
                            item.progressTask.Description = $"{fileName} [red]Failed[/]";
                            item.progressTask.Value = 100;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                });

            // Output summary
            var totalSkipped = skippedCount + serverSkippedCount;
            var totalAttempted = files.Count + alreadySkipped.Count + oversizedCount;

            // Build skip summary parts
            var skipParts = new List<string>();
            if (totalSkipped > 0)
            {
                skipParts.Add($"{totalSkipped} already exist");
            }
            if (oversizedCount > 0)
            {
                skipParts.Add($"{oversizedCount} too large");
            }
            var skipSummary = skipParts.Count > 0 ? $" ({string.Join(", ", skipParts)})" : "";

            if (failureCount == 0 && notFoundFiles.Count == 0)
            {
                if (totalSkipped > 0 || oversizedCount > 0)
                {
                    _console.MarkupLineInterpolated($"Uploaded {successCount} files to {Destination}. {totalSkipped + oversizedCount} skipped{skipSummary}.");
                }
                else
                {
                    _console.MarkupLineInterpolated($"Uploaded {successCount} files to {Destination}");
                }
            }
            else
            {
                _console.MarkupLineInterpolated($"[yellow]Uploaded {successCount} of {files.Count} files to {Destination}[/]");

                if (notFoundFiles.Count > 0)
                {
                    _console.MarkupLineInterpolated($"[red]{notFoundFiles.Count} files not found: {string.Join(", ", notFoundFiles.Select(f => _fileSystem.Path.GetFileName(f)))}[/]");
                }

                foreach (var (path, error) in failedFiles)
                {
                    _console.MarkupLineInterpolated($"[red]Failed: {_fileSystem.Path.GetFileName(path)} - {error}[/]");
                }

                if (totalSkipped > 0 || oversizedCount > 0)
                {
                    _console.MarkupLineInterpolated($"{totalSkipped + oversizedCount} skipped{skipSummary}.");
                }
            }
        }

        /// <summary>
        /// Converts an HttpRequestException into a user-friendly error message.
        /// </summary>
        private static string GetFriendlyErrorMessage(HttpRequestException ex)
        {
            // Check for specific status codes first
            if (ex.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
            {
                return "File exceeds the server's maximum file size limit";
            }

            // Check for stream copying errors (often caused by server rejecting upload mid-stream)
            if (ex.Message.Contains("copying content to a stream", StringComparison.OrdinalIgnoreCase))
            {
                // This often happens when the server closes the connection due to file size
                if (ex.InnerException != null)
                {
                    // Check if inner exception hints at the cause
                    var innerMsg = ex.InnerException.Message;
                    if (innerMsg.Contains("connection was closed", StringComparison.OrdinalIgnoreCase) ||
                        innerMsg.Contains("aborted", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Upload was rejected by the server. The file may exceed the server's size limit.";
                    }
                }
                return "Upload was interrupted. The file may exceed the server's size limit.";
            }

            // For other HttpRequestExceptions, return the original message
            return ex.Message;
        }
    }
}
