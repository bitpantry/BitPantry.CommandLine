using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.API
{
    public interface IContainerScope : IDisposable
    {
        /// <summary>
        /// Returns an instance of the type requested
        /// </summary>
        /// <param name="commandType">The type to instantiate</param>
        /// <returns>An instance of the provided type</returns>
        CommandBase Get(Type commandType);
    }
}
