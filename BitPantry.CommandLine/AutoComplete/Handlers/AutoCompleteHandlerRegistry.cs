using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BitPantry.CommandLine.Component;

#nullable enable

namespace BitPantry.CommandLine.AutoComplete.Handlers
{
    /// <summary>
    /// Immutable registry for autocomplete handler lookup at runtime.
    /// Does NOT activate handlers - use HandlerActivator for that.
    /// </summary>
    public class AutoCompleteHandlerRegistry : IAutoCompleteHandlerRegistry
    {
        private readonly IReadOnlyList<Type> _typeHandlers;

        /// <summary>
        /// Creates an immutable handler registry from the builder's type list.
        /// </summary>
        /// <param name="typeHandlers">The list of registered handler types</param>
        internal AutoCompleteHandlerRegistry(List<Type> typeHandlers)
        {
            _typeHandlers = typeHandlers.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the count of registered type handlers.
        /// </summary>
        public int TypeHandlerCount => _typeHandlers.Count;

        /// <summary>
        /// Finds the handler type for the specified argument, or null if no handler matches.
        /// Attribute handlers take precedence over type handlers.
        /// Type handlers use last-registered-wins ordering (iterates in reverse).
        /// </summary>
        /// <param name="argumentInfo">The argument to find a handler for.</param>
        /// <param name="activator">The activator to use for checking CanHandle on type handlers.</param>
        /// <returns>The handler type, or null if none matches.</returns>
        public Type? FindHandler(ArgumentInfo argumentInfo, HandlerActivator activator)
        {
            var propertyInfo = argumentInfo.PropertyInfo.GetPropertyInfo();

            // 1. Check for attribute handler first (takes precedence)
            var autoCompleteAttribute = propertyInfo.GetCustomAttributes(inherit: true)
                .OfType<IAutoCompleteAttribute>()
                .FirstOrDefault();
            if (autoCompleteAttribute != null)
            {
                return autoCompleteAttribute.HandlerType;
            }

            // 2. Fall back to type handlers
            var propertyType = propertyInfo.PropertyType;
            
            // Unwrap Nullable<T> to get the underlying type
            var lookupType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Iterate in reverse: last registered wins
            for (int i = _typeHandlers.Count - 1; i >= 0; i--)
            {
                var handlerType = _typeHandlers[i];
                var handler = activator.Activate(handlerType);
                
                if (handler is ITypeAutoCompleteHandler typeHandler && typeHandler.CanHandle(lookupType))
                {
                    return handlerType;
                }
            }

            return null;
        }
    }
}
