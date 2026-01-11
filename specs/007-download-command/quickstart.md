# Quickstart: Download Command

**Date**: 2026-01-10  
**Feature**: 007-download-command

## Overview

The `server download` command enables users to transfer files from a connected remote server to their local machine. This document provides implementation examples for the key components.

---

## 1. DownloadCommand Class Structure

```csharp
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR;
using Microsoft.Extensions.FileSystemGlobbing;
using Spectre.Console;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    [Command(Group = typeof(ServerGroup), Name = "download")]
    [Description("Downloads files from the remote server")]
    public class DownloadCommand : CommandBase
    {
        private readonly IServerProxy _proxy;
        private readonly FileTransferService _fileTransferService;
        private readonly IAnsiConsole _console;
        private readonly IFileSystem _fileSystem;

        [Argument(Position = 0, Name = "source", IsRequired = true)]
        [Description("Remote file path or glob pattern")]
        public string Source { get; set; }

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
            // 1. Verify connection
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
            {
                _console.MarkupLine("[red]Not connected to server[/]");
                return;
            }

            // 2. Expand source pattern
            var files = await ExpandSourcePattern(Source, ctx.CancellationToken);
            
            if (files.Count == 0)
            {
                if (ContainsGlobCharacters(Source))
                    _console.MarkupLineInterpolated($"[yellow]No files matched pattern: {Source}[/]");
                else
                    _console.MarkupLineInterpolated($"[yellow]File not found: {Source}[/]");
                return;
            }

            // 3. Check for filename collisions
            var collisions = DetectCollisions(files);
            if (collisions.Any())
            {
                _console.MarkupLine("[red]Error: Filename collision detected. The following files would overwrite each other:[/]");
                foreach (var group in collisions)
                {
                    foreach (var path in group.Paths)
                        _console.MarkupLineInterpolated($"  - {path}");
                }
                _console.MarkupLine("No files were downloaded.");
                return;
            }

            // 4. Download files
            if (files.Count == 1)
                await DownloadSingleFileAsync(files[0], ctx.CancellationToken);
            else
                await DownloadMultipleFilesAsync(files, ctx.CancellationToken);
        }
    }
}
```

---

## 2. Expand Remote Source Pattern

```csharp
private async Task<List<RemoteFileMatch>> ExpandSourcePattern(string source, CancellationToken ct)
{
    var matches = new List<RemoteFileMatch>();

    if (!ContainsGlobCharacters(source))
    {
        // Literal path - check if file exists
        var info = await _fileTransferService.GetFileInfo(source, ct);
        if (info != null && info.Exists)
        {
            matches.Add(new RemoteFileMatch(
                RemotePath: source,
                FileName: _fileSystem.Path.GetFileName(source),
                Size: info.Length,
                LocalDestination: ResolveLocalPath(source)
            ));
        }
        return matches;
    }

    // Glob pattern - enumerate files with info (uses enhanced EnumerateFiles)
    var (basePath, pattern) = ParseGlobPattern(source);
    var isRecursive = pattern.Contains("**");
    
    var files = await _fileTransferService.EnumerateFiles(
        basePath,
        pattern,
        recursive: isRecursive,
        ct);

    foreach (var file in files)
    {
        matches.Add(new RemoteFileMatch(
            RemotePath: file.Path,
            FileName: _fileSystem.Path.GetFileName(file.Path),
            Size: file.Size,
            LocalDestination: ResolveLocalPath(file.Path)
        ));
    }

    return matches;
}

private bool ContainsGlobCharacters(string path)
    => path.Contains('*') || path.Contains('?');

private string ResolveLocalPath(string remotePath)
{
    var fileName = _fileSystem.Path.GetFileName(remotePath);
    
    if (Destination.EndsWith('/') || Destination.EndsWith('\\'))
        return _fileSystem.Path.Combine(Destination.TrimEnd('/', '\\'), fileName);
    
    return Destination;
}
```

---

## 3. Collision Detection

