using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Processing.Activation
{
    class SystemActivatorScope : IContainerScope
    {
        public void Dispose()
        {
            // do nothing
        }

        public CommandBase Get(Type commandType)
        {
            return Activator.CreateInstance(commandType) as CommandBase;
        }
    }
}
