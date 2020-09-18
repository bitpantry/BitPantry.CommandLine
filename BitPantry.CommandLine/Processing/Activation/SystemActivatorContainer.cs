using BitPantry.CommandLine.API;
using System;

namespace BitPantry.CommandLine.Processing.Activation
{
    /// <summary>
    /// Uses the standard System.Activate for dependency injection - used as the default container
    /// </summary>c
    class SystemActivatorContainer : IContainer
    {
        public void Dispose()
        {
            // nothing to dispose
        }

        public IContainerScope CreateScope()
        {
            return new SystemActivatorScope();
        }
    }
}
