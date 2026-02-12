using System;
using BitPantry.CommandLine.Component;

namespace BitPantry.CommandLine.AutoComplete.Handlers
{
    /// <summary>
    /// Immutable registry for autocomplete handler lookup at runtime.
    /// Does NOT activate handlers - use AutoCompleteHandlerActivator for that.
    /// </summary>
    public interface IAutoCompleteHandlerRegistry
    {
        /// <summary>
        /// Finds the handler type for the specified argument, or null if no handler matches.
        /// Attribute handlers take precedence over type handlers.
        /// Type handlers use last-registered-wins ordering.
        /// </summary>
        /// <param name="argumentInfo">The argument to find a handler for</param>
        /// <param name="activator">The activator to use for checking CanHandle on type handlers</param>
        /// <returns>The handler type, or null if none matches</returns>
        Type FindHandler(ArgumentInfo argumentInfo, AutoCompleteHandlerActivator activator);

        /// <summary>
        /// Gets the count of registered type handlers.
        /// </summary>
        int TypeHandlerCount { get; }
    }
}
