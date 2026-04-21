using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using Spectre.Console;

namespace SandboxServer.Commands;

/// <summary>
/// Remote command that reads a file from the client via IClientFileAccess.
/// When run on the server, this triggers the SignalR push → client upload flow.
/// Exercises: US-002, US-003, US-004, FR-006, FR-017
/// </summary>
[Command(Name = "remote-get")]
public class RemoteGetFileCommand : CommandBase
{
    [Argument(Position = 0)]
    [ClientFilePathAutoComplete]
    public string ClientPath { get; set; }

    private readonly IClientFileAccess _clientFiles;

    public RemoteGetFileCommand(IClientFileAccess clientFiles)
    {
        _clientFiles = clientFiles;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        try
        {
            await using var file = await _clientFiles.GetFileAsync(ClientPath, ct: ctx.CancellationToken);
            Console.WriteLine($"[REMOTE] File: {file.FileName} ({file.Length} bytes)");

            using var reader = new StreamReader(file.Stream);
            var content = await reader.ReadToEndAsync(ctx.CancellationToken);
            Console.WriteLine($"[REMOTE] Content: {content}");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"[REMOTE] Not found: {ex.Message}");
        }
        catch (FileAccessDeniedException ex)
        {
            Console.WriteLine($"[REMOTE] Access denied: {ex.Message}");
        }
    }
}

/// <summary>
/// Remote command that saves in-memory content to a file on the client via IClientFileAccess.
/// When run on the server, this triggers the SignalR push → client download flow.
/// Exercises: US-001, US-003, US-006, FR-003, FR-006, FR-016
/// </summary>
[Command(Name = "remote-save")]
public class RemoteSaveStreamCommand : CommandBase
{
    [Argument(Position = 0)]
    [ClientFilePathAutoComplete]
    public string ClientPath { get; set; }

    [Argument(Position = 1)]
    public string Content { get; set; }

    private readonly IClientFileAccess _clientFiles;

    public RemoteSaveStreamCommand(IClientFileAccess clientFiles)
    {
        _clientFiles = clientFiles;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(Content);
            using var stream = new MemoryStream(bytes);
            await _clientFiles.SaveFileAsync(stream, ClientPath, ct: ctx.CancellationToken);
            Console.WriteLine($"[REMOTE] Saved {bytes.Length} bytes to {ClientPath}");
        }
        catch (FileAccessDeniedException ex)
        {
            Console.WriteLine($"[REMOTE] Access denied: {ex.Message}");
        }
    }
}

/// <summary>
/// Remote command that copies a server-side file to the client via IClientFileAccess.
/// The source path is relative to the server's storage root.
/// Exercises: US-001, US-003, FR-004, FR-006, FR-016
/// </summary>
[Command(Name = "remote-copy")]
public class RemoteCopyFileCommand : CommandBase
{
    [Argument(Position = 0)]
    [ServerFilePathAutoComplete]
    public string SourcePath { get; set; }

    [Argument(Position = 1)]
    [ClientFilePathAutoComplete]
    public string ClientPath { get; set; }

    private readonly IClientFileAccess _clientFiles;

    public RemoteCopyFileCommand(IClientFileAccess clientFiles)
    {
        _clientFiles = clientFiles;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        try
        {
            await _clientFiles.SaveFileAsync(SourcePath, ClientPath, ct: ctx.CancellationToken);
            Console.WriteLine($"[REMOTE] Copied {SourcePath} -> {ClientPath}");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"[REMOTE] Source not found: {ex.Message}");
        }
        catch (FileAccessDeniedException ex)
        {
            Console.WriteLine($"[REMOTE] Access denied: {ex.Message}");
        }
    }
}

/// <summary>
/// Remote command that reads multiple files from the client by glob pattern via IClientFileAccess.
/// Exercises: US-007, FR-021, FR-023
/// </summary>
[Command(Name = "remote-get-files")]
public class RemoteGetFilesCommand : CommandBase
{
    [Argument(Position = 0)]
    public string GlobPattern { get; set; }

    [Argument(Name = "--limit")]
    public int Limit { get; set; } = 0;

    private readonly IClientFileAccess _clientFiles;

    public RemoteGetFilesCommand(IClientFileAccess clientFiles)
    {
        _clientFiles = clientFiles;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        try
        {
            int count = 0;
            await foreach (var file in _clientFiles.GetFilesAsync(GlobPattern, ct: ctx.CancellationToken))
            {
                await using (file)
                {
                    using var reader = new StreamReader(file.Stream);
                    var content = await reader.ReadToEndAsync(ctx.CancellationToken);
                    Console.WriteLine($"[REMOTE] {file.FileName} ({file.Length} bytes): {content}");
                }
                count++;
                if (Limit > 0 && count >= Limit)
                {
                    Console.WriteLine($"[REMOTE] Stopped at limit {Limit}");
                    break;
                }
            }
            Console.WriteLine($"[REMOTE] Total files: {count}");
        }
        catch (FileAccessDeniedException ex)
        {
            Console.WriteLine($"[REMOTE] Access denied: {ex.Message}");
        }
    }
}
