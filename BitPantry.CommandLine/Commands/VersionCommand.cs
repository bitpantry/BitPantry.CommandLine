using BitPantry.CommandLine.API;
using Spectre.Console;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitPantry.CommandLine.Commands
{
    /// <summary>
    /// Displays the application version.
    /// </summary>
    [Command(Name = "version")]
    [Description("Displays the application version")]
    public class VersionCommand : CommandBase
    {
        [Argument]
        [Alias('f')]
        [Description("Include framework assembly versions")]
        public Option Full { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var version = entryAssembly?.GetName().Version?.ToString() ?? "Unknown";
            var name = entryAssembly?.GetName().Name ?? "Application";

            Console.MarkupLine($"[bold]{name}[/] v{version}");

            if (Full.IsPresent)
            {
                Console.WriteLine();
                Console.MarkupLine("[dim]Framework assemblies:[/]");

                foreach (var assembly in GetFrameworkAssemblies())
                {
                    Console.MarkupLine($"  [grey]{assembly.Name}[/] {assembly.Version}");
                }
            }
        }

        private IEnumerable<(string Name, string Version)> GetFrameworkAssemblies()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
                return Enumerable.Empty<(string, string)>();

            return entryAssembly
                .GetReferencedAssemblies()
                .Where(a => a.Name != null && (
                    a.Name.StartsWith("BitPantry.CommandLine") ||
                    a.Name.StartsWith("Microsoft.") ||
                    a.Name.StartsWith("System.")))
                .OrderBy(a => a.Name)
                .Select(a => (a.Name ?? "Unknown", a.Version?.ToString() ?? "Unknown"))
                .Take(20); // Limit to avoid excessive output
        }
    }
}