```csharp
private record CollisionGroup(string FileName, List<string> Paths);

private List<CollisionGroup> DetectCollisions(List<RemoteFileMatch> files)
{
    return files
        .GroupBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
        .Where(g => g.Count() > 1)
        .Select(g => new CollisionGroup(g.Key, g.Select(f => f.RemotePath).ToList()))
        .ToList();
}
```

---

## 4. Single File Download with Progress

```csharp
private async Task DownloadSingleFileAsync(RemoteFileMatch file, CancellationToken ct)
{
    var showProgress = file.Size >= DownloadConstants.ProgressDisplayThreshold;

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
                    var task = ctx.AddTask($"Downloading {file.FileName}");
                    task.MaxValue = file.Size;

                    await _fileTransferService.DownloadFile(
                        file.RemotePath,
                        file.LocalDestination,
                        progress =>
                        {
                            task.Value = progress.TotalRead;
                            return Task.CompletedTask;
                        },
                        ct);

                    task.Value = file.Size;
                });
        }
        else
        {
            await _fileTransferService.DownloadFile(
                file.RemotePath,
                file.LocalDestination,
                null,
                ct);
        }

        _console.MarkupLineInterpolated($"Downloaded {file.FileName} to {file.LocalDestination}");
    }
    catch (FileNotFoundException)
    {
        _console.MarkupLineInterpolated($"[red]File not found: {file.RemotePath}[/]");
    }
    catch (UnauthorizedAccessException ex)
    {
        _console.MarkupLineInterpolated($"[red]Permission denied: {ex.Message}[/]");
    }
    catch (InvalidDataException)
    {
        _console.MarkupLine("[red]Download failed: Checksum verification failed[/]");
    }
    catch (HttpRequestException ex)
    {
        _console.MarkupLineInterpolated($"[red]Download failed: {GetFriendlyErrorMessage(ex)}[/]");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("disconnected"))
    {
        _console.MarkupLine("[red]Connection lost during download[/]");
    }
}
```

---

## 5. Multiple Files Download with Aggregate Progress

```csharp
private async Task DownloadMultipleFilesAsync(List<RemoteFileMatch> files, CancellationToken ct)
{
    var successCount = 0;
    var failureCount = 0;
    var failedFiles = new List<(string path, string error)>();

    var totalSize = files.Sum(f => f.Size);
    var showProgress = totalSize >= DownloadConstants.ProgressDisplayThreshold;
    
    var semaphore = new SemaphoreSlim(DownloadConstants.MaxConcurrentDownloads);
    long totalBytesDownloaded = 0;
    var lastProgressPerFile = new ConcurrentDictionary<string, long>();

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
                var progressTask = ctx.AddTask($"Downloading to {Destination}");
                progressTask.MaxValue = totalSize;

                var downloadTasks = files.Select(async file =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        await _fileTransferService.DownloadFile(
                            file.RemotePath,
                            file.LocalDestination,
                            progress =>
                            {
                                var lastValue = lastProgressPerFile.GetOrAdd(file.RemotePath, 0L);
                                var delta = progress.TotalRead - lastValue;
                                if (delta > 0)
                                {
                                    lastProgressPerFile[file.RemotePath] = progress.TotalRead;
                                    var newTotal = Interlocked.Add(ref totalBytesDownloaded, delta);
                                    progressTask.Value = Math.Min(newTotal, totalSize);
                                }
                                return Task.CompletedTask;
                            },
                            ct);

                        Interlocked.Increment(ref successCount);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failureCount);
                        lock (failedFiles)
                        {
                            failedFiles.Add((file.RemotePath, GetFriendlyErrorMessage(ex)));
                        }
                        // Add remaining size to progress
                        var lastValue = lastProgressPerFile.GetOrAdd(file.RemotePath, 0L);
                        var remaining = file.Size - lastValue;
                        if (remaining > 0)
                        {
                            Interlocked.Add(ref totalBytesDownloaded, remaining);
                            progressTask.Value = Math.Min(totalBytesDownloaded, totalSize);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(downloadTasks);
                progressTask.Value = totalSize;
            });
    }
    else
    {
        // No progress bar for small total
        var downloadTasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await _fileTransferService.DownloadFile(
                    file.RemotePath,
                    file.LocalDestination,
                    null,
                    ct);
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failureCount);
                lock (failedFiles) { failedFiles.Add((file.RemotePath, GetFriendlyErrorMessage(ex))); }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(downloadTasks);
    }

    // Output summary
    OutputDownloadSummary(successCount, failureCount, files.Count, failedFiles);
}

private void OutputDownloadSummary(int successCount, int failureCount, int totalFiles, 
    List<(string path, string error)> failedFiles)
{
    if (failureCount == 0)
    {
        _console.MarkupLineInterpolated($"Downloaded {successCount} files to {Destination}");
    }
    else
    {
        _console.MarkupLineInterpolated($"[yellow]Downloaded {successCount} of {totalFiles} files to {Destination}[/]");
        foreach (var (path, error) in failedFiles)
        {
            _console.MarkupLineInterpolated($"[red]Failed: {_fileSystem.Path.GetFileName(path)} - {error}[/]");
        }
    }
}
```

