using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxClient.Commands;

/// <summary>
/// Demos for the [Flag] attribute feature.
/// 
/// [Flag] marks a bool argument as presence-only:
///   - Just --verbose (no value) → true
///   - Omitting --verbose → false
///   - Cannot pass --verbose true/false (that's for non-flag bools)
/// 
/// Non-flag bools require explicit values:
///   - --enabled true → true
///   - --enabled false → false
///   - Just --enabled (no value) → ERROR
/// </summary>
[Group(Name = "flag")]
public class FlagGroup { }

/// <summary>
/// Basic flag demo - verbose output.
/// 
/// Try:
///   flag basic hello              → "hello" (quiet)
///   flag basic hello --verbose    → "hello" with extra info (verbose)
/// </summary>
[Command(Group = typeof(FlagGroup), Name = "basic")]
public class FlagBasicCommand : CommandBase
{
    [Argument(Position = 0)]
    public string Message { get; set; } = "";

    [Argument(Name = "verbose")]
    [Flag]
    public bool Verbose { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        if (Verbose)
        {
            Console.MarkupLine($"[grey][[VERBOSE]] Executing at {DateTime.Now}[/]");
            Console.MarkupLine($"[grey][[VERBOSE]] Message length: {Message.Length}[/]");
        }
        Console.MarkupLine($"[green]{Message}[/]");
    }
}

/// <summary>
/// Multiple flags demo - build options.
/// 
/// Try:
///   flag build                           → normal build
///   flag build --clean                   → clean build
///   flag build --release                 → release build
///   flag build --clean --release --verbose → clean + release + verbose
/// </summary>
[Command(Group = typeof(FlagGroup), Name = "build")]
public class FlagBuildCommand : CommandBase
{
    [Argument(Name = "clean")]
    [Flag]
    public bool Clean { get; set; }

    [Argument(Name = "release")]
    [Flag]
    public bool Release { get; set; }

    [Argument(Name = "verbose")]
    [Flag]
    public bool Verbose { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        var config = Release ? "Release" : "Debug";
        
        if (Verbose)
            Console.MarkupLine($"[grey]Configuration: {config}, Clean: {Clean}[/]");
        
        if (Clean)
            Console.MarkupLine("[yellow]Cleaning previous build...[/]");
        
        Console.MarkupLine($"[green]Building in {config} mode...[/]");
        Console.MarkupLine("[green]Build complete![/]");
    }
}

/// <summary>
/// Comparison: Flag vs Non-Flag bool.
/// 
/// Try these to see the difference:
///   flag compare --active true           → works (non-flag bool)
///   flag compare --active false          → works (non-flag bool)
///   flag compare --active                → ERROR! (non-flag requires value)
///   flag compare --force                 → works (flag, presence = true)
///   flag compare --force true            → ERROR! (flag doesn't take value)
///   flag compare --active true --force   → both set
/// </summary>
[Command(Group = typeof(FlagGroup), Name = "compare")]
public class FlagCompareCommand : CommandBase
{
    /// <summary>
    /// Non-flag bool: requires explicit true/false value.
    /// </summary>
    [Argument(Name = "active")]
    public bool Active { get; set; }

    /// <summary>
    /// Flag bool: presence means true, absence means false.
    /// </summary>
    [Argument(Name = "force")]
    [Flag]
    public bool Force { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"[blue]Active (non-flag bool):[/] {Active}");
        Console.MarkupLine($"[blue]Force (flag bool):[/] {Force}");
        
        if (Force)
            Console.MarkupLine("[yellow]⚠ Force mode enabled![/]");
    }
}

/// <summary>
/// Real-world example: file sync command.
/// 
/// Try:
///   flag sync /src /dest                              → basic sync
///   flag sync /src /dest --dryrun                     → preview only
///   flag sync /src /dest --delete --recursive --verbose → full sync with deletion
/// </summary>
[Command(Group = typeof(FlagGroup), Name = "sync")]
public class FlagSyncCommand : CommandBase
{
    [Argument(Position = 0, Name = "source")]
    public string Source { get; set; } = "";

    [Argument(Position = 1, Name = "destination")]
    public string Destination { get; set; } = "";

    [Argument(Name = "dryrun")]
    [Flag]
    public bool DryRun { get; set; }

    [Argument(Name = "recursive")]
    [Flag]
    public bool Recursive { get; set; }

    [Argument(Name = "delete")]
    [Flag]
    public bool Delete { get; set; }

    [Argument(Name = "verbose")]
    [Flag]
    public bool Verbose { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        if (DryRun)
            Console.MarkupLine("[yellow][[DRY RUN]] No files will be modified[/]");
        
        Console.MarkupLine($"[blue]Syncing:[/] {Source} → {Destination}");
        
        if (Verbose)
        {
            Console.MarkupLine($"[grey]  Recursive: {Recursive}[/]");
            Console.MarkupLine($"[grey]  Delete extra: {Delete}[/]");
        }
        
        // Simulate some files
        var files = new[] { "file1.txt", "file2.txt", "subdir/file3.txt" };
        foreach (var file in files)
        {
            if (!Recursive && file.Contains('/'))
                continue;
                
            var action = DryRun ? "Would copy" : "Copying";
            Console.MarkupLine($"[green]  {action}:[/] {file}");
        }
        
        if (Delete)
        {
            var action = DryRun ? "Would delete" : "Deleting";
            Console.MarkupLine($"[red]  {action}:[/] orphaned.txt");
        }
        
        Console.MarkupLine("[green]Sync complete![/]");
    }
}
