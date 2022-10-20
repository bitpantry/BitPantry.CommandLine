using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.API
{

    /// <summary>
    /// Defines the members for a dependency injection container that can be used to inject commands
    /// </summary>
    public interface IContainer : IDisposable
    {
        /// <summary>
        /// Creates a dependency scope for scoped lifetime management
        /// </summary>
        /// <returns>The dependency scope</returns>
        IContainerScope CreateScope();
    }
}