---

## 6. FileTransferService.DownloadFile Enhancement

```csharp
public async Task<DownloadResult> DownloadFile(
    string remoteFilePath, 
    string localFilePath, 
    Func<FileDownloadProgress, Task>? progressCallback = null,
    CancellationToken token = default)
{
    if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
        throw new InvalidOperationException("The client is disconnected");

    var correlationId = Guid.NewGuid().ToString();
    var tcs = new TaskCompletionSource();
    long fileSize = 0;

    // Register progress callback if provided
    if (progressCallback != null)
    {
        await _downloadProgressRegistry.Register(correlationId, async progress =>
        {
            fileSize = progress.TotalSize;
            await progressCallback(progress);
            if (progress.TotalRead >= progress.TotalSize)
                tcs.TrySetResult();
        });
    }

    try
    {
        using var client = _httpClientFactory.CreateClient();

        // Build URL with progress parameters
        var downloadUrl = $"{_proxy.Server.ConnectionUri.AbsoluteUri.TrimEnd('/')}/{ServiceEndpointNames.FileDownload}" +
            $"?filePath={Uri.EscapeDataString(remoteFilePath)}" +
            (progressCallback != null ? $"&connectionId={_proxy.Server.ConnectionId}&correlationId={correlationId}" : "");

        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        
        if (_accessTokenMgr.CurrentToken?.Token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessTokenMgr.CurrentToken.Token);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new FileNotFoundException($"Remote file not found: {remoteFilePath}", remoteFilePath);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Download failed: {response.StatusCode}", null, response.StatusCode);

        // Get expected checksum
        var expectedChecksum = response.Headers.TryGetValues("X-File-Checksum", out var values) 
            ? values.First() : null;

        // Ensure local directory exists
        var directory = Path.GetDirectoryName(localFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // Stream to local file with checksum verification
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var responseStream = await response.Content.ReadAsStreamAsync(token);
        await using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);

        var buffer = new byte[81920];
        int bytesRead;
        long totalRead = 0;

        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, token);
            hasher.AppendData(buffer, 0, bytesRead);
            totalRead += bytesRead;
        }

        // Verify checksum
        if (!string.IsNullOrEmpty(expectedChecksum))
        {
            var actualChecksum = Convert.ToHexString(hasher.GetHashAndReset());
            if (!string.Equals(expectedChecksum, actualChecksum, StringComparison.OrdinalIgnoreCase))
            {
                // Delete corrupt file
                File.Delete(localFilePath);
                throw new InvalidDataException($"Checksum verification failed for: {remoteFilePath}");
            }
        }

        return new DownloadResult(remoteFilePath, localFilePath, DownloadStatus.Success, totalRead, null);
    }
    catch
    {
        // Clean up partial file on any failure
        if (File.Exists(localFilePath))
            File.Delete(localFilePath);
        throw;
    }
    finally
    {
        if (progressCallback != null)
            await _downloadProgressRegistry.Unregister(correlationId);
    }
}
```

