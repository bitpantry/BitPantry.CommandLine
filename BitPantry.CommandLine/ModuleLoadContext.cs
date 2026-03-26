using System;
using System.Reflection;
using System.Runtime.Loader;

namespace BitPantry.CommandLine
{
    /// <summary>
    /// A custom AssemblyLoadContext for loading plugin modules with dependency isolation.
    /// Each plugin gets its own load context, which allows it to have its own dependencies
    /// while sharing types from the host's default context (like BitPantry.CommandLine types).
    /// </summary>
    internal class ModuleLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        /// <summary>
        /// Creates a new ModuleLoadContext for loading a plugin assembly and its dependencies.
        /// </summary>
        /// <param name="pluginPath">The full path to the plugin's main assembly (.dll file).</param>
        public ModuleLoadContext(string pluginPath)
            : base(isCollectible: false) // Modules are loaded once during build, no need for unloading
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        /// <summary>
        /// Attempts to load an assembly by name from the plugin's directory.
        /// If the assembly is not found locally, returns null to allow fallback to the default context.
        /// This fallback behavior is critical - it allows plugin types to share base types
        /// (like CommandBase, ICommandModule) with the host, avoiding type mismatch issues.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly to load.</param>
        /// <returns>The loaded assembly, or null to fall back to the default context.</returns>
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? path = _resolver.ResolveAssemblyToPath(assemblyName);
            if (path != null)
            {
                return LoadFromAssemblyPath(path);
            }

            // Return null to fall back to Default context.
            // This is how plugin's CommandBase, ICommandModule, and attribute types
            // resolve to the host's copies, avoiding type-mismatch problems.
            return null;
        }

        /// <summary>
        /// Attempts to load an unmanaged (native) DLL from the plugin's directory.
        /// </summary>
        /// <param name="unmanagedDllName">The name of the unmanaged DLL to load.</param>
        /// <returns>A handle to the loaded library, or IntPtr.Zero to fall back to default resolution.</returns>
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (path != null)
            {
                return LoadUnmanagedDllFromPath(path);
            }

            return IntPtr.Zero;
        }
    }
}
