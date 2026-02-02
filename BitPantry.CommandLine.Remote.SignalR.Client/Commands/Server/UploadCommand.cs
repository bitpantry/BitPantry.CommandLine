using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR;
using Microsoft.Extensions.FileSystemGlobbing;
using GlobbingDirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;
using Spectre.Console;
using System.IO.Abstractions;
using System.Net;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
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
        [Flag]
        [Description("Skip files that already exist on the server")]
        public bool SkipExisting { get; set; }

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
            if (missingFiles.Count > 0 && existingFiles.Count == 0 && !GlobPatternHelper.ContainsGlobCharacters(Source))
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

            if (SkipExisting)
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
            if (!GlobPatternHelper.ContainsGlobCharacters(source))
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
            var (baseDir, pattern) = GlobPatternHelper.ParseGlobPattern(source, _fileSystem);

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

            // Collect all matching files
            var matchedFiles = result.Files
                .Select(file => _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(baseDir, file.Path)))
                .ToList();

            // Apply ? wildcard filtering using shared helper
            var filteredFiles = GlobPatternHelper.ApplyQuestionMarkFilter(
                matchedFiles,
                originalPattern,
                _fileSystem.Path.GetFileName);

            existing.AddRange(filteredFiles);

            return (existing, missing);
        }

        /// <summary>
        /// Resolves the destination path by appending the source filename if destination ends with a separator.
        /// </summary>
        internal string ResolveDestinationPath(string sourcePath)
        {
            var fileName = _fileSystem.Path.GetFileName(sourcePath);
            return GlobPatternHelper.ResolveDestinationPath(Destination, fileName);
        }

        private async Task UploadSingleFileAsync(string filePath, int skippedCount, int oversizedCount, CancellationToken ct)
        {
            var fileName = _fileSystem.Path.GetFileName(filePath);
            var destPath = ResolveDestinationPath(filePath);
            var fileSize = _fileSystem.FileInfo.New(filePath).Length;
            var showProgress = fileSize >= UploadConstants.ProgressDisplayThreshold;
            var skipIfExists = SkipExisting;
            var wasSkipped = false;

            try
            {
                if (showProgress)
                {
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
                            var task = ctx.AddTask($"Uploading {fileName}");
                            task.MaxValue = fileSize;

                            var result = await _fileTransferService.UploadFile(
                                filePath,
                                destPath,
                                progress =>
                                {
                                    task.Value = progress.TotalRead;
                                    return Task.CompletedTask;
                                },
                                ct,
                                skipIfExists);

                            if (result?.Status == UploadConstants.StatusSkipped)
                            {
                                wasSkipped = true;
                            }
                            else
                            {
                                task.Value = fileSize;
                            }
                        });
                }
                else
                {
                    var result = await _fileTransferService.UploadFile(filePath, destPath, null, ct, skipIfExists);
                    
                    if (result?.Status == UploadConstants.StatusSkipped)
                    {
                        wasSkipped = true;
                    }
                }

                // Always show clean summary at the end
                if (wasSkipped)
                {
                    skippedCount++;
                }

                if (skippedCount > 0 || oversizedCount > 0)
                {
                    var totalSkipped = skippedCount + oversizedCount;
                    var skipParts = new List<string>();
                    if (skippedCount > 0) skipParts.Add($"{skippedCount} already exist");
                    if (oversizedCount > 0) skipParts.Add($"{oversizedCount} too large");
                    var skipSummary = $" ({string.Join(", ", skipParts)})";
                    
                    if (wasSkipped)
                    {
                        _console.MarkupLineInterpolated($"Uploaded 0 files to {Destination}. {totalSkipped} skipped{skipSummary}.");
                    }
                    else
                    {
                        _console.MarkupLineInterpolated($"Uploaded {fileName} to {destPath}. {totalSkipped} skipped{skipSummary}.");
                    }
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
            var skipIfExists = SkipExisting;

            // Calculate total size for aggregate progress
            var fileSizes = files.ToDictionary(f => f, f => _fileSystem.FileInfo.New(f).Length);
            var totalSize = fileSizes.Values.Sum();
            var showProgress = totalSize >= UploadConstants.ProgressDisplayThreshold;

            var semaphore = new SemaphoreSlim(UploadConstants.MaxConcurrentUploads);
            long totalBytesUploaded = 0;
            string currentFile = "";
            var lastProgressPerFile = new System.Collections.Concurrent.ConcurrentDictionary<string, long>();

            if (showProgress)
            {
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
                        var progressTask = ctx.AddTask($"Uploading to {Destination}");
                        progressTask.MaxValue = totalSize;

                        var uploadTasks = files.Select(async filePath =>
                        {
                            await semaphore.WaitAsync(ct);
                            try
                            {
                                var fileName = _fileSystem.Path.GetFileName(filePath);
                                var destPath = ResolveDestinationPath(filePath);
                                var fileSize = fileSizes[filePath];

                                // Update current file being uploaded
                                Interlocked.Exchange(ref currentFile, fileName);
                                progressTask.Description = $"Uploading: {currentFile}";

                                var result = await _fileTransferService.UploadFile(
                                    filePath,
                                    destPath,
                                    progress =>
                                    {
                                        // TotalRead is cumulative for this file, so compute delta
                                        var lastValue = lastProgressPerFile.GetOrAdd(filePath, 0L);
                                        var delta = progress.TotalRead - lastValue;
                                        if (delta > 0)
                                        {
                                            lastProgressPerFile[filePath] = progress.TotalRead;
                                            var newTotal = Interlocked.Add(ref totalBytesUploaded, delta);
                                            progressTask.Value = Math.Min(newTotal, totalSize);
                                        }
                                        return Task.CompletedTask;
                                    },
                                    ct,
                                    skipIfExists);

                                if (result?.Status == UploadConstants.StatusSkipped)
                                {
                                    Interlocked.Increment(ref serverSkippedCount);
                                    // Add file size to progress since we're skipping (counts as processed)
                                    Interlocked.Add(ref totalBytesUploaded, fileSize);
                                    progressTask.Value = Math.Min(totalBytesUploaded, totalSize);
                                }
                                else
                                {
                                    Interlocked.Increment(ref successCount);
                                }
                            }
                            catch (FileNotFoundException)
                            {
                                lock (notFoundFiles)
                                {
                                    notFoundFiles.Add(filePath);
                                }
                                // Add file size to progress since we're done with this file
                                var fileSize = fileSizes[filePath];
                                Interlocked.Add(ref totalBytesUploaded, fileSize);
                                progressTask.Value = Math.Min(totalBytesUploaded, totalSize);
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref failureCount);
                                lock (failedFiles)
                                {
                                    var errorMessage = ex is HttpRequestException httpEx
                                        ? GetFriendlyErrorMessage(httpEx)
                                        : ex.Message;
                                    failedFiles.Add((filePath, errorMessage));
                                }
                                // Add file size to progress since we're done with this file
                                var fileSize = fileSizes[filePath];
                                Interlocked.Add(ref totalBytesUploaded, fileSize);
                                progressTask.Value = Math.Min(totalBytesUploaded, totalSize);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });

                        await Task.WhenAll(uploadTasks);
                        progressTask.Value = totalSize; // Ensure 100% at end
                    });
            }
            else
            {
                // No progress display for small total size
                var uploadTasks = files.Select(async filePath =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        var fileName = _fileSystem.Path.GetFileName(filePath);
                        var destPath = ResolveDestinationPath(filePath);

                        var result = await _fileTransferService.UploadFile(
                            filePath,
                            destPath,
                            null,
                            ct,
                            skipIfExists);

                        if (result?.Status == UploadConstants.StatusSkipped)
                        {
                            Interlocked.Increment(ref serverSkippedCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref successCount);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        lock (notFoundFiles)
                        {
                            notFoundFiles.Add(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failureCount);
                        lock (failedFiles)
                        {
                            var errorMessage = ex is HttpRequestException httpEx
                                ? GetFriendlyErrorMessage(httpEx)
                                : ex.Message;
                            failedFiles.Add((filePath, errorMessage));
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(uploadTasks);
            }

            // Output summary - always the same format regardless of progress display
            OutputUploadSummary(successCount, failureCount, skippedCount + serverSkippedCount, 
                oversizedCount, files.Count, failedFiles, notFoundFiles);
        }

        private void OutputUploadSummary(int successCount, int failureCount, int skippedCount, 
            int oversizedCount, int totalFiles, List<(string path, string error)> failedFiles, 
            List<string> notFoundFiles)
        {
            var totalSkipped = skippedCount + oversizedCount;

            // Build skip summary parts
            var skipParts = new List<string>();
            if (skippedCount > 0)
            {
                skipParts.Add($"{skippedCount} already exist");
            }
            if (oversizedCount > 0)
            {
                skipParts.Add($"{oversizedCount} too large");
            }
            var skipSummary = skipParts.Count > 0 ? $" ({string.Join(", ", skipParts)})" : "";

            if (failureCount == 0 && notFoundFiles.Count == 0)
            {
                if (totalSkipped > 0)
                {
                    _console.MarkupLineInterpolated($"Uploaded {successCount} files to {Destination}. {totalSkipped} skipped{skipSummary}.");
                }
                else
                {
                    _console.MarkupLineInterpolated($"Uploaded {successCount} files to {Destination}");
                }
            }
            else
            {
                _console.MarkupLineInterpolated($"[yellow]Uploaded {successCount} of {totalFiles} files to {Destination}[/]");

                if (notFoundFiles.Count > 0)
                {
                    _console.MarkupLineInterpolated($"[red]{notFoundFiles.Count} files not found: {string.Join(", ", notFoundFiles.Select(f => _fileSystem.Path.GetFileName(f)))}[/]");
                }

                foreach (var (path, error) in failedFiles)
                {
                    _console.MarkupLineInterpolated($"[red]Failed: {_fileSystem.Path.GetFileName(path)} - {error}[/]");
                }

                if (totalSkipped > 0)
                {
                    _console.MarkupLineInterpolated($"{totalSkipped} skipped{skipSummary}.");
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
