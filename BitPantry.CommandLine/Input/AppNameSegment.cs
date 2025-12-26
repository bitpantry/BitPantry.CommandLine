using System.Reflection;

namespace BitPantry.CommandLine.Input
{
    /// <summary>
    /// Prompt segment displaying the application name.
    /// </summary>
    public class AppNameSegment : IPromptSegment
    {
        private readonly string _appName;

        public int Order => 0;

        public AppNameSegment(string appName = null)
        {
            _appName = appName ?? GetDefaultAppName();
        }

        public string Render() => _appName;

        private static string GetDefaultAppName()
        {
            var assembly = Assembly.GetEntryAssembly();
            return assembly?.GetName().Name?.ToLowerInvariant() ?? "cli";
        }
    }
}
