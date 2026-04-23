using System.Reflection;

namespace BitPantry.CommandLine.Remote.SignalR
{
    /// <summary>
    /// Centralized helper for discovering loaded BitPantry assembly versions.
    /// </summary>
    public static class AssemblyVersionHelper
    {
        private const string CommandLineAssemblyPrefix = "BitPantry.CommandLine";
        private const string UnknownVersion = "0.0.0";

        /// <summary>
        /// Returns the entry assembly name and version (major.minor.patch) for the current process.
        /// </summary>
        public static (string Name, string Version) GetExecutingAssemblyVersion()
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName();
            return (
                assemblyName?.Name ?? string.Empty,
                assemblyName?.Version?.ToString(3) ?? UnknownVersion);
        }

        /// <summary>
        /// Returns a dictionary of name → version (major.minor.patch) for all currently
        /// loaded assemblies whose name starts with "BitPantry.CommandLine".
        /// </summary>
        public static Dictionary<string, string> GetBitPantryCommandLineAssemblyVersions()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name?.StartsWith(CommandLineAssemblyPrefix, StringComparison.OrdinalIgnoreCase) == true)
                .ToDictionary(
                    a => a.GetName().Name!,
                    a => a.GetName().Version?.ToString(3) ?? UnknownVersion);
        }
    }
}
