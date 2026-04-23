using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands
{
    [Command(Name = "version")]
    [Description("Displays version information")]
    public class VersionCommand : CommandBase
    {
        private const string UnknownVersion = "0.0.0";

        private readonly IServerProxy _proxy;

        [Argument(Name = "full")]
        [Flag]
        [Alias('f')]
        [Description("Show executing and loaded BitPantry.CommandLine assembly versions (local and remote)")]
        public bool Full { get; set; }

        public VersionCommand(IServerProxy proxy)
        {
            _proxy = proxy;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            if (!Full)
            {
                var (_, executingAssemblyVersion) = AssemblyVersionHelper.GetExecutingAssemblyVersion();
                Console.MarkupLine(executingAssemblyVersion);
                return;
            }

            var table = new Table();
            table.AddColumn("Assembly");
            table.AddColumn("Version");
            table.AddColumn("Source");
            table.AddColumn("Kind");
            table.Border(TableBorder.Simple);

            var (localExecutingAssemblyName, localExecutingAssemblyVersion) = AssemblyVersionHelper.GetExecutingAssemblyVersion();
            if (!string.IsNullOrWhiteSpace(localExecutingAssemblyName))
            {
                table.AddRow(
                    localExecutingAssemblyName,
                    localExecutingAssemblyVersion,
                    "Local",
                    "Executing");
            }

            if (_proxy.ConnectionState == ServerProxyConnectionState.Connected
                && _proxy.Server != null
                && !string.IsNullOrWhiteSpace(_proxy.Server.ExecutingAssemblyName))
            {
                table.AddRow(
                    _proxy.Server.ExecutingAssemblyName,
                    string.IsNullOrWhiteSpace(_proxy.Server.ExecutingAssemblyVersion) ? UnknownVersion : _proxy.Server.ExecutingAssemblyVersion,
                    "Remote",
                    "Executing");
            }

            // All loaded BitPantry.CommandLine assemblies (excluding executing assemblies)
            var localAssemblyVersions = AssemblyVersionHelper.GetBitPantryCommandLineAssemblyVersions();
            foreach (var kvp in localAssemblyVersions)
            {
                if (string.Equals(kvp.Key, localExecutingAssemblyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                table.AddRow(kvp.Key, kvp.Value, "Local", "Loaded");
            }

            // Remote loaded assemblies (only when connected and versions are available)
            if (_proxy.ConnectionState == ServerProxyConnectionState.Connected
                && _proxy.Server?.AssemblyVersions?.Count > 0)
            {
                foreach (var kvp in _proxy.Server.AssemblyVersions)
                {
                    if (string.Equals(kvp.Key, _proxy.Server.ExecutingAssemblyName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    table.AddRow(kvp.Key, kvp.Value, "Remote", "Loaded");
                }
            }

            Console.Write(table);
        }
    }
}
