using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BitPantry.CommandLine.Component;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace BitPantry.CommandLine.AutoComplete.Handlers
{
    /// <summary>
    /// Registry for autocomplete handlers.
    /// Manages registration and resolution of handlers.
    /// </summary>
    /// <remarks>
    /// <para><b>Activation Pattern (follows CommandRegistry pattern):</b></para>
    /// <para>
    /// Handlers follow the same two-phase activation pattern as commands:
    /// </para>
    /// <list type="number">
    ///   <item><description><b>Registration Phase</b>: <c>Register&lt;THandler&gt;()</c> stores the handler type in an internal list.</description></item>
    ///   <item><description><b>Build Phase</b>: <c>ConfigureServices(IServiceCollection)</c> registers all handler types with DI as transient services.</description></item>
    ///   <item><description><b>Runtime Phase</b>: <c>GetHandler()</c> uses <c>IServiceProvider</c> to resolve handler instances on demand.</description></item>
    /// </list>
    /// <para>
    /// This pattern allows handlers to have their own constructor dependencies resolved via DI,
    /// while keeping the registry's registration API simple (just types, no instances).
    /// </para>
    /// <para><b>Usage:</b></para>
    /// <code>
    /// // During setup:
    /// registry.Register&lt;MyHandler&gt;();
    /// registry.ConfigureServices(services);  // Adds all registered types to DI
    /// 
    /// // After Build() provides IServiceProvider:
    /// registry.SetServiceProvider(serviceProvider);
    /// 
    /// // At runtime (inside GetHandler):
    /// var handler = _serviceProvider.GetRequiredService(handlerType);
    /// </code>
    /// <para>
    /// See <c>CommandRegistry.ConfigureServices()</c> and <c>CommandActivator</c> for the
    /// equivalent command activation pattern this is based on.
    /// </para>
    /// </remarks>
    public class AutoCompleteHandlerRegistry
    {
        private readonly List<Type> _typeHandlers = new();
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// Gets the count of registered type handlers.
        /// </summary>
        public int TypeHandlerCount => _typeHandlers.Count;

        /// <summary>
        /// Sets the service provider for runtime handler resolution.
        /// Called after Build() creates the IServiceProvider.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance.</param>
        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Registers a type handler.
        /// </summary>
        /// <typeparam name="THandler">The handler type to register.</typeparam>
        public void Register<THandler>() where THandler : ITypeAutoCompleteHandler
        {
            _typeHandlers.Add(typeof(THandler));
        }

        /// <summary>
        /// Gets the handler for the specified argument, or null if no handler matches.
        /// Attribute handlers take precedence over type handlers.
        /// Type handlers use last-registered-wins ordering (iterates in reverse).
        /// </summary>
        /// <param name="argumentInfo">The argument to find a handler for.</param>
        /// <returns>The matching handler, or null if none matches.</returns>
        public IAutoCompleteHandler? GetHandler(ArgumentInfo argumentInfo)
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("ServiceProvider not set. Call SetServiceProvider() before GetHandler().");

            var propertyInfo = argumentInfo.PropertyInfo.GetPropertyInfo();

            // 1. Check for attribute handler first (takes precedence)
            var autoCompleteAttribute = propertyInfo.GetCustomAttributes(inherit: true)
                .OfType<IAutoCompleteAttribute>()
                .FirstOrDefault();
            if (autoCompleteAttribute != null)
            {
                return (IAutoCompleteHandler)_serviceProvider.GetRequiredService(autoCompleteAttribute.HandlerType);
            }

            // 2. Fall back to type handlers
            var propertyType = propertyInfo.PropertyType;
            
            // Unwrap Nullable<T> to get the underlying type
            var lookupType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Iterate in reverse: last registered wins
            for (int i = _typeHandlers.Count - 1; i >= 0; i--)
            {
                var handlerType = _typeHandlers[i];
                var handler = (ITypeAutoCompleteHandler)_serviceProvider.GetRequiredService(handlerType);
                
                if (handler.CanHandle(lookupType))
                {
                    return handler;
                }
            }

            return null;
        }
    }
}
