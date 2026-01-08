using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Commands;

namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// A minimal test command with no arguments.
/// </summary>
[Command(Name = "testcmd")]
[Description("A test command")]
public class MinimalTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A test command with a string argument.
/// </summary>
[Command(Name = "argcmd")]
[Description("A command with a string argument")]
public class StringArgTestCommand : CommandBase
{
    [Argument]
    [Description("The name argument")]
    public string Name { get; set; } = string.Empty;

    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A test command with an enum argument for testing enum completion.
/// </summary>
[Command(Name = "enumcmd")]
[Description("A command with an enum argument")]
public class EnumArgTestCommand : CommandBase
{
    [Argument]
    [Description("The level argument")]
    public TestLevel Level { get; set; }

    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// Test enum for enum completion testing.
/// </summary>
public enum TestLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// A test command with multiple arguments.
/// </summary>
[Command(Name = "multicmd")]
[Description("A command with multiple arguments")]
public class MultiArgTestCommand : CommandBase
{
    [Argument]
    [Description("The name argument")]
    public string Name { get; set; } = string.Empty;

    [Argument]
    [Description("The count argument")]
    public int Count { get; set; }

    [Argument]
    [Description("Verbose output")]
    public bool Verbose { get; set; }

    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A test command with a positional argument.
/// </summary>
[Command(Name = "poscmd")]
[Description("A command with a positional argument")]
public class PositionalTestCommand : CommandBase
{
    [Argument(Position = 0)]
    [Description("The source file")]
    public string Source { get; set; } = string.Empty;

    [Argument(Position = 1)]
    [Description("The destination file")]
    public string Destination { get; set; } = string.Empty;

    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A test command with an IsRest positional argument.
/// </summary>
[Command(Name = "restcmd")]
[Description("A command with variadic positional arguments")]
public class IsRestTestCommand : CommandBase
{
    [Argument(Position = 0, IsRest = true)]
    [Description("Multiple files")]
    public string[] Files { get; set; } = Array.Empty<string>();

    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A server-related test command for testing partial matching.
/// </summary>
[Command(Name = "server")]
[Description("Server management command")]
public class ServerCommand : CommandBase
{
    [Argument]
    [Description("The host address")]
    public string Host { get; set; } = string.Empty;

    [Argument]
    [Description("The port number")]
    public int Port { get; set; } = 8080;

    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A connect subcommand for server.
/// </summary>
[Command(Name = "connect")]
[Description("Connect to a server")]
public class ConnectTestCommand : CommandBase
{
    [Argument]
    [Description("The host to connect to")]
    public string Host { get; set; } = string.Empty;

    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A disconnect subcommand for server.
/// </summary>
[Command(Name = "disconnect")]
[Description("Disconnect from server")]
public class DisconnectTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A service command (similar to server for testing partial match ranking).
/// </summary>
[Command(Name = "service")]
[Description("Service management command")]
public class ServiceCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A setup command (similar to server for testing partial match ranking).
/// </summary>
[Command(Name = "setup")]
[Description("Setup configuration")]
public class SetupCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A help command for testing exact match scenarios.
/// </summary>
[Command(Name = "help")]
[Description("Display help information")]
public class HelpTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A helper command for testing exact match scenarios (helps vs help).
/// </summary>
[Command(Name = "helper")]
[Description("Helper utility command")]
public class HelperTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A helpful command for testing exact match scenarios.
/// </summary>
[Command(Name = "helpful")]
[Description("Helpful tips command")]
public class HelpfulTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A server-dev command for testing hyphen filtering.
/// </summary>
[Command(Name = "server-dev")]
[Description("Development server management")]
public class ServerDevCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A server-prod command for testing hyphen filtering.
/// </summary>
[Command(Name = "server-prod")]
[Description("Production server management")]
public class ServerProdCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A config command for testing substring matching (contains "fig").
/// </summary>
[Command(Name = "config")]
[Description("Configuration management")]
public class ConfigTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A command with a path argument for testing quoted path completion.
/// </summary>
[Command(Name = "pathcmd")]
[Description("A command with a path argument")]
public class PathArgTestCommand : CommandBase
{
    [Argument]
    [Description("The file path")]
    public string Path { get; set; } = string.Empty;

    public void Execute(CommandExecutionContext ctx) { }
}

// ============================================================================
// Additional test commands starting with 's' for viewport scrolling tests
// (TC-10.x requires 15+ matching items to test scrolling behavior)
// ============================================================================

[Command(Name = "scan")]
[Description("Scan command for testing")]
public class ScanTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "search")]
[Description("Search command for testing")]
public class SearchTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "security")]
[Description("Security command for testing")]
public class SecurityTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "seed")]
[Description("Seed command for testing")]
public class SeedTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "send")]
[Description("Send command for testing")]
public class SendTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "shell")]
[Description("Shell command for testing")]
public class ShellTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "show")]
[Description("Show command for testing")]
public class ShowTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "shutdown")]
[Description("Shutdown command for testing")]
public class ShutdownTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "signal")]
[Description("Signal command for testing")]
public class SignalTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "snapshot")]
[Description("Snapshot command for testing")]
public class SnapshotTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "sort")]
[Description("Sort command for testing")]
public class SortTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "source")]
[Description("Source command for testing")]
public class SourceTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "spawn")]
[Description("Spawn command for testing")]
public class SpawnTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "split")]
[Description("Split command for testing")]
public class SplitTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "start")]
[Description("Start command for testing")]
public class StartTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "status")]
[Description("Status command for testing")]
public class StatusTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "stop")]
[Description("Stop command for testing")]
public class StopTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "storage")]
[Description("Storage command for testing")]
public class StorageTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "subscribe")]
[Description("Subscribe command for testing")]
public class SubscribeTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

[Command(Name = "sync")]
[Description("Sync command for testing")]
public class SyncTestCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A test command with positional arguments AND completion attributes (like FileDownloadCommand).
/// This reproduces the real-world scenario of commands with positional file paths.
/// </summary>
[Command(Name = "filecopy")]
[Description("Copy a file with positional completion")]
public class PositionalFileCompletionTestCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("The source file path")]
    [FilePathCompletion]
    public string Source { get; set; } = string.Empty;

    [Argument(Position = 1, IsRequired = true)]
    [Description("The destination file path")]
    [FilePathCompletion]
    public string Destination { get; set; } = string.Empty;

    public void Execute(CommandExecutionContext ctx) { }
}

/// <summary>
/// A test command with positional arguments (like FileDownloadCommand structure).
/// Uses FilePathCompletion for testing positional argument autocomplete.
/// </summary>
[Command(Name = "remotedownload")]
[Description("Download a file with positional completion")]
public class PositionalRemoteFileCompletionTestCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("The source file path")]
    [FilePathCompletion]
    public string Source { get; set; } = string.Empty;

    [Argument(Position = 1, IsRequired = true)]
    [Description("The destination file path")]
    [FilePathCompletion]
    public string Destination { get; set; } = string.Empty;

    public void Execute(CommandExecutionContext ctx) { }
}