---

## 7. Server-Side Streaming with Progress

```csharp
// In FileTransferEndpointService.cs
public async Task<IResult> DownloadFile(string filePath, HttpContext httpContext)
{
    // ... path validation (existing code) ...

    // Get file info
    var fileInfo = _fileSystem.FileInfo.New(validatedPath);
    var fileSize = fileInfo.Length;

    // Get SignalR connection info from query string
    var connectionId = httpContext.Request.Query["connectionId"].FirstOrDefault();
    var correlationId = httpContext.Request.Query["correlationId"].FirstOrDefault();
    var client = !string.IsNullOrEmpty(connectionId) 
        ? _cliHubCtx.Clients.Client(connectionId) 
        : null;

    // Set response headers
    httpContext.Response.ContentType = "application/octet-stream";
    httpContext.Response.Headers.ContentLength = fileSize;

    // Compute checksum and stream file
    using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    await using var fileStream = _fileSystem.File.OpenRead(validatedPath);
    
    var buffer = new byte[81920]; // 80KB chunks
    int bytesRead;
    long totalRead = 0;
    var lastProgressTime = DateTime.UtcNow;
    const int ProgressThrottleMs = 100;

    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, httpContext.RequestAborted)) > 0)
    {
        hasher.AppendData(buffer, 0, bytesRead);
        await httpContext.Response.Body.WriteAsync(buffer, 0, bytesRead, httpContext.RequestAborted);
        totalRead += bytesRead;

        // Send progress via SignalR (throttled)
        if (client != null && !string.IsNullOrEmpty(correlationId))
        {
            var now = DateTime.UtcNow;
            if ((now - lastProgressTime).TotalMilliseconds >= ProgressThrottleMs || totalRead >= fileSize)
            {
                lastProgressTime = now;
                await client.SendAsync("ReceiveMessage", new FileDownloadProgressMessage(
                    correlationId,
                    totalRead,
                    fileSize
                ), httpContext.RequestAborted);
            }
        }
    }

    // Add checksum header (sent as trailer or set before streaming in HTTP/2)
    var checksum = Convert.ToHexString(hasher.GetHashAndReset());
    httpContext.Response.Headers["X-File-Checksum"] = checksum;

    return Results.Empty;
}
```

---

## 8. Constants

> **Note**: Documentation references constant **names** (e.g., `DownloadConstants.ProgressDisplayThreshold`) rather than hardcoded values. This ensures docs remain accurate if values change. Initial values mirror `UploadConstants` for consistency.

```csharp
public static class DownloadConstants
{
    /// <summary>
    /// Minimum total download size to display progress bar.
    /// Mirrors UploadConstants.ProgressDisplayThreshold for consistency.
    /// </summary>
    public const long ProgressDisplayThreshold = 25 * 1024 * 1024;

    /// <summary>
    /// Maximum concurrent file downloads.
    /// Mirrors UploadConstants.MaxConcurrentUploads for consistency.
    /// </summary>
    public const int MaxConcurrentDownloads = 4;

    /// <summary>
    /// Chunk size for streaming downloads (80KB).
    /// </summary>
    public const int ChunkSize = 81920;

    /// <summary>
    /// Minimum milliseconds between progress updates.
    /// </summary>
    public const int ProgressThrottleMs = 100;
}
```

---

## 9. Helper Types

```csharp
public record RemoteFileMatch(
    string RemotePath,
    string FileName,
    long Size,
    string LocalDestination
);

public record DownloadResult(
    string RemotePath,
    string LocalPath,
    DownloadStatus Status,
    long BytesTransferred,
    string? Error
);

public enum DownloadStatus
{
    Success,
    Failed
}

public record FileDownloadProgress(
    long TotalRead,
    long TotalSize,
    string? Error,
    string CorrelationId
);
```
