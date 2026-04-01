using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR;
using Spectre.Console;
using System.Reflection;

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
        [Description("Show all BitPantry assembly versions (local and remote)")]
        public bool Full { get; set; }

        public VersionCommand(IServerProxy proxy)
        {
            _proxy = proxy;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            if (!Full)
            {
                var entryVersion = Assembly.GetEntryAssembly()?.GetName().Version;
                Console.MarkupLine(entryVersion?.ToString(3) ?? UnknownVersion);
                return;
            }

            var table = new Table();
            table.AddColumn("Assembly");
            table.AddColumn("Version");
            table.AddColumn("Source");
            table.Border(TableBorder.Simple);

            // Entry assembly row
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var entryName = entryAssembly.GetName();
                table.AddRow(
                    entryName.Name ?? string.Empty,
                    entryName.Version?.ToString(3) ?? UnknownVersion,
                    "Local");
            }

            // All loaded BitPantry assemblies (excluding entry assembly)
            var bitPantryVersions = AssemblyVersionHelper.GetBitPantryAssemblyVersions();
            var entryAssemblyName = entryAssembly?.GetName().Name;
            foreach (var kvp in bitPantryVersions)
            {
                if (kvp.Key == entryAssemblyName)
                    continue;

                table.AddRow(kvp.Key, kvp.Value, "Local");
            }

            // Remote assemblies (only when connected and versions are available)
            if (_proxy.ConnectionState == ServerProxyConnectionState.Connected
                && _proxy.Server?.AssemblyVersions?.Count > 0)
            {
                foreach (var kvp in _proxy.Server.AssemblyVersions)
                {
                    table.AddRow(kvp.Key, kvp.Value, "Remote");
                }
            }

            Console.Write(table);
        }
    }
}
