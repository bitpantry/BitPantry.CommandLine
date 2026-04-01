namespace BitPantry.CommandLine.Remote.SignalR
{
    /// <summary>
    /// Centralized helper for discovering loaded BitPantry assembly versions.
    /// </summary>
    public static class AssemblyVersionHelper
    {
        private const string UnknownVersion = "0.0.0";

        /// <summary>
        /// Returns a dictionary of name → version (major.minor.patch) for all currently
        /// loaded assemblies whose name starts with "BitPantry".
        /// </summary>
        public static Dictionary<string, string> GetBitPantryAssemblyVersions()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name?.StartsWith("BitPantry", StringComparison.OrdinalIgnoreCase) == true)
                .ToDictionary(
                    a => a.GetName().Name!,
                    a => a.GetName().Version?.ToString(3) ?? UnknownVersion);
        }
    }
}
