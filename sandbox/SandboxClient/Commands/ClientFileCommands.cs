using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using Spectre.Console;

namespace SandboxClient.Commands;

/// <summary>
/// Tests IClientFileAccess.GetFileAsync locally — reads a file and prints its content.
/// Exercises: US-002, US-003, FR-002, FR-005
/// </summary>
[Command(Name = "local-get")]
public class LocalGetFileCommand : CommandBase
{
    [Argument(Position = 0)]
    [ClientFilePathAutoComplete]
    public string ClientPath { get; set; }

    private readonly IClientFileAccess _clientFiles;

    public LocalGetFileCommand(IClientFileAccess clientFiles)
    {
        _clientFiles = clientFiles;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        try
        {
            await using var file = await _clientFiles.GetFileAsync(ClientPath, ct: ctx.CancellationToken);
            AnsiConsole.MarkupLine($"[green]File:[/] {file.FileName} ({file.Length} bytes)");

            using var reader = new StreamReader(file.Stream);
            var content = await reader.ReadToEndAsync(ctx.CancellationToken);
            AnsiConsole.MarkupLine($"[blue]Content:[/] {Markup.Escape(content)}");
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Not found:[/] {Markup.Escape(ex.Message)}");
        }
    }
}

/// <summary>
/// Tests IClientFileAccess.SaveFileAsync(Stream, ...) locally — writes in-memory content to a file.
/// Exercises: US-001, US-003, US-006, FR-003, FR-005, FR-008
/// </summary>
[Command(Name = "local-save")]
public class LocalSaveStreamCommand : CommandBase
{
    [Argument(Position = 0)]
    [ClientFilePathAutoComplete]
    public string ClientPath { get; set; }

    [Argument(Position = 1)]
    public string Content { get; set; }

    private readonly IClientFileAccess _clientFiles;

    public LocalSaveStreamCommand(IClientFileAccess clientFiles)
    {
        _clientFiles = clientFiles;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(Content);
        using var stream = new MemoryStream(bytes);
        await _clientFiles.SaveFileAsync(stream, ClientPath, ct: ctx.CancellationToken);
        AnsiConsole.MarkupLine($"[green]Saved[/] {bytes.Length} bytes to {Markup.Escape(ClientPath)}");
    }
}

/// <summary>
/// Tests IClientFileAccess.SaveFileAsync(string, string) locally — copies a source file to a destination.
/// Exercises: US-001, US-003, FR-004, FR-005, FR-008
/// </summary>
[Command(Name = "local-copy")]
public class LocalCopyFileCommand : CommandBase
{
    [Argument(Position = 0)]
    [ClientFilePathAutoComplete]
    public string SourcePath { get; set; }

    [Argument(Position = 1)]
    [ClientFilePathAutoComplete]
    public string DestPath { get; set; }

    private readonly IClientFileAccess _clientFiles;

    public LocalCopyFileCommand(IClientFileAccess clientFiles)
    {
        _clientFiles = clientFiles;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        try
        {
            await _clientFiles.SaveFileAsync(SourcePath, DestPath, ct: ctx.CancellationToken);
            AnsiConsole.MarkupLine($"[green]Copied[/] {Markup.Escape(SourcePath)} -> {Markup.Escape(DestPath)}");
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Source not found:[/] {Markup.Escape(ex.Message)}");
        }
    }
}

/// <summary>
/// Tests IClientFileAccess.GetFilesAsync locally — reads multiple files by glob pattern.
/// Exercises: US-007, FR-021, FR-024
/// </summary>
[Command(Name = "local-get-files")]
public class LocalGetFilesCommand : CommandBase
{
    [Argument(Position = 0)]
    public string GlobPattern { get; set; }

    [Argument(Name = "--limit")]
    public int Limit { get; set; } = 0;

    private readonly IClientFileAccess _clientFiles;

    public LocalGetFilesCommand(IClientFileAccess clientFiles)
    {
        _clientFiles = clientFiles;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        int count = 0;
        await foreach (var file in _clientFiles.GetFilesAsync(GlobPattern, ct: ctx.CancellationToken))
        {
            await using (file)
            {
                using var reader = new StreamReader(file.Stream);
                var content = await reader.ReadToEndAsync(ctx.CancellationToken);
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(file.FileName)}[/] ({file.Length} bytes): {Markup.Escape(content)}");
            }
            count++;
            if (Limit > 0 && count >= Limit)
            {
                AnsiConsole.MarkupLine($"[yellow]Stopped at limit {Limit}[/]");
                break;
            }
        }
        AnsiConsole.MarkupLine($"[blue]Total files:[/] {count}");
    }
}
